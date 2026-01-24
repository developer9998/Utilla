using Fusion;
using HarmonyLib;

namespace Utilla.Patches;

[HarmonyPatch(typeof(GorillaGameManager)), HarmonyWrapSafe]
public class GameModePatches
{
    [HarmonyPatch("GameTypeName"), HarmonyPrefix]
    public static bool GameTypeNamePatch(GorillaGameManager __instance, ref string __result) {
        if (int.TryParse(__instance.GameType().ToString(), out _)) {
            __result = __instance.GameModeName();
            return false;
        }
        return true;
    }
}