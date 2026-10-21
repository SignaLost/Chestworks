using UnityEngine;

namespace Chestworks.Modules.Network;

/// <summary>Adding and dropping items the way the game does.</summary>
internal static class Items
{
    /// <summary>Adds up to the amount to an inventory in stacks the game would make, never past its free space; returns what was added.</summary>
    internal static int Add(Inventory inventory, GameObject prefab, int amount, bool cheated)
    {
        ItemDrop.ItemData template = prefab.GetComponent<ItemDrop>().m_itemData;
        string name = template.m_shared.m_name;
        int maxStack = Mathf.Max(1, template.m_shared.m_maxStackSize);
        int remaining = Mathf.Min(amount, Space(inventory, template));
        int before = inventory.CountItems(name, -1, false);

        while (remaining > 0)
        {
            ItemDrop.ItemData item = template.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = Mathf.Min(remaining, maxStack);
            item.m_worldLevel = (byte)Game.m_worldLevel;
            item.m_cheated = cheated;
            remaining -= item.m_stack;

            if (!inventory.AddItem(item))
            {
                break;
            }
        }

        return inventory.CountItems(name, -1, false) - before;
    }

    /// <summary>Room for the item: free space in its stacks plus empty slots.</summary>
    internal static int Space(Inventory inventory, ItemDrop.ItemData template)
    {
        int maxStack = Mathf.Max(1, template.m_shared.m_maxStackSize);
        int freeSlots = inventory.GetWidth() * inventory.GetHeight() - inventory.GetAllItems().Count;
        return inventory.FindFreeStackSpace(template.m_shared.m_name, Game.m_worldLevel) + freeSlots * maxStack;
    }

    /// <summary>Drops loose items at the position in stacks no larger than the item's maximum.</summary>
    internal static void DropLoose(GameObject prefab, int count, Vector3 position, bool cheated)
    {
        while (count > 0)
        {
            ItemDrop drop = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<ItemDrop>();
            drop.SetStack(Mathf.Min(count, drop.m_itemData.m_shared.m_maxStackSize));
            ItemDrop.OnCreateNew(drop);
            drop.m_itemData.m_cheated = cheated;
            count -= drop.m_itemData.m_stack;
        }
    }
}
