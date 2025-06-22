using GorillaGameModes;

namespace Utilla.Utils
{
    public static class GameModeUtils
    {
        public static string GetGameModeName(GameModeType gameModeType)
        {
            if (GameMode.GetGameModeInstance(gameModeType) is GorillaGameManager gameManager && gameManager)
                return gameManager.GameModeName();
            return GameMode.GameModeZoneMapping.GetModeName(gameModeType);
        }
    }
}
