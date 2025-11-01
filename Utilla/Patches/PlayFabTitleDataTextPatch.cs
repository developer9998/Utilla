using HarmonyLib;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(PlayFabTitleDataTextDisplay), nameof(PlayFabTitleDataTextDisplay.Start))]
    internal class PlayFabTitleDataTextPatch
    {
        public static bool Prefix(PlayFabTitleDataTextDisplay __instance) => !__instance.playfabKey.Contains("COC");
    }
}
