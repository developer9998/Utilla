using HarmonyLib;
using UnityEngine;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaLevelScreen), nameof(GorillaLevelScreen.UpdateText))]
    internal class GorillaLevelScreenPatch
    {
        public static bool Prefix(GorillaLevelScreen __instance) => __instance.GetComponent<MeshRenderer>();
    }
}
