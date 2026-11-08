using System.Collections.Generic;
using UnityEngine;
using Chestworks.Common;

namespace Chestworks.Core;
/// <summary>
///     Sends take and store changes for a container another game owns to that owner over routed RPCs, used on
///     servers that run Chestworks. The sender has already applied the change to its local copy; the owner applies
///     it to the authoritative inventory. A request that reaches a game which no longer owns the container is
///     passed on to the new owner, up to <see cref="MaxHops" /> times, and dropped if it returns to its origin.
///     When the owner holds fewer items than a take asked for, because someone else took them in the same moment,
///     it says so, and the taker pays the difference from their own inventory or another container in reach.
/// </summary>
internal static class ContainerRequests
{
    private const string TakeRpc = "Chestworks_ContainerTake";
    private const string StoreRpc = "Chestworks_ContainerStore";
    private const string ShortRpc = "Chestworks_ContainerTakeShort";

    /// <summary>How many times a request may be passed on.</summary>
    private const int MaxHops = 2;

    /// <summary>Takes waiting for the owner's answer, by request number; forgotten after a while.</summary>
    private sealed class Pending
    {
        internal string Purpose;
        internal string Name;
        internal int Quality;
        internal bool WorldLevelBased;
        internal float Until;
    }

    private const float PendingSeconds = 30f;
    private static readonly Dictionary<int, Pending> _pending = new();
    private static int _nextRequest;

    internal static void Register()
    {
        ZRoutedRpc.instance.Register<ZDOID, ZPackage>(TakeRpc, OnTake);
        ZRoutedRpc.instance.Register<ZDOID, ZPackage>(StoreRpc, OnStore);
        ZRoutedRpc.instance.Register<int, int>(ShortRpc, OnShort);
        _pending.Clear();
    }

    /// <summary>Asks the container's owner to remove items this game already removed from its local copy.</summary>
    internal static void Take(string purpose, ZNetView view, string name, int amount, int quality, bool worldLevelBased)
    {
        Forget();
        int request = ++_nextRequest;
        _pending[request] = new Pending { Purpose = purpose, Name = name, Quality = quality, WorldLevelBased = worldLevelBased, Until = Time.time + PendingSeconds };

        ZPackage package = new();
        package.Write(name);
        package.Write(amount);
        package.Write(quality);
        package.Write(worldLevelBased);

        // After the fields a 1.0.0 owner reads, which ignores the rest and never answers.
        package.Write(request);
        Send(TakeRpc, view.GetZDO(), package, ZDOMan.GetSessionID(), 0);
    }

    /// <summary>Asks the container's owner to add items this game already added to its local copy.</summary>
    internal static void Store(ZNetView view, string prefab, int amount, bool cheated)
    {
        ZPackage package = new();
        package.Write(prefab);
        package.Write(amount);
        package.Write(cheated);
        Send(StoreRpc, view.GetZDO(), package, ZDOMan.GetSessionID(), 0);
    }

    private static void Send(string rpc, ZDO zdo, ZPackage body, long origin, int hops)
    {
        ZPackage package = new();
        package.Write(origin);
        package.Write(hops);
        package.Write(body);
        ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), rpc, zdo.m_uid, package);
    }

    private static void OnTake(long sender, ZDOID id, ZPackage package)
    {
        long origin = package.ReadLong();
        int hops = package.ReadInt();
        ZPackage body = package.ReadPackage();

        if (!Owned(TakeRpc, id, body, origin, hops, out Container container))
        {
            return;
        }

        string name = body.ReadString();
        int amount = body.ReadInt();
        int quality = body.ReadInt();
        bool worldLevelBased = body.ReadBool();

        int request = body.GetPos() < body.Size() ? body.ReadInt() : 0;

        Inventory inventory = container.GetInventory();
        int held = inventory.CountItems(name, quality, worldLevelBased);
        inventory.RemoveItem(name, amount, quality, worldLevelBased);

        if (held < amount)
        {
            Log.LogInfo($"{container.name} held {held} of the {amount} {name} asked for.");

            if (request != 0)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(origin, ShortRpc, request, amount - held);
            }
        }
    }

    /// <summary>The owner's answer that a take came up short; the difference is paid for here.</summary>
    private static void OnShort(long sender, int request, int missing)
    {
        if (!_pending.TryGetValue(request, out Pending take))
        {
            return;
        }

        _pending.Remove(request);
        Player player = Player.m_localPlayer;

        if (!player || missing <= 0)
        {
            return;
        }

        Inventory own = player.GetInventory();
        int fromOwn = Mathf.Min(missing, own.CountItems(take.Name, take.Quality, take.WorldLevelBased));

        if (fromOwn > 0)
        {
            own.RemoveItem(take.Name, fromOwn, take.Quality, take.WorldLevelBased);
            missing -= fromOwn;
        }

        if (missing > 0)
        {
            missing -= StorageNetwork.Take(take.Purpose, take.Name, missing, take.Quality, take.WorldLevelBased);
        }

        if (missing > 0)
        {
            Log.LogInfo($"{missing} {take.Name} taken from a container were already gone, and nothing was left to pay them with.");
        }
    }

    private static void Forget()
    {
        if (_pending.Count == 0)
        {
            return;
        }

        List<int> stale = null;

        foreach (KeyValuePair<int, Pending> entry in _pending)
        {
            if (Time.time > entry.Value.Until)
            {
                (stale ??= new List<int>()).Add(entry.Key);
            }
        }

        if (stale != null)
        {
            foreach (int request in stale)
            {
                _pending.Remove(request);
            }
        }
    }

    /// <summary>Adds the items on the owner's game, dropping any that do not fit beside the container.</summary>
    private static void OnStore(long sender, ZDOID id, ZPackage package)
    {
        long origin = package.ReadLong();
        int hops = package.ReadInt();
        ZPackage body = package.ReadPackage();

        if (!Owned(StoreRpc, id, body, origin, hops, out Container container))
        {
            return;
        }

        GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(body.ReadString()) : null;
        int amount = body.ReadInt();
        bool cheated = body.ReadBool();

        if (!prefab || !prefab.TryGetComponent(out ItemDrop drop))
        {
            return;
        }

        Inventory inventory = container.GetInventory();
        int stored = inventory.HaveItem(drop.m_itemData.m_shared.m_name, false)
            ? Items.Add(inventory, prefab, amount, cheated)
            : 0;

        if (stored < amount)
        {
            Items.DropLoose(prefab, amount - stored, container.transform.position + Vector3.up, cheated);
        }
    }

    /// <summary>
    ///     The container, when this game owns it; otherwise the request is passed on. A request passed
    ///     back to the game that made it is dropped.
    /// </summary>
    private static bool Owned(string rpc, ZDOID id, ZPackage body, long origin, int hops, out Container container)
    {
        container = null;
        ZDO zdo = ZDOMan.instance.GetZDO(id);

        if (zdo == null)
        {
            Log.LogWarning($"{rpc}: container is gone.");
            return false;
        }

        ZNetView view = ZNetScene.instance ? ZNetScene.instance.FindInstance(zdo) : null;

        if (view && view.IsOwner())
        {
            if (origin == ZDOMan.GetSessionID())
            {
                return false;
            }

            container = view.GetComponentInChildren<Container>();
            return container && container.GetInventory() != null;
        }

        if (zdo.GetOwner() != ZDOMan.GetSessionID() && hops < MaxHops)
        {
            body.SetPos(0);
            Send(rpc, zdo, body, origin, hops + 1);
        }
        else
        {
            Log.LogWarning($"{rpc}: no owner could apply the change.");
        }

        return false;
    }
}
