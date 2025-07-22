using HarmonyLib;
using Utilla.Models;
using Utilla.Utils;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaScoreBoard), nameof(GorillaScoreBoard.RoomType)), HarmonyPriority(Priority.VeryHigh)]
    public class ScoreBoardRoomNamePatch
    {
        public static bool Prefix(ref string __result)
        {
            Gamemode gamemode = GameModeUtils.CurrentGamemode;
            __result = gamemode is not null ? gamemode.DisplayName.ToUpper() : GorillaScoreBoard.error;
            return false;
        }
    }
}
