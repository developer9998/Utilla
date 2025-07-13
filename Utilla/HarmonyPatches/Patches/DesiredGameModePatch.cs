using GorillaGameModes;
using GorillaNetworking;
using HarmonyLib;
using System;
using System.Linq;
using Utilla.Models;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaNetworkJoinTrigger), nameof(GorillaNetworkJoinTrigger.GetDesiredGameType))]
    internal class DesiredGameModePatch
    {
        public static bool Prefix(GorillaNetworkJoinTrigger __instance, ref string __result)
        {
            string currentGameMode = GorillaComputer.instance.currentGameMode.Value;

            /*
            if (GameModeUtils.GetGamemodeFromId(currentGameMode) is Gamemode gamemode && gamemode.BaseGamemode.HasValue && gamemode.BaseGamemode.Value < GameModeType.Count)
            {
                GameModeType gameModeType = gamemode.BaseGamemode.Value;
                Logging.Info($"{currentGameMode} has type {gameModeType}");

                GTZone zone = __instance.zone;
                bool isPrivate = NetworkSystem.Instance.SessionIsPrivate;

                GameModeType verifiedGameMode = GameMode.GameModeZoneMapping.VerifyModeForZone(zone, gameModeType, isPrivate);
                if (gameModeType == verifiedGameMode)
                {
                    Logging.Info($"Mode supported for {__instance.zone}");
                    __result = currentGameMode;
                    return false;
                }

                Logging.Info($"Mode unsupported for {__instance.zone}");
                GameModeType fallbackGameMode = GameMode.GameModeZoneMapping.GetModesForZone(zone, isPrivate).First();
                Logging.Log($"Fallback game mode: {fallbackGameMode}");
                GorillaComputer.instance.SetGameModeWithoutButton(fallbackGameMode.ToString());
                return false;
            }
            */

            if (!Enum.IsDefined(typeof(GameModeType), currentGameMode))
            {
                __result = currentGameMode;
                Logging.Info($"Join trigger returning non-defined desired game mode {currentGameMode}");
                return false;
            }

            return true;
        }
    }
}
