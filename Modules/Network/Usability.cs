using System.Collections.Generic;
using BepInEx.Bootstrap;
using UnityEngine;
using Chestworks.Common;

namespace Chestworks.Modules.Network;

/// <summary>Which containers may be drawn on, and which storage another player is using.</summary>
internal static class Usability
{
    private static readonly List<Container> _containers = new();

    private const string MultiUserChestGuid = "com.maxsch.valheim.MultiUserChest";

    /// <summary>Horizontal speed above which a cart or ship counts as moving.</summary>
    private const float MovingSpeed = 0.5f;

    private static bool? _sharedChests;

    /// <summary>Whether MultiUserChest is installed, which lets a container open elsewhere stay usable.</summary>
    private static bool SharedChests => _sharedChests ??= Chainloader.PluginInfos.ContainsKey(MultiUserChestGuid);

    /// <summary>
    ///     Whether the container is player-built, open to this player by the game's own checks, not open
    ///     elsewhere unless shared, and not on a cart or ship that is moving or in use.
    /// </summary>
    internal static bool Usable(Registry.Entry entry, long playerId)
    {
        Container container = entry.Container;
        ZDO zdo = container.m_nview.GetZDO();

        if (container.GetInventory() == null || zdo.GetLong(ZDOVars.s_creator) == 0)
        {
            return false;
        }

        if (container.IsInUse() || (OpenElsewhere(container) && !(ServerMod.Present && SharedChests))
            || (container.m_wagon && container.m_wagon.InUse()))
        {
            return false;
        }

        if (entry.Ship && (entry.Ship.HaveControllingPlayer() || entry.Ship.GetSpeedSetting() != Ship.Speed.Stop))
        {
            return false;
        }

        // Velocity is only reliable on the owner's machine.
        if (entry.Body && !entry.Body.isKinematic)
        {
            Vector3 velocity = entry.Body.linearVelocity;

            if (velocity.x * velocity.x + velocity.z * velocity.z > MovingSpeed * MovingSpeed)
            {
                return false;
            }
        }

        // The game's CheckAccess reads the piece's creator for private chests.
        if (container.m_privacy == Container.PrivacySetting.Private && !container.m_piece)
        {
            return false;
        }

        if (!container.CheckAccess(playerId))
        {
            return false;
        }

        return !container.m_checkGuardStone || PrivateArea.CheckAccess(container.transform.position, 0f, false);
    }

    /// <summary>Whether the container is marked open by the game controlling it, when that is not this one.</summary>
    internal static bool OpenElsewhere(Container container)
    {
        ZNetView view = container ? container.m_nview : null;
        return view && view.IsValid() && !view.IsOwner() && view.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
    }

    /// <summary>Whether a container in the piece is open elsewhere, or its cart is pulled by another player.</summary>
    internal static bool StorageInUse(Piece piece)
    {
        piece.GetComponentsInChildren(_containers);

        foreach (Container container in _containers)
        {
            if (OpenElsewhere(container))
            {
                _containers.Clear();
                return true;
            }
        }

        _containers.Clear();

        // Cart attachment is synced to every machine.
        Vagon cart = piece.GetComponentInChildren<Vagon>();
        Player self = Player.m_localPlayer;
        return cart && cart.IsAttached() && !(self && cart.IsAttached(self));
    }
}
