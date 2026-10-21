using System;
using System.Collections.Generic;
using UnityEngine;
using Chestworks.Common;

namespace Chestworks.Core;

/// <summary>
///     The core of Chestworks: each purpose's reach rules, the usable containers they reach for the local player
///     sorted nearest first, cached stock counts, and taking and storing items across those containers. Containers
///     are gathered from <see cref="Registry" /> on a short interval and filtered by <see cref="Usability" />; other mods
///     can defer a purpose to themselves. A container another game owns is changed locally and through
///     <see cref="ContainerRequests" /> on a modded server, or claimed via <see cref="Ownership" /> on a vanilla one.
/// </summary>
internal static class StorageNetwork
{
    /// <summary>Seconds between gatherings of a purpose's containers.</summary>
    private const float ScanInterval = 0.25f;

    /// <summary>Meters the character or the point must move to gather sooner.</summary>
    private const float ScanMove = 4f;

    /// <summary>Meters the point must move for the gathered containers to be reordered around it.</summary>
    private const float SortMove = 0.5f;

    private sealed class Purpose
    {
        internal string Owner;
        internal Func<Player, ReachRules> Rules;
        internal readonly List<Container> Available = new();
        internal readonly Dictionary<(string Name, int Quality, bool WorldLevel), int> Counts = new();
        internal float NextScan = -1f;
        internal Vector3 ScanCharacter;
        internal Vector3 ScanPoint;
        internal Vector3 SortPoint;
        internal int CountFrame = -1;
        internal int Signature;
        internal int RulesFrame = -1;
        internal ReachRules LastRules;
    }

    private sealed class Deferral
    {
        internal string Guid;
        internal string For;
        internal Func<bool> Active;
    }

    private static readonly Dictionary<string, Purpose> _purposes = new();
    private static readonly List<Deferral> _deferrals = new();
    private static readonly List<Container> _scratch = new();
    private static readonly List<Registry.Entry> _entries = new();
    private static readonly List<Container> _none = new();
    private static readonly List<CraftingStation> _stations = new();
    private static readonly HashSet<CraftingStation> _linked = new();
    private static readonly List<float> _distances = new();

    /// <summary>Mods that show container stock themselves.</summary>
    internal static readonly HashSet<string> ShowsStock = new();

    private static int _revision;

    /// <summary>Rises whenever stock may have changed; reading it brings each purpose's containers up to date first.</summary>
    internal static int Revision
    {
        get
        {
            foreach (string purpose in _purposes.Keys)
            {
                Available(purpose);
            }

            return _revision;
        }
    }

    internal static void Bump()
    {
        _revision++;
    }

    internal static void RegisterReach(string purpose, string owner, Func<Player, ReachRules> rules)
    {
        if (_purposes.TryGetValue(purpose, out Purpose existing) && existing.Owner != owner)
        {
            Log.LogWarning($"{owner} asked to set the reach for '{purpose}', which {existing.Owner} already sets; ignored.");
            return;
        }

        _purposes[purpose] = new Purpose { Owner = owner, Rules = rules };
        _revision++;
    }

    internal static void UnregisterReach(string purpose, string owner)
    {
        if (_purposes.TryGetValue(purpose, out Purpose existing) && existing.Owner == owner)
        {
            _purposes.Remove(purpose);
            _revision++;
        }
    }

    /// <summary>Registers a deferral, replacing one the same mod made for the same purpose.</summary>
    internal static void Defer(string guid, string purpose, Func<bool> active)
    {
        _deferrals.RemoveAll(deferral => deferral.Guid == guid && deferral.For == purpose);
        _deferrals.Add(new Deferral { Guid = guid, For = purpose, Active = active });
    }

    /// <summary>Whether another mod takes from chests for this purpose itself right now.</summary>
    internal static bool Deferred(string purpose)
    {
        foreach (Deferral deferral in _deferrals)
        {
            if ((deferral.For == null || deferral.For == purpose) && deferral.Active())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The purpose's rules for the local player, asked for once a frame; null when it reaches nothing now.</summary>
    private static ReachRules RulesFor(string purpose, out Purpose state)
    {
        state = null;
        Player player = Player.m_localPlayer;

        if (purpose == null || !player || !_purposes.TryGetValue(purpose, out state))
        {
            return null;
        }

        if (state.RulesFrame == Time.frameCount)
        {
            return state.LastRules;
        }

        state.RulesFrame = Time.frameCount;

        try
        {
            state.LastRules = state.Rules(player);
        }
        catch (Exception e)
        {
            Log.LogError($"Reach rules for '{purpose}' from {state.Owner} failed: {e.Message}");
            state.LastRules = null;
        }

        return state.LastRules;
    }

    internal static bool IsActive(string purpose)
    {
        return RulesFor(purpose, out _) != null && !Deferred(purpose);
    }

    /// <summary>The containers the purpose reaches, nearest first; empty when it is not active.</summary>
    internal static List<Container> Available(string purpose)
    {
        ReachRules rules = RulesFor(purpose, out Purpose state);

        if (state == null)
        {
            return _none;
        }

        if (rules == null || Deferred(purpose))
        {
            if (state.Available.Count > 0 || state.Signature != 0)
            {
                state.Available.Clear();
                state.Signature = 0;
                _revision++;
            }

            state.NextScan = -1f;
            return state.Available;
        }

        Player player = Player.m_localPlayer;
        Vector3 character = player.transform.position;
        Vector3 point = rules.Point ?? character;

        if (Time.time < state.NextScan && (character - state.ScanCharacter).sqrMagnitude < ScanMove * ScanMove
            && (point - state.ScanPoint).sqrMagnitude < ScanMove * ScanMove)
        {
            if ((point - state.SortPoint).sqrMagnitude >= SortMove * SortMove)
            {
                state.SortPoint = point;
                SortByDistance(state.Available, point);
            }

            return state.Available;
        }

        state.NextScan = Time.time + ScanInterval;
        state.ScanCharacter = character;
        state.ScanPoint = point;
        state.SortPoint = point;
        state.CountFrame = -1;

        Collect(player, rules, point, state.Available);

        int signature = Signature(state.Available);

        if (signature != state.Signature)
        {
            state.Signature = signature;
            _revision++;
        }

        return state.Available;
    }

    /// <summary>Stock of an item across the purpose's containers, cached for the frame.</summary>
    internal static int Count(string purpose, string name, int quality = -1, bool matchWorldLevel = true)
    {
        List<Container> containers = Available(purpose);

        if (containers.Count == 0 || !_purposes.TryGetValue(purpose, out Purpose state))
        {
            return 0;
        }

        (string, int, bool) key = (name, quality, matchWorldLevel);

        if (state.CountFrame != Time.frameCount)
        {
            state.CountFrame = Time.frameCount;
            state.Counts.Clear();
        }

        if (state.Counts.TryGetValue(key, out int cached))
        {
            return cached;
        }

        int total = 0;

        foreach (Container container in containers)
        {
            if (container)
            {
                // Load picks up changes by other players ahead of the game's once-a-second check.
                container.Load();
                total += container.GetInventory().CountItems(name, quality, matchWorldLevel);
            }
        }

        state.Counts[key] = total;
        return total;
    }

    internal static bool Cheated(string purpose, Piece.Requirement[] resources, int quality, bool matchWorldLevel)
    {
        foreach (Container container in Available(purpose))
        {
            if (container && container.GetInventory().ItemCheated(resources, quality, matchWorldLevel))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Takes up to the amount from the purpose's containers, nearest first, and returns what was taken.
    ///     Each removal from a container another game owns must be mirrored to that owner.
    /// </summary>
    internal static int Take(string purpose, string name, int amount, int quality, bool worldLevelBased)
    {
        int taken = 0;

        foreach (Container container in Available(purpose))
        {
            if (taken >= amount)
            {
                break;
            }

            if (!container)
            {
                continue;
            }

            container.Load();
            Inventory inventory = container.GetInventory();
            int part = Mathf.Min(amount - taken, inventory.CountItems(name, quality, worldLevelBased));

            if (part <= 0)
            {
                continue;
            }

            bool ask = Ownership.Prepare(container);
            inventory.RemoveItem(name, part, quality, worldLevelBased);
            taken += part;

            if (ask)
            {
                ContainerRequests.Take(container.m_nview, name, part, quality, worldLevelBased);
            }
        }

        Changed(purpose);
        return taken;
    }

    /// <summary>
    ///     Stores up to the amount in the purpose's containers that already hold the item, nearest the point first,
    ///     never in the excluded piece; returns what was stored. Not held back by other mods taking over the purpose.
    /// </summary>
    internal static int Store(string purpose, Vector3 point, GameObject prefab, int amount, bool cheated, Piece exclude)
    {
        ReachRules rules = RulesFor(purpose, out _);

        if (rules == null || !prefab || !prefab.TryGetComponent(out ItemDrop drop))
        {
            return 0;
        }

        ItemDrop.ItemData template = drop.m_itemData;
        int stored = 0;

        _scratch.Clear();
        _scratch.AddRange(Available(purpose));
        SortByDistance(_scratch, point);

        foreach (Container container in _scratch)
        {
            if (stored >= amount)
            {
                break;
            }

            if (!container)
            {
                continue;
            }

            if (exclude && (container.m_nview == exclude.m_nview || container.transform.IsChildOf(exclude.transform)))
            {
                continue;
            }

            container.Load();
            Inventory inventory = container.GetInventory();

            if (!inventory.HaveItem(template.m_shared.m_name, false) || Items.Space(inventory, template) <= 0)
            {
                continue;
            }

            bool ask = Ownership.Prepare(container);
            int part = Items.Add(inventory, prefab, amount - stored, cheated);
            stored += part;

            if (ask && part > 0)
            {
                ContainerRequests.Store(container.m_nview, prefab.name, part, cheated);
            }
        }

        _scratch.Clear();
        Changed(purpose);
        return stored;
    }

    /// <summary>Drops a purpose's cached counts and marks stock as changed.</summary>
    internal static void Changed(string purpose)
    {
        if (purpose != null && _purposes.TryGetValue(purpose, out Purpose state))
        {
            state.CountFrame = -1;
        }

        _revision++;
    }

    /// <summary>Usable containers in the radius around the character, or in range of the stations the rules include.</summary>
    private static void Collect(Player player, ReachRules rules, Vector3 point, List<Container> into)
    {
        into.Clear();

        if (!player)
        {
            return;
        }

        FindStations(rules, player.transform.position, point);

        Vector3 eye = player.m_eye ? player.m_eye.position : player.transform.position;
        long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
        float radiusSqr = rules.Radius > 0f ? rules.Radius * rules.Radius : -1f;

        Registry.Live(_entries);

        foreach (Registry.Entry entry in _entries)
        {
            Vector3 position = entry.Container.transform.position;
            bool inRadius = (eye - position).sqrMagnitude < radiusSqr;

            if ((inRadius || InStationRange(position)) && Usability.Usable(entry, playerId))
            {
                into.Add(entry.Container);
            }
        }

        _entries.Clear();
        _stations.Clear();
        _linked.Clear();
        SortByDistance(into, point);
    }

    private static void SortByDistance(List<Container> containers, Vector3 point)
    {
        _distances.Clear();

        foreach (Container container in containers)
        {
            _distances.Add(container ? (container.transform.position - point).sqrMagnitude : float.MaxValue);
        }

        for (int i = 1; i < containers.Count; i++)
        {
            Container container = containers[i];
            float distance = _distances[i];
            int j = i;

            while (j > 0 && _distances[j - 1] > distance)
            {
                containers[j] = containers[j - 1];
                _distances[j] = _distances[j - 1];
                j--;
            }

            containers[j] = container;
            _distances[j] = distance;
        }
    }

    /// <summary>Stations covering the character or the point; with Linked, every station chained to them by overlapping ranges.</summary>
    private static void FindStations(ReachRules rules, Vector3 character, Vector3 point)
    {
        _stations.Clear();
        _linked.Clear();

        if (!rules.Stations)
        {
            return;
        }

        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if ((Covers(station, character) || Covers(station, point)) && _linked.Add(station))
            {
                _stations.Add(station);
            }
        }

        if (!rules.Linked)
        {
            return;
        }

        // Each station is tested against the chain once; the chain grows while the loop runs.
        for (int i = 0; i < _stations.Count; i++)
        {
            CraftingStation from = _stations[i];

            foreach (CraftingStation other in CraftingStation.m_allStations)
            {
                if (other && !_linked.Contains(other) && Overlap(from, other) && _linked.Add(other))
                {
                    _stations.Add(other);
                }
            }
        }
    }

    /// <summary>Whether the station's build range, measured flat, covers the point.</summary>
    private static bool Covers(CraftingStation station, Vector3 point)
    {
        return station && Utils.DistanceXZ(station.transform.position, point) < station.GetStationBuildRange();
    }

    private static bool Overlap(CraftingStation a, CraftingStation b)
    {
        return Utils.DistanceXZ(a.transform.position, b.transform.position) < a.GetStationBuildRange() + b.GetStationBuildRange();
    }

    private static bool InStationRange(Vector3 position)
    {
        foreach (CraftingStation station in _stations)
        {
            if (Covers(station, position))
            {
                return true;
            }
        }

        return false;
    }

    private static int Signature(List<Container> containers)
    {
        int signature = containers.Count;

        foreach (Container container in containers)
        {
            signature = signature * 31 + (container ? container.GetInstanceID() : 0);
        }

        return signature;
    }
}
