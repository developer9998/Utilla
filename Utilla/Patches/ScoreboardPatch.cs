using HarmonyLib;
using UnityEngine;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaScoreboardSpawner), nameof(GorillaScoreboardSpawner))]
    internal class ScoreboardPatch
    {
        public static void Prefix(ref GameObject ___notInRoomText)
        {
            if (___notInRoomText == null || !___notInRoomText)
            {
                ___notInRoomText = new GameObject();
            }
        }
    }
}
