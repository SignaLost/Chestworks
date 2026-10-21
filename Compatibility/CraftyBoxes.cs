using BepInEx.Bootstrap;
using Chestworks.Modules.Network;

namespace Chestworks.Compatibility;

/// <summary>Leaves every purpose to AzuCraftyBoxes while it is installed; storing is unaffected.</summary>
internal static class CraftyBoxes
{
    private const string Guid = "Azumatt.AzuCraftyBoxes";

    private static bool? _installed;

    /// <summary>Registers the deferral; the install check runs on first use.</summary>
    internal static void Register()
    {
        StorageNetwork.Defer(Guid, null, () => _installed ??= Chainloader.PluginInfos.ContainsKey(Guid));
    }
}
