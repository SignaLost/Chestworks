using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Chestworks.Compatibility;
using Chestworks.Common;

namespace Chestworks;

/// <summary>
///     The BepInEx entry point for Chestworks, a library mod that gives other mods a shared storage network:
///     which containers are in reach for a purpose, and multiplayer-safe ways to count, take and store items in them.
///     On load it sets up logging, registers the AzuCraftyBoxes deferral and applies every Harmony patch class
///     on its own, so one failing patch leaves the rest in place.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
[BepInProcess("valheim.x86_64")]
[BepInProcess("valheim_server.exe")]
[BepInProcess("valheim_server.x86_64")]
[BepInDependency(Jotunn.Main.ModGuid)]
// Must load before this plugin when present.
[BepInDependency("seneaL.valheim.ui", BepInDependency.DependencyFlags.SoftDependency)]
// A modded server requires the mod on every client; a vanilla server does not.
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public sealed class ChestworksPlugin : BaseUnityPlugin
{
    public const string PluginName = "Chestworks";
    public const string PluginGuid = "NullSignal.Chestworks";
    public const string PluginVersion = "1.0.0";

    private const string SecGeneral = "1. General";

    private readonly Harmony _harmony = new(PluginGuid);

    private void Awake()
    {
        Log.Init(Logger);

        Log.Verbosity = Config.Bind(SecGeneral, "Verbosity", Log.InfoLevel.Low,
            "How much this mod writes to the log. [Not Synced with Server]");

        CraftyBoxes.Register();
        ApplyPatches();

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }

    /// <summary>Patches each patch class separately, continuing past failures.</summary>
    private void ApplyPatches()
    {
        int applied = 0;
        int failed = 0;

        foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                PatchClassProcessor processor = _harmony.CreateClassProcessor(type);

                if (processor.Patch() is { Count: > 0 })
                {
                    applied++;
                }
            }
            catch (Exception e)
            {
                failed++;
                Logger.LogError($"Patch failed: {type.Name} - {e.Message}");
            }
        }

        Logger.LogInfo($"Applied {applied} patch classes" + (failed > 0 ? $", {failed} failed." : "."));
    }
}
