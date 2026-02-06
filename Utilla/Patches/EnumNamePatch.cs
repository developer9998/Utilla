using GorillaGameModes;
using HarmonyLib;
using Utilla.Utils;

namespace Utilla.Patches;

[HarmonyPatch(typeof(EnumUtilExt), nameof(EnumUtilExt.GetName))]
internal class EnumNamePatch
{
    public static bool Prefix(object TEnum, ref string __result)
    {
        if (TEnum.GetType() == typeof(GameModeType))
        {
            __result = EnumData<GameModeType>.Shared.EnumToName.TryGetValue((GameModeType)TEnum, out string result) ? result : GameModeUtils.GetGameModeName((GameModeType)TEnum);
            return false;
        }

        return true;
    }
}
