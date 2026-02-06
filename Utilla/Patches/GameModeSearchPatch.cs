using HarmonyLib;
using GorillaGameModes;
using Utilla.Utils;
using Utilla.Models;
using Utilla.Tools;

namespace Utilla.Patches;

[HarmonyPatch(typeof(GameMode), nameof(GameMode.FindGameModeInString))]
internal class GameModeSearchPatch
{
    public static bool Prefix(string gmString, ref string __result)
    {
        if (GameModeUtils.FindGamemodeInString(gmString) is Gamemode gamemode)
        {
            __result = gamemode.ID;
            return false;
        }

        Logging.Error("NOT GOOD");
        return true;
    }
}
