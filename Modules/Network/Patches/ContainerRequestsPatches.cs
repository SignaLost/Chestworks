using HarmonyLib;
using UnityEngine;
using Chestworks.Common;

namespace Chestworks.Modules.Network.Patches;
[HarmonyPatch(typeof(Game), nameof(Game.Start))]
internal static class Game_Start_ContainerRequests_Patch
{
    private static void Postfix()
    {
        ContainerRequests.Register();
    }
}
