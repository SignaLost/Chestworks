using Chestworks.Common;

namespace Chestworks.Modules.Network;

/// <summary>Who applies a change to a container.</summary>
internal static class Ownership
{
    /// <summary>
    ///     Prepares a change to the container; returns true when its owner must apply it. Otherwise
    ///     this game owns the container afterward, taking it over on a vanilla server.
    /// </summary>
    internal static bool Prepare(Container container)
    {
        ZNetView view = container.m_nview;

        if (view.IsOwner())
        {
            return false;
        }

        if (ServerMod.Present)
        {
            return true;
        }

        long previous = view.GetZDO().GetOwner();
        view.ClaimOwnership();

        if (previous != 0)
        {
            ZDOMan.instance.ForceSendZDO(previous, view.GetZDO().m_uid);
        }

        return false;
    }
}
