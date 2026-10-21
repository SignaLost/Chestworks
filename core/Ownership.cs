using Chestworks.Common;

namespace Chestworks.Core;

/// <summary>Decides which game applies a change to a container's inventory.</summary>
internal static class Ownership
{
    /// <summary>
    ///     Prepares a local change to the container. Returns true when another game owns it on a modded server;
    ///     the caller must then send the same change to the owner through <see cref="ContainerRequests" />.
    ///     Otherwise this game owns the container afterward, claiming it from the previous owner on a vanilla server.
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
