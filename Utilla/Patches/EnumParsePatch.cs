using GorillaGameModes;
using HarmonyLib;
using System;

namespace Utilla.Patches;

[HarmonyPatch(typeof(Enum), nameof(Enum.Parse), argumentTypes: [typeof(Type), typeof(string), typeof(bool)])]
internal class EnumParsePatch
{
    public static bool Prefix(Type enumType, string value, ref object __result)
    {
        if (enumType == typeof(GameModeType))
        {
            EnumData<GameModeType> shared = EnumData<GameModeType>.Shared;
            __result = shared.NameToEnum.TryGetValue(value, out var gameMode) ? gameMode : GameModeType.Casual;
            return false;
        }

        return true;
    }
}
