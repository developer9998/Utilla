using GorillaGameModes;
using System;
using System.Linq;
using Utilla.Behaviours;
using Utilla.Models;

namespace Utilla.Utils
{
    public static class GameModeUtils
    {
        public static Gamemode FindGamemodeInString(string gmString) => GetGamemode(gamemode => gmString.EndsWith(gamemode.ID));

        public static Gamemode GetGamemodeFromId(string id) => GetGamemode(gamemode => gamemode.ID == id);

        public static Gamemode GetGamemode(Func<Gamemode, bool> predicate)
        {
            // Search all gamemodes in reverse order to prioritize modded gamemodes
            if (GamemodeManager.HasInstance && GamemodeManager.Instance.Gamemodes.LastOrDefault(predicate) is Gamemode gameMode)
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
