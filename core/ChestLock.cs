using BepInEx.Bootstrap;
using Chestworks.Common;
using Chestworks.Compatibility;

namespace Chestworks.Core;

/// <summary>
///     A chest a player has locked is left alone: nothing is taken from it or stored in it for any purpose. The lock
///     is saved on the chest itself, so everyone on the server sees the same one. SeneaL UI's own exclusion, set from
///     its chest header, counts as a lock too, so a chest excluded there stays untouched for players without it.
/// </summary>
internal static class ChestLock
{
    private const string SeneaLGuid = "seneaL.valheim.ui";

    private static readonly int LockedKey = "Chestworks.Locked".GetStableHashCode();
    private static readonly int SeneaLKey = "seneal.nocraft".GetStableHashCode();

    private static bool? _seneaL;

    /// <summary>Whether SeneaL UI is installed, whose chest header has an exclusion button of its own.</summary>
    internal static bool SeneaLPresent => _seneaL ??= Chainloader.PluginInfos.ContainsKey(SeneaLGuid);

    internal static bool IsLocked(Container container)
    {
        ZDO zdo = container && container.m_nview ? container.m_nview.GetZDO() : null;
        return zdo != null && (zdo.GetInt(LockedKey) == 1 || zdo.GetString(SeneaLKey) == "1");
    }

    /// <summary>Locks or unlocks the chest; unlocking also clears SeneaL UI's exclusion, so the chest really is free again.</summary>
    internal static void Set(Container container, bool locked)
    {
        ZNetView view = container ? container.m_nview : null;
        ZDO zdo = view ? view.GetZDO() : null;

        if (zdo == null)
        {
            return;
        }

        if (!view.IsOwner())
        {
            view.ClaimOwnership();
        }

        zdo.Set(LockedKey, locked ? 1 : 0);

        if (!locked && zdo.GetString(SeneaLKey) == "1")
        {
            zdo.Set(SeneaLKey, "");
        }

        StorageNetwork.Bump();
        Log.LogInfo($"{container.name} {(locked ? "locked" : "unlocked")}.");
    }

    /// <summary>The gray "( Locked )" put after a locked chest's name, spaced like the game's "( Empty )" and in SeneaL UI's muted color when it is themed.</summary>
    internal static string LockedMark()
    {
        return "<color=" + SeneaLTheme.MutedHex("#9a9a9a") + ">( " + Names.Localize(Tokens.Locked) + " )</color>";
    }
}
