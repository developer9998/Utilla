using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Utilla.Behaviours
{
    public class UtillaNetworkController : Singleton<UtillaNetworkController>, IInRoomCallbacks
    {
        Events.RoomJoinedArgs lastRoom;

        public override void Initialize()
        {
            base.Initialize();

            NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinedRoom;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += OnLeftRoom;
        }

        public void OnJoinedRoom()
        {
            //if (GTAppState.isQuitting) return;

            // trigger events
            bool isPrivate = false;
            string gamemode = "";
            if (PhotonNetwork.CurrentRoom != null)
            {
                var currentRoom = PhotonNetwork.NetworkingClient.CurrentRoom;
                isPrivate = !currentRoom.IsVisible; // Room Browser rooms
                if (currentRoom.CustomProperties.TryGetValue("gameMode", out var gamemodeObject))
                {
                    gamemode = gamemodeObject as string;
                }
            }

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
            //if (GTAppState.isQuitting) return;

            if (lastRoom != null)
            {
                Events.Instance.TriggerRoomLeft(lastRoom);
                lastRoom = null;
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            //if (GTAppState.isQuitting) return;

            if (!propertiesThatChanged.TryGetValue("gameMode", out var gameModeObject) || gameModeObject is not string gameMode) return;

            if (lastRoom.Gamemode.Contains(Constants.GamemodePrefix) != gameMode.Contains(Constants.GamemodePrefix))
            {
                Singleton<GamemodeManager>.Instance.OnRoomLeft(null, lastRoom);
            }

            lastRoom.Gamemode = gameMode;
            lastRoom.isPrivate = PhotonNetwork.CurrentRoom.IsVisible;
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
