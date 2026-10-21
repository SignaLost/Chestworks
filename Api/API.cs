using System;
using System.Collections.Generic;
using UnityEngine;
using Chestworks.Modules.Network;

namespace Chestworks;

/// <summary>
///     The Chestworks storage network for other mods, for the local player and the main thread only.
///     Before a world loads, queries return nothing and changes do nothing.
/// </summary>
public static class API
{
    public const int Version = 1;

    public const string Guid = ChestworksPlugin.PluginGuid;

    // ---- Setting up a purpose ----

    /// <summary>
    ///     Sets which containers a purpose reaches; a second mod registering the same purpose is refused.
    ///     The rules are called at most once a frame and return null while the purpose reaches nothing.
    /// </summary>
    public static void RegisterReach(string purpose, string ownerGuid, Func<Player, ReachRules> rules)
    {
        if (!string.IsNullOrEmpty(purpose) && !string.IsNullOrEmpty(ownerGuid) && rules != null)
        {
            StorageNetwork.RegisterReach(purpose, ownerGuid, rules);
        }
    }

    public static void UnregisterReach(string purpose, string ownerGuid)
    {
        if (!string.IsNullOrEmpty(purpose) && !string.IsNullOrEmpty(ownerGuid))
        {
            StorageNetwork.UnregisterReach(purpose, ownerGuid);
        }
    }

    /// <summary>While <paramref name="active" /> returns true, the purpose, or every purpose for null, reaches nothing.</summary>
    public static void DefersTo(string modGuid, string purpose, Func<bool> active)
    {
        if (!string.IsNullOrEmpty(modGuid) && active != null)
        {
            StorageNetwork.Defer(modGuid, purpose, active);
        }
    }

    /// <summary>Marks a mod as showing <see cref="Stock" /> in its own interface, which drops any compatibility patch for it; call once at startup.</summary>
    public static void ShowsStock(string modGuid)
    {
        if (!string.IsNullOrEmpty(modGuid))
        {
            StorageNetwork.ShowsStock.Add(modGuid);
        }
    }

    // ---- Reading ----

    /// <summary>Whether the purpose reaches containers right now: registered, its rules allow it, and no other mod has taken it over.</summary>
    public static bool IsActive(string purpose)
    {
        return StorageNetwork.IsActive(purpose);
    }

    /// <summary>Rises whenever stock may have changed, including moving away from containers or a purpose turning off.</summary>
    public static int Revision => StorageNetwork.Revision;

    /// <summary>
    ///     How much of an item, by shared name, the purpose's containers hold, without the inventory.
    ///     Cached for the frame; 0 when the purpose is not active.
    /// </summary>
    public static int Stock(string purpose, string item, int quality = -1)
    {
        return string.IsNullOrEmpty(item) ? 0 : StorageNetwork.Count(purpose, item, quality);
    }

    /// <summary>The purpose's containers, nearest its point first, added to the list.</summary>
    public static void GetContainers(string purpose, List<Container> into)
    {
        if (into == null)
        {
            return;
        }

        foreach (Container container in StorageNetwork.Available(purpose))
        {
            if (container)
            {
                into.Add(container);
            }
        }
    }

    /// <summary>Whether the purpose's containers hold cheated items of these requirements.</summary>
    public static bool HasCheated(string purpose, Piece.Requirement[] resources, int quality = -1, bool matchWorldLevel = true)
    {
        return resources != null && StorageNetwork.Cheated(purpose, resources, quality, matchWorldLevel);
    }

    // ---- Changing ----

    /// <summary>Takes up to the amount from the purpose's containers, nearest first, and returns what was taken. The inventory is not touched.</summary>
    public static int Take(string purpose, string item, int amount, int quality = -1, bool worldLevelBased = true)
    {
        if (string.IsNullOrEmpty(item) || amount <= 0)
        {
            return 0;
        }

        return StorageNetwork.Take(purpose, item, amount, quality, worldLevelBased);
    }

    /// <summary>
    ///     Stores up to the amount of an item prefab in the purpose's containers that already hold it, nearest the
    ///     point first, never in <paramref name="exclude" />. Returns what was stored; works while another mod has taken the purpose over.
    /// </summary>
    public static int Store(string purpose, Vector3 point, GameObject prefab, int amount, bool cheated = false, Piece exclude = null)
    {
        return amount <= 0 ? 0 : StorageNetwork.Store(purpose, point, prefab, amount, cheated, exclude);
    }

    /// <summary>
    ///     Adds up to the amount of an item prefab to an inventory in the stacks the game would make, never past
    ///     its free space, and returns what was added. Carry weight is not checked; the caller must own a container's inventory.
    /// </summary>
    public static int AddItems(Inventory inventory, GameObject prefab, int amount, bool cheated = false)
    {
        if (inventory == null || !prefab || !prefab.GetComponent<ItemDrop>() || amount <= 0)
        {
            return 0;
        }

        return Items.Add(inventory, prefab, amount, cheated);
    }

    // ---- Protection ----

    /// <summary>Whether the container is marked open by the game controlling it, when that is another player's.</summary>
    public static bool IsOpenElsewhere(Container container)
    {
        return Usability.OpenElsewhere(container);
    }

    /// <summary>
    ///     Whether the local player may draw on the container: player-built, allowed by its privacy and wards, not
    ///     open elsewhere, and not on a moving or pulled cart or ship. Reach is not considered.
    /// </summary>
    public static bool IsUsable(Container container)
    {
        if (!container || !container.m_nview || !container.m_nview.IsValid() || !Game.instance)
        {
            return false;
        }

        Registry.Entry entry = Registry.Of(container);
        return entry != null && Usability.Usable(entry, Game.instance.GetPlayerProfile().GetPlayerID());
    }

    /// <summary>Whether another player is using the piece's storage: a container in it is open elsewhere, or its cart is pulled by someone else.</summary>
    public static bool IsStorageInUse(Piece piece)
    {
        return piece && Usability.StorageInUse(piece);
    }
}
