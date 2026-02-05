// https://github.com/Not-A-Bird-07/Utilla/commit/c813503da35b39e63290a776af447e16a88d64c5

using HarmonyLib;

namespace Utilla.Patches;

[HarmonyPatch(typeof(GorillaGameManager)), HarmonyWrapSafe]
internal class GameModePatches
{
    [HarmonyPatch("GameTypeName"), HarmonyPrefix]
    public static bool GameTypeNamePatch(GorillaGameManager __instance, ref string __result)
    {
        if (int.TryParse(__instance.GameType().ToString(), out _))
        {
            __result = __instance.GameModeName();
            return false;
        }
        return true;
    }
}