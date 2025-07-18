using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Utilla.Behaviours;
using Utilla.Tools;

namespace Utilla
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
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

            new GameObject(Constants.Name, typeof(UtillaNetworkController), typeof(GamemodeManager));
        }
    }
}
