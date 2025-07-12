using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Utilla.Behaviours;
using Utilla.HarmonyPatches;
using Utilla.Tools;

namespace Utilla
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;

        public Plugin()
        {
            Logger = base.Logger;

            UtillaPatches.ApplyHarmonyPatches();

            DontDestroyOnLoad(this);
        }

        public static void PostInitialized()
        {
            Logging.Message("PostInitialized");

            new GameObject(Constants.Name, typeof(UtillaNetworkController), typeof(GamemodeManager));
        }
    }
}
