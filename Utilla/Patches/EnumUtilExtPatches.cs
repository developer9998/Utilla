using System.Reflection;
using GorillaGameModes;
using HarmonyLib;
using Utilla.Utils;

namespace Utilla.Patches;

[HarmonyPatch]
public class EnumUtilExtPatches
{
    public static MethodBase TargetMethod()
    {
        return typeof(EnumUtilExt)
            .GetMethod(nameof(EnumUtilExt.GetName), BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(GameModeType));
    }
    
    public static bool Prefix(GameModeType e, ref string __result) {
        if (int.TryParse(e.ToString(), out _)) {
            string a = GameModeUtils.GetGameModeInstance(e).GameTypeName();
            __result = a;
            return false;
        }

        return true;
    }
}