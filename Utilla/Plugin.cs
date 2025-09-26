using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Utilla.Behaviours;
using Utilla.Tools;

namespace Utilla
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    internal class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;

        public Plugin()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Constants.GUID);

            DontDestroyOnLoad(this);
        }

        public static void PostInitialized()
        {
            Logging.Message("PostInitialized");

            GameObject gameObject = new($"{Constants.Name} {Constants.Version}", typeof(UtillaNetworkController), typeof(GamemodeManager), typeof(ConductBoardManager));
            DontDestroyOnLoad(gameObject);
        }
    }
}
