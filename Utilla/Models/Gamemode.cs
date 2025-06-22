using GorillaGameModes;
using System;

namespace Utilla.Models
{
    /// <summary>
    /// The base gamemode for a gamemode to inherit.
    /// </summary>
    /// <remarks>
    /// None should not be used from an external program.
    /// </remarks>
    public enum BaseGamemode
    {
        /// <summary>
        /// There is no gamemode manager to rely on, this should only be used by the Utilla mod when preparing modded gamemodes or gamemodes using a unique gamemode manager.
        /// </summary>
        None,
        /// <summary>
        /// Infection gamemode, requires at least four participating players for infection and under for tag.
        /// </summary>
        Infection,
        /// <summary>
        /// Casual gamemode, no players are affected by the gamemode, such as tagging or infecting.
        /// </summary>
        Casual,
        /// <summary>
        /// Hunt gamemode, requires at least four participating players.
        /// </summary>
        Hunt,
        /// <summary>
        /// Paintbrawl gamemode, requires at least two participating players.
        /// </summary>
        Paintbrawl
    }

    public class Gamemode
    {
        /// <summary>
        /// The title of the Gamemode visible through the gamemode selector
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The internal ID of the Gamemode
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// An optional reference of a game mode to inherit
        /// </summary>
        public GameModeType? BaseGamemode { get; }

        /// <summary>
        /// An optional reference of a game mode manager to create
        /// </summary>
        public Type GameManager { get; }

        public Gamemode(string displayName, GameModeType gameModeType)
        {
            ID = gameModeType.ToString();
            DisplayName = displayName;
            BaseGamemode = gameModeType;
        }

        public Gamemode(string id, string displayName, GameModeType? game_mode_type = null)
        {
            ID = game_mode_type.HasValue ? string.Concat(id, game_mode_type) : id;
            DisplayName = displayName;
            BaseGamemode = game_mode_type;
        }

        public Gamemode(string id, string displayName, Type gameManager)
        {
            if (!typeof(GorillaGameManager).IsAssignableFrom(gameManager))
                throw new ArgumentException("Game manager must be inherited from GorillaGameManager");

            ID = id;
            DisplayName = displayName;
            GameManager = gameManager;
        }
    }
}
