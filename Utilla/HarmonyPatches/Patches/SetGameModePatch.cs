using GorillaNetworking;
using HarmonyLib;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.SetGameModeWithoutButton))]
    public static class SetGameModePatch
    {
        public static bool PreventSettingMode;

        [HarmonyPrefix]
        public static bool Prefix() => !PreventSettingMode;
    }
}