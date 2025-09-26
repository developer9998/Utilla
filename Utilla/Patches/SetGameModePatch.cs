using GorillaNetworking;
using HarmonyLib;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.SetGameModeWithoutButton))]
    internal static class SetGameModePatch
    {
        public static bool PreventSettingMode;

        [HarmonyPrefix]
        public static bool Prefix() => !PreventSettingMode;
    }
}