using ExitGames.Client.Photon;
using Photon.Realtime;
using Utilla.Models;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla.Behaviours
{
    internal class UtillaNetworkController : Singleton<UtillaNetworkController>, IInRoomCallbacks
    {
        /// <summary>
        /// The Gamemode instance based on the current room
        /// </summary>
        /// <remarks>
        /// Requires the NetworkSystem instance alongside presence in a room
        /// </remarks>
        public Gamemode CurrentGamemode
        {
            get
            {
                if (currentGamemode is null && NetworkSystem.Instance is NetworkSystem netSys && netSys && netSys.InRoom)
                    currentGamemode = GameModeUtils.FindGamemodeInString(netSys.GameModeString);
                return currentGamemode;
            }
        }

        private Gamemode currentGamemode;

        private Events.RoomJoinedArgs lastRoom;

        public override void Initialize()
        {
            base.Initialize();

            NetworkSystem.Instance.OnMultiplayerStarted += OnJoinedRoom;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += OnLeftRoom;
        }

        public void OnJoinedRoom()
        {
            if (ApplicationQuittingState.IsQuitting) return;

            // trigger events

            bool isPrivate = false;
            string gamemode = string.Empty;

            if (NetworkSystem.Instance is NetworkSystem netSys && netSys)
            {
                isPrivate = netSys.SessionIsPrivate;
                gamemode = netSys.GameModeString;
            }
            else Logging.Warning("what the shit");

            Events.RoomJoinedArgs args = new()
            {
                isPrivate = isPrivate,
                Gamemode = gamemode
            };

            Events.Instance.TriggerRoomJoin(args);

            lastRoom = args;

            //RoomUtils.ResetQueue();
        }

        public void OnLeftRoom()
        {
            if (ApplicationQuittingState.IsQuitting) return;

            if (lastRoom != null)
            {
                Events.Instance.TriggerRoomLeft(lastRoom);
                lastRoom = null;
            }

            currentGamemode = null;
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (ApplicationQuittingState.IsQuitting || !propertiesThatChanged.TryGetValue("gameMode", out object gameModeObject) || gameModeObject is not string gameMode) return;

            if (lastRoom.Gamemode.Contains(Constants.GamemodePrefix) != gameMode.Contains(Constants.GamemodePrefix))
            {
                Singleton<GamemodeManager>.Instance.OnRoomLeft(null, lastRoom);
            }

            lastRoom.Gamemode = gameMode;
            lastRoom.isPrivate = NetworkSystem.Instance.SessionIsPrivate;

            currentGamemode = GameModeUtils.FindGamemodeInString(gameMode);
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {

        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {

        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {

        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {

        }
    }
}
