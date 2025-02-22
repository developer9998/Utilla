using BepInEx;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Utilla.HarmonyPatches;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private UtillaNetworkController _networkController;
        public static bool foundit;
        public void Start()
        {
            Logging.Logger = Logger;

            DontDestroyOnLoad(this);
            RoomUtils.RoomCode = RoomUtils.RandomString(6); // Generate a random room code in case we need it

            _networkController = gameObject.AddComponent<UtillaNetworkController>();

            Events.GameInitialized += PostInitialized;

            UtillaPatches.ApplyHarmonyPatches();

            foreach (var wawa in Resources.FindObjectsOfTypeAll<GameModeSelectorButtonLayout>())
            {
                wawa.GetOrAddComponent<UtillaGamemodeSelector>(out UtillaGamemodeSelector weewee);
            }
        }

        void Update()
        {
            if (!foundit)
            {
                foreach (var wawa in Resources.FindObjectsOfTypeAll<GameModeSelectorButtonLayout>())
                {
                    wawa.GetOrAddComponent<UtillaGamemodeSelector>(out UtillaGamemodeSelector weewee);
                }
            }
        }

        public void PostInitialized(object sender, EventArgs e)
        {
            Logging.Info("Game initialized");

            GameObject gameModeManagerObject = new(typeof(GamemodeManager).FullName, typeof(GamemodeManager));
            DontDestroyOnLoad(gameModeManagerObject);
            _networkController.gameModeManager = gameModeManagerObject.GetComponent<GamemodeManager>();
        }
    }
}
