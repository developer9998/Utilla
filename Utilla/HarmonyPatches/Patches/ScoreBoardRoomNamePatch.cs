using HarmonyLib;
using Utilla.Behaviours;
using Utilla.Models;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaScoreBoard), nameof(GorillaScoreBoard.RoomType)), HarmonyPriority(Priority.VeryHigh)]
    public class ScoreBoardRoomNamePatch
    {
        public static bool Prefix(ref string __result)
        {
            Gamemode gamemode = UtillaNetworkController.Instance.CurrentGamemode;
            __result = gamemode is not null ? gamemode.DisplayName : "ERROR";
            return false;
        }
    }
}
