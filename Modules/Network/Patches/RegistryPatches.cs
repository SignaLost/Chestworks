using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Chestworks.Modules.Network.Patches;
/// <summary>Registers containers as they load.</summary>
[HarmonyPatch(typeof(Container), "Awake")]
internal static class Container_Awake_Registry_Patch
{
    private static void Postfix(Container __instance)
    {
        if (__instance.m_nview && __instance.m_nview.GetZDO() != null)
        {
            Registry.Add(__instance);
        }
    }
}

/// <summary>Drops containers as they are destroyed; unloaded ones are dropped at the next scan.</summary>
[HarmonyPatch(typeof(Container), "OnDestroyed")]
internal static class Container_OnDestroyed_Registry_Patch
{
    private static void Prefix(Container __instance)
    {
        Registry.Remove(__instance);
    }
}

/// <summary>Marks stock as changed when contents arrive from another game.</summary>
[HarmonyPatch(typeof(Container), "Load")]
internal static class Container_Load_Revision_Patch
{
    private static void Postfix(bool __result)
    {
        if (__result)
        {
            StorageNetwork.Bump();
        }
    }
}

/// <summary>Marks stock as changed when contents change on this game.</summary>
[HarmonyPatch(typeof(Container), "OnContainerChanged")]
internal static class Container_OnContainerChanged_Revision_Patch
{
    private static void Postfix()
    {
        StorageNetwork.Bump();
    }
}
