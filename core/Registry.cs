using System.Collections.Generic;
using UnityEngine;

namespace Chestworks.Core;
/// <summary>
///     Tracks every loaded container that could join the storage network, registered as it wakes and dropped
///     when destroyed or found unloaded. Each entry caches the rigidbody and ship of the container's root, which
///     <see cref="Usability" /> uses to reject storage on moving carts and ships. Containers carried by a player are never registered.
/// </summary>
internal static class Registry
{
    internal sealed class Entry
    {
        internal Container Container;
        internal Rigidbody Body;
        internal Ship Ship;
    }

    private static readonly Dictionary<Container, Entry> _entries = new();
    private static readonly List<Container> _dead = new();

    internal static void Add(Container container)
    {
        // Backpack mods parent a container to the player.
        if (!container || _entries.ContainsKey(container) || container.GetComponentInParent<Player>())
        {
            return;
        }

        _entries[container] = Make(container);
    }

    internal static void Remove(Container container)
    {
        if (container)
        {
            _entries.Remove(container);
        }
    }

    /// <summary>Fills the list with the entries still loaded and drops the rest.</summary>
    internal static void Live(List<Entry> into)
    {
        into.Clear();

        foreach (KeyValuePair<Container, Entry> pair in _entries)
        {
            Container container = pair.Key;

            if (!container || !container.m_nview || !container.m_nview.IsValid())
            {
                _dead.Add(container);
            }
            else
            {
                into.Add(pair.Value);
            }
        }

        foreach (Container container in _dead)
        {
            _entries.Remove(container);
        }

        _dead.Clear();
    }

    /// <summary>The entry for a container, made if it is not registered; null for one a player carries.</summary>
    internal static Entry Of(Container container)
    {
        if (_entries.TryGetValue(container, out Entry entry))
        {
            return entry;
        }

        return container.GetComponentInParent<Player>() ? null : Make(container);
    }

    private static Entry Make(Container container)
    {
        ZNetView root = container.m_nview;
        return new Entry { Container = container, Body = root ? root.GetComponent<Rigidbody>() : null, Ship = root ? root.GetComponent<Ship>() : null };
    }
}
