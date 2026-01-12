using GorillaGameModes;
using HarmonyLib;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GameMode), nameof(GameMode.FindGameModeInString))]
    internal class GameModePatch
    {
        public static void Prefix(ref string gmString) => gmString = gmString.Replace("MODDED_", "");
    }
}
