using GorillaGameModes;
using GorillaNetworking;
using HarmonyLib;
using UnityEngine;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.InitializeGameMode), argumentTypes: [])]
    internal class InitializeGamemodePatch
    {
        internal static bool Prefix(GorillaComputer __instance)
        {
            if (__instance.didInitializeGameMode) return false;

            string text = PlayerPrefs.GetString("currentGameMode", GameModeType.Infection.ToString());

            __instance.leftHanded = PlayerPrefs.GetInt("leftHanded", 0) == 1;
            __instance.OnModeSelectButtonPress(text, __instance.leftHanded);
            // GameModePages.SetSelectedGameModeShared(text);
            __instance.didInitializeGameMode = true;

            return false;
        }
    }
}
