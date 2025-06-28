using GorillaGameModes;
using System;
using Utilla.Behaviours;
using Utilla.Models;

namespace Utilla.Utils
{
    public static class GameModeUtils
    {
        public static Gamemode GetGamemodeFromId(string id) => GetGamemode(gamemode => gamemode.ID == id);

        public static Gamemode GetGamemode(Predicate<Gamemode> predicate)
        {
            if (GamemodeManager.HasInstance && GamemodeManager.Instance.Gamemodes.Find(predicate) is Gamemode gameMode)
                return gameMode;
            return null;
        }

        public static string GetGameModeName(GameModeType gameModeType)
        {
            if (GetGameModeInstance(gameModeType) is GorillaGameManager gameManager)
                return gameManager.GameModeName();
            return GameMode.GameModeZoneMapping.GetModeName(gameModeType);
        }

        public static GorillaGameManager GetGameModeInstance(GameModeType gameModeType)
        {
            if (GameMode.GetGameModeInstance(gameModeType) is GorillaGameManager gameManager && gameManager)
                return gameManager;
            return null;
        }
    }
}
