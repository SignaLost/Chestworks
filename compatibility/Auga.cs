using System;
using BepInEx.Bootstrap;
using HarmonyLib;
using Chestworks.Common;

namespace Chestworks.Compatibility;

/// <summary>Knows whether Auga is installed: its chest panel has no wood tabs, and its buttons keep their hexagon shape when copied for the lock button.</summary>
internal static class Auga
{
    internal const string Guid = "randyknapp.mods.auga";

    private static bool? _present;

    internal static bool Present => _present ??= Chainloader.PluginInfos.ContainsKey(Guid);

    /// <summary>Tells Auga to leave Chestworks' own interface pieces as they are drawn; Auga restyles what mods that depend on it make unless asked not to.</summary>
    internal static void KeepOwnLook(string pluginGuid)
    {
        if (!Present)
        {
            return;
        }

        try
        {
            Type api = AccessTools.TypeByName("Auga.API");
            AccessTools.Method(api, "AutoRestyle_Exclude", new[] { typeof(string) })?.Invoke(null, new object[] { pluginGuid });
        }
        catch (Exception e)
        {
            Log.LogWarning($"Auga could not be asked to leave the lock button alone: {e.Message}");
        }
    }
}
