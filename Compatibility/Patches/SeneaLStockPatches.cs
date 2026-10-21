using System;
using System.Reflection;
using HarmonyLib;
using Chestworks.Modules.Network;
using Chestworks.Common;

namespace Chestworks.Compatibility.Patches;
/// <summary>Adds the purpose's stock to the "have" count on SeneaL UI's panels.</summary>
[HarmonyPatch]
internal static class SeneaL_Stock_Count_Patch
{
    private static bool Prepare()
    {
        SeneaLStock.Register();
        return SeneaLStock.Type != null;
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(SeneaLStock.Type, "Count", new[] { typeof(Player), typeof(string), typeof(int) });
    }

    private static void Postfix(Player p, string name, int quality, ref int __result)
    {
        string purpose = p == Player.m_localPlayer ? SeneaLStock.Counting() : null;

        if (purpose != null)
        {
            __result += StorageNetwork.Count(purpose, name, quality);
        }
    }
}

/// <summary>Adds the purpose's stock to the panels' "from chests" count.</summary>
[HarmonyPatch]
internal static class SeneaL_Stock_FromChests_Patch
{
    private static bool Prepare()
    {
        return SeneaLStock.Type != null;
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(SeneaLStock.Type, "FromChests", new[] { typeof(Player), typeof(string) });
    }

    private static void Postfix(Player p, string name, ref int __result)
    {
        string purpose = p == Player.m_localPlayer ? SeneaLStock.Counting() : null;

        if (purpose != null)
        {
            __result += StorageNetwork.Count(purpose, name);
        }
    }
}

/// <summary>Marks a material as at hand for the panels' lists when the purpose's stock holds it.</summary>
[HarmonyPatch]
internal static class SeneaL_Stock_Have_Patch
{
    private static bool Prepare()
    {
        return SeneaLStock.Type != null;
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(SeneaLStock.Type, "Have", new[] { typeof(Player), typeof(string) });
    }

    private static void Postfix(Player p, string name, ref bool __result)
    {
        string purpose = !__result && p == Player.m_localPlayer ? SeneaLStock.Counting() : null;

        if (purpose != null)
        {
            __result = StorageNetwork.Count(purpose, name) > 0;
        }
    }
}
