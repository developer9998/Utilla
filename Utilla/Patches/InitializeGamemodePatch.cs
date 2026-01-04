using GorillaGameModes;
using GorillaNetworking;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using System;
using System.Linq;

namespace Utilla.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.InitializeGameMode), argumentTypes: [])]
    internal class InitializeGamemodePatch
    {
        public static string GameModeKey { get; private set; }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (GameModeKey != null && GameModeKey.Length > 0) return instructions;

            CodeInstruction[] codes = [.. instructions];

            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    object operand = codes[i].operand;
                    Type opType = operand.GetType();

                    string methodName = (string)AccessTools.Property(opType, "Name").GetValue(operand);
                    Type returnType = (Type)AccessTools.Property(opType, "ReturnType").GetValue(operand);
                    Type declaringType = (Type)AccessTools.Property(opType, "DeclaringType").GetValue(operand);

                    if (methodName != "GetString" || returnType != typeof(string) || declaringType != typeof(PlayerPrefs)) continue;

                    CodeInstruction code = codes.Take(i + 1).LastOrDefault(code => code.opcode == OpCodes.Ldstr);
                    if (code != null)
                    {
                        GameModeKey = (string)code.operand;
                        break;
                    }
                }
            }

            return codes;
        }

        internal static bool Prefix(GorillaComputer __instance)
        {
            if (!__instance.didInitializeGameMode)
            {
                string gameMode = PlayerPrefs.GetString(GameModeKey, GameModeType.Infection.ToString());

                GorillaComputer.sessionCount = 100;
                __instance.leftHanded = PlayerPrefs.GetInt("leftHanded", 0) == 1;
                __instance.OnModeSelectButtonPress(gameMode, __instance.leftHanded);
                GameModePages.SetSelectedGameModeShared(gameMode);

                __instance.didInitializeGameMode = true;
            }

            return false;
        }
    }
}
