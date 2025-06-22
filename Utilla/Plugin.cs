using BepInEx;
using UnityEngine;
using Utilla.Behaviours;
using Utilla.HarmonyPatches;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public Plugin()
        {
            Logging.Logger = Logger;
            UtillaPatches.ApplyHarmonyPatches();
            DontDestroyOnLoad(this);
            RoomUtils.RoomCode = RoomUtils.RandomString(6); // Generate a random room code in case we need it
        }

        public static void PostInitialized()
        {
            new GameObject(Constants.Name, typeof(UtillaNetworkController), typeof(GamemodeManager));
        }
    }
}
