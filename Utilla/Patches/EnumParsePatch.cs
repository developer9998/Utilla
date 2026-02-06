using System;
using HarmonyLib;

using GorillaGameModes;

namespace Utilla.Patches;

[HarmonyPatch(typeof(Enum), nameof(Enum.Parse), argumentTypes: [typeof(Type), typeof(string), typeof(bool)])]
internal class EnumParsePatch
{
    public static bool Prefix(Type enumType, string value, ref object __result)
    {
        if (enumType == typeof(GameModeType))
        {
            __result = Enum.TryParse<GameModeType>(value, out var result) ? result : GameModeType.Casual;
            return false;
        }

        return true;
    }
}
