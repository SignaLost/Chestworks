using UnityEngine;
using Chestworks.Common;

namespace Chestworks.Modules.Network;
/// <summary>
///     Changes to a container another game controls, applied by that game. A request that reaches a
///     game which no longer owns the container is passed on to the new owner.
/// </summary>
internal static class ContainerRequests
{
    private const string TakeRpc = "Chestworks_ContainerTake";
    private const string StoreRpc = "Chestworks_ContainerStore";

    /// <summary>How often a request may be passed on.</summary>
    private const int MaxHops = 2;

    internal static void Register()
    {
        ZRoutedRpc.instance.Register<ZDOID, ZPackage>(TakeRpc, OnTake);
        ZRoutedRpc.instance.Register<ZDOID, ZPackage>(StoreRpc, OnStore);
    }

    internal static void Take(ZNetView view, string name, int amount, int quality, bool worldLevelBased)
    {
        ZPackage package = new();
        package.Write(name);
        package.Write(amount);
        package.Write(quality);
        package.Write(worldLevelBased);
        Send(TakeRpc, view.GetZDO(), package, ZDOMan.GetSessionID(), 0);
    }

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

        Inventory inventory = container.GetInventory();
        int held = inventory.CountItems(name, quality, worldLevelBased);
        inventory.RemoveItem(name, amount, quality, worldLevelBased);

        if (held < amount)
        {
            Log.LogWarning($"{container.name} held {held} of the {amount} {name} asked for.");
        }
    }

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
