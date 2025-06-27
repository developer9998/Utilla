using System;
using GorillaGameModes;
using GorillaNetworking;
using HarmonyLib;
using Utilla.Tools;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaNetworkJoinTrigger), nameof(GorillaNetworkJoinTrigger.GetDesiredGameType))]
    internal class DesiredGameModePatch
    {
        public static bool Prefix(GorillaNetworkJoinTrigger __instance, ref string __result)
        {
            if (__instance.GetType() == typeof(GorillaNetworkRankedJoinTrigger))
            {
                Logging.Info($"Joinng Ranked, Forcing to Ranked gameMode");
                __result = "InfectionCompetitive";
                return false;
            }
            else
            {
                var gameMode = GorillaComputer.instance.currentGameMode.Value;
                if (!Enum.IsDefined(typeof(GameModeType), gameMode))
                {
                    Logging.Info($"Join trigger returning non-defined desired game mode {gameMode}");

                    __result = gameMode;
                    return false;
                }
                return true;
            }
        }
    }
}