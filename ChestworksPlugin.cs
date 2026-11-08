using System;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using Chestworks.Compatibility;
using Chestworks.Common;
using Chestworks.Patch;
using UnityEngine;

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
[BepInDependency(Compatibility.Auga.Guid, BepInDependency.DependencyFlags.SoftDependency)]
// A modded server requires the mod on every client; a vanilla server does not.
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public sealed class ChestworksPlugin : BaseUnityPlugin
{
    public const string PluginName = "Chestworks";
    public const string PluginGuid = "NullSignal.Chestworks";
    public const string PluginVersion = "1.1.1";

    private const string SecGeneral = "1. General";
    private const string EnglishFile = "chestworks.json";
    private const string EnglishResource = "Chestworks.Translations.English.json";

    /// <summary>Items of each kind that stay in the chests, never counted or taken.</summary>
    internal static ConfigEntry<int> Keep;

    internal static ConfigEntry<LockButtonStyle> LockButtonStyle;
    internal static ConfigEntry<LockButtonAnchor> LockButtonAnchor;
    internal static ConfigEntry<Vector2> LockButtonOffset;
    internal static ConfigEntry<LockTabAnchor> LockTabAnchor;
    internal static ConfigEntry<Vector2> LockTabOffset;

    private readonly Harmony _harmony = new(PluginGuid);
    private ConfigWatcher _configWatcher;

    private void Awake()
    {
        Log.Init(Logger);
        Compatibility.Auga.KeepOwnLook(PluginGuid);

        Log.Verbosity = Config.Bind(SecGeneral, "Verbosity", Log.InfoLevel.Low,
            "How much this mod writes to the log. [Not Synced with Server]");
        Keep = Config.Bind(SecGeneral, "Keep In Chests", 0,
            new ConfigDescription("How many of each item mods always leave in your chests. [Not Synced with Server]", new AcceptableValueRange<int>(0, 1000)));
        LockButtonStyle = Config.Bind(SecGeneral, "Lock Button Style", Common.LockButtonStyle.Button,
            "How the padlock looks: a square copy of the Take All button, or a tab like the chest's weight tab. [Not Synced with Server]");
        LockButtonAnchor = Config.Bind(SecGeneral, "Button Anchor", Common.LockButtonAnchor.TakeAll,
            "With the Button style, what it sits against: just past the Take All or Place Stacks button, or inside a corner of the chest panel. [Not Synced with Server]");
        LockButtonOffset = Config.Bind(SecGeneral, "Button Offset", Vector2.zero,
            "With the Button style, how far it moves from its anchor, in pixels, right and up. [Not Synced with Server]");
        LockTabAnchor = Config.Bind(SecGeneral, "Tab Anchor", Common.LockTabAnchor.RightTop,
            "With the Tab style, which edge of the chest panel it hangs from and where along it, the way the weight tab hangs from the right. [Not Synced with Server]");
        LockTabOffset = Config.Bind(SecGeneral, "Tab Offset", Vector2.zero,
            "With the Tab style, how far it moves from its anchor, in pixels, right and up. [Not Synced with Server]");
        LockButtonStyle.SettingChanged += (_, _) => InventoryGui_UpdateContainer_Lock_Patch.Refresh();
        LockButtonAnchor.SettingChanged += (_, _) => InventoryGui_UpdateContainer_Lock_Patch.Refresh();
        LockButtonOffset.SettingChanged += (_, _) => InventoryGui_UpdateContainer_Lock_Patch.Refresh();
        LockTabAnchor.SettingChanged += (_, _) => InventoryGui_UpdateContainer_Lock_Patch.Refresh();
        LockTabOffset.SettingChanged += (_, _) => InventoryGui_UpdateContainer_Lock_Patch.Refresh();

        _configWatcher = new ConfigWatcher(Config);
        RegisterEnglish();
        CraftyBoxes.Register();
        ApplyPatches();

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        _configWatcher?.Tick();
    }

    private void OnDestroy()
    {
        _configWatcher?.Dispose();
        _harmony?.UnpatchSelf();
    }

    /// <summary>Adds the embedded English translations when no <c>Translations/English/chestworks.json</c> is on disk for Jotunn to load.</summary>
    private void RegisterEnglish()
    {
        string onDisk = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "Translations", "English", EnglishFile);

        if (File.Exists(onDisk))
        {
            return;
        }

        byte[] bytes = EmbeddedResources.Read(EnglishResource);

        if (bytes != null)
        {
            LocalizationManager.Instance.GetLocalization().AddJsonFile("English", Encoding.UTF8.GetString(bytes));
        }
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
