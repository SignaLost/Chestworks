using Chestworks.Common;
using Chestworks.Core;
using HarmonyLib;
using UnityEngine;

namespace Chestworks.Patch;
/// <summary>Registers the container request RPCs when a game starts.</summary>
[HarmonyPatch(typeof(Game), nameof(Game.Start))]
internal static class Game_Start_ContainerRequests_Patch
{
    private static void Postfix()
    {
        ContainerRequests.Register();
    }
}
