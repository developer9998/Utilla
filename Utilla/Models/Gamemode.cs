using GorillaGameModes;
using System;
using System.Linq;

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
        Paintbrawl,
        /// <summary>
        /// Ghost Tag gamemode, requires at least four participating players for infection and under for tag.
        /// </summary>
        Ghost,
        /// <summary>
        /// Ambush gamemode, requires at least four participating players for infection and under for tag.
        /// </summary>
        Ambush,
        /// <summary>
        /// Freeze Tag gamemode, requires at least four participating players for infection and under for tag.
        /// </summary>
        FreezeTag,
        /// <summary>
        /// Guardian gamemode, requires at least one participating player.
        /// </summary>
        Guardian

        // i dont think they've added anymore gamemodes besides these
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
        /// The BaseGamemode being inherited
        /// </summary>
        public GameModeType? BaseGamemode { get; }
        public Type GameManager { get; }

        public Gamemode(string id, string displayName, GameModeType? game_mode_type = null)
        {
            ID = id;
            DisplayName = displayName;
            BaseGamemode = game_mode_type;
            // GamemodeString = game_mode_type.HasValue ? BaseGamemode.ToString() : string.Empty;
        }

        public Gamemode(string id, string displayName, Type gameManager)
        {
            ID = id;
            DisplayName = displayName;
            GameManager = gameManager;
            // GamemodeString = Constants.GamemodePrefix + ID;
        }
    }
}
