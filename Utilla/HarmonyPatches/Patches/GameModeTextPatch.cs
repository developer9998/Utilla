using GorillaNetworking;
using HarmonyLib;
using Utilla.Behaviours;
using Utilla.Models;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.UpdateGameModeText))]
    internal class GameModeTextPatch
    {
        public static bool Prefix(GorillaComputer __instance)
        {
            if (NetworkSystem.Instance is null) return true;

            WatchableStringSO currentGameModeText = __instance.currentGameModeText;

            if (!NetworkSystem.Instance.InRoom)
            {
                currentGameModeText.Value = "CURRENT MODE\n-NOT IN ROOM-";
                return false;
            }

            Gamemode gamemode = UtillaNetworkController.Instance.CurrentGamemode;
            currentGameModeText.Value = $"CURRENT MODE\n{(gamemode is not null ? gamemode.DisplayName : "ERROR")}";

            return false;
        }
    }
}
