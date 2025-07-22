using GorillaGameModes;
using GorillaNetworking;
using GorillaTag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilla.Models;
using Utilla.Tools;

namespace Utilla.Behaviours
{
    [RequireComponent(typeof(GameModeSelectorButtonLayout)), DisallowMultipleComponent]
    internal class UtillaGamemodeSelector : MonoBehaviour
    {
        public static Dictionary<GTZone, UtillaGamemodeSelector> SelectorLookup = [];

        // public List<Gamemode> BaseGameModes;
        public readonly Dictionary<bool, List<Gamemode>> SelectorGameModes = [];

        public GameModeSelectorButtonLayout Layout;
        public GTZone Zone;

        public int CurrentPage, PageCount, PageCapacity;

        private ModeSelectButton[] modeSelectButtons = [];
        private static GameObject fallbackTemplateButton = null;

        public async void Awake()
        {
            Layout = GetComponent<GameModeSelectorButtonLayout>();
            Zone = Layout.zone;

            if (SelectorLookup.ContainsKey(Zone))
            {
                Logging.Warning($"Found duplicate game mode selector for zone {Zone}");
            }
            else
            {
                SelectorLookup.Add(Zone, this);
                Logging.Info($"Initializing game mode selector for zone {Zone}");
            }

            while (Layout.currentButtons.Count == 0)
            {
                Logging.Info("Awaiting button creation");
                await Task.Delay(100);
            }

            modeSelectButtons = [.. Layout.currentButtons];// [.. Layout.currentButtons.Take(BaseGameModes.Count)];
            PageCapacity = modeSelectButtons.Length;

            foreach (var mb in modeSelectButtons)
            {
                TMP_Text gamemodeTitle = mb.gameModeTitle;
                gamemodeTitle.enableAutoSizing = true;
                gamemodeTitle.fontSizeMax = gamemodeTitle.fontSize;
                gamemodeTitle.fontSizeMin = 0f;
                gamemodeTitle.transform.localPosition = new Vector3(gamemodeTitle.transform.localPosition.x, 0f, gamemodeTitle.transform.localPosition.z + 0.08f);
            }

            CreatePageButtons(modeSelectButtons.First().gameObject);

            Logging.Info("Checking for game mode manager");

            if (!GamemodeManager.HasInstance)
            {
                if (Zone == PhotonNetworkController.Instance.StartZone)
                {
                    Logging.Info("Start zone detected - creating game mode manager");
                    Plugin.PostInitialized();
                    return;
                }
            }

            while (!GamemodeManager.HasInstance || GamemodeManager.Instance.Gamemodes is null || GamemodeManager.Instance.ModdedGamemodesPerMode is null || GamemodeManager.Instance.CustomGameModes is null)
            {
                await Task.Delay(100);
                Logging.Info("Waiting for game mode manager");
            }

            if (ZoneManagement.instance.activeZones is var activeZones && activeZones.Contains(Zone))
            {
                Logging.Info("Checking game mode validity");
                CheckGameMode();
            }

            PageCount = Mathf.CeilToInt(GetSelectorGameModes().Count / (float)PageCapacity);
            ShowPage();
        }

        public void OnEnable()
        {
            NetworkSystem.Instance.OnJoinedRoomEvent += ShowPage;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += ShowPage;
        }

        public void OnDisable()
        {
            NetworkSystem.Instance.OnJoinedRoomEvent -= ShowPage;
            NetworkSystem.Instance.OnReturnedToSinglePlayer -= ShowPage;
        }

        public List<Gamemode> GetSelectorGameModes()
        {
            bool sessionIsPrivate = NetworkSystem.Instance.SessionIsPrivate;

            if (SelectorGameModes.TryGetValue(sessionIsPrivate, out List<Gamemode> gameModeList))
                return gameModeList;

            gameModeList = [];

            Logging.Info($"GetSelectorGameModes {Zone}");

            GameModeType[] modesForZone = [.. GameMode.GameModeZoneMapping.GetModesForZone(Zone, NetworkSystem.Instance.SessionIsPrivate)];
            
            // Base gamemodes
            for (int i = 0; i < modesForZone.Length; i++)
            {
                if (GamemodeManager.Instance.DefaultGameModesPerMode.TryGetValue(modesForZone[i], out Gamemode gamemode))
                {
                    gameModeList.Add(gamemode);
                }
            }

            // Modded gamemodes
            for (int i = 0; i < modesForZone.Length; i++)
            {
                if (GamemodeManager.Instance.ModdedGamemodesPerMode.TryGetValue(modesForZone[i], out Gamemode gamemode))
                {
                    Logging.Info($"+ \"{gamemode.DisplayName}\" ({modesForZone[i].GetName()})");
                    gameModeList.Add(gamemode);
                    continue;
                }

                gameModeList.Add(null); // TODO: substitute null item with empty game mode object
            }

            // Custom gamemodes
            if (GamemodeManager.Instance.CustomGameModes is List<Gamemode> customGameModes)
            {
                for (int i = 0; i < customGameModes.Count; i++)
                {
                    Gamemode gameMode = customGameModes[i];
                    Logging.Info($"+ \"{gameMode.DisplayName}\"");
                    gameModeList.Add(gameMode);
                    continue;
                }
            }

            if (SelectorGameModes.TryAdd(sessionIsPrivate, gameModeList))
            {
                Logging.Info(string.Join(", ", gameModeList.Select(gameMode => gameMode.DisplayName).Select(gameMode => string.Format("\"{0}\"", gameMode))));
            }

            return gameModeList;
        }

        public void CheckGameMode()
        {
            string currentGameMode = GorillaComputer.instance.currentGameMode.Value;
            List<string> gamemodeNames = [.. GetSelectorGameModes().Where(gameMode => gameMode is not null).Select(game_mode => game_mode.ID)];

            if (gamemodeNames.Contains(currentGameMode))
            {
                ShowPage();
                return;
            }

            if (currentGameMode.StartsWith(Constants.GamemodePrefix))
            {
                string moddedModeName = string.Concat(Constants.GamemodePrefix, currentGameMode);
                if (gamemodeNames.Contains(moddedModeName))
                {
                    Logging.Message("Switching to modded variant");
                    Logging.Info(moddedModeName);
                    GorillaComputer.instance.SetGameModeWithoutButton(moddedModeName);
                    CurrentPage = Mathf.Max(0, gamemodeNames.FindIndex(gamemode => gamemode == moddedModeName));
                    ShowPage();
                    return;
                }
            }

            currentGameMode = gamemodeNames.First();
            Logging.Message("Switching to vanilla variant");
            Logging.Info(currentGameMode);
            GorillaComputer.instance.SetGameModeWithoutButton(currentGameMode);
            CurrentPage = Mathf.Max(0, gamemodeNames.FindIndex(gamemode => gamemode == currentGameMode));
            ShowPage();
        }

        void CreatePageButtons(GameObject templateButton)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.SetActive(false);
            MeshFilter meshFilter = cube.GetComponent<MeshFilter>();

            GameObject CreatePageButton(string text, Action onPressed)
            {
                // button creation
                GameObject button = Instantiate(templateButton.transform.childCount == 0 ? fallbackTemplateButton : templateButton);

                // button appearence
                button.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
                button.GetComponent<Renderer>().material = templateButton.GetComponent<GorillaPressableButton>().unpressedMaterial;

                // button location
                button.transform.parent = templateButton.transform.parent;
                button.transform.localRotation = templateButton.transform.localRotation;
                button.transform.localScale = Vector3.one * 0.1427168f; // shouldn't hurt anyone for now 

                TMP_Text tmpText = button.transform.Find("Title")?.GetComponent<TMP_Text>() ?? button.GetComponentInChildren<TMP_Text>(true);
                if (tmpText)
                {
                    tmpText.gameObject.SetActive(true);
                    tmpText.enabled = true;
                    tmpText.transform.localPosition = Vector3.forward * 0.525f;
                    tmpText.transform.localEulerAngles = Vector3.up * 180f;
                    tmpText.transform.localScale = Vector3.Scale(tmpText.transform.localScale, new Vector3(0.5f, 0.5f, 1));
                    tmpText.text = text;
                    tmpText.color = Color.black;
                    tmpText.horizontalAlignment = HorizontalAlignmentOptions.Center;
                    if (tmpText.TryGetComponent(out StaticLodGroup group)) Destroy(group);
                }
                else if (button.GetComponentInChildren<Text>() is Text buttonText)
                {
                    buttonText.text = text;
                    buttonText.transform.localScale = Vector3.Scale(buttonText.transform.localScale, new Vector3(2, 2, 1));
                }

                // button behaviour
                Destroy(button.GetComponent<ModeSelectButton>());
                var unityEvent = new UnityEvent();
                unityEvent.AddListener(new UnityAction(onPressed));
                var pressable_button = button.AddComponent<GorillaPressableButton>();
                pressable_button.onPressButton = unityEvent;

                return button;
            }

            GameObject nextPageButton = CreatePageButton("-->", NextPage);
            nextPageButton.transform.localPosition = new Vector3(-0.745f, nextPageButton.transform.position.y + 0.005f, nextPageButton.transform.position.z - 0.03f);

            GameObject previousPageButton = CreatePageButton("<--", PreviousPage);
            previousPageButton.transform.localPosition = new Vector3(-0.745f, -0.633f, previousPageButton.transform.position.z - 0.03f);

            Destroy(cube);

            if (templateButton.transform.childCount != 0)
            {
                fallbackTemplateButton = templateButton;
            }

            Invoke(nameof(ShowPage), 1);
        }

        public void NextPage()
        {
            CurrentPage = (CurrentPage + 1) % PageCount;
            ShowPage();
        }

        public void PreviousPage()
        {
            CurrentPage = (CurrentPage <= 0) ? PageCount - 1 : CurrentPage - 1;
            ShowPage();
        }

        public void ShowPage()
        {
            List<Gamemode> allGamemodes = GetSelectorGameModes();
            List<Gamemode> shownGamemodes = [.. allGamemodes.Skip(CurrentPage * PageCapacity).Take(PageCapacity)];

            for (int i = 0; i < modeSelectButtons.Length; i++)
            {
                Gamemode gamemode = shownGamemodes.ElementAtOrDefault(i);
                bool hasMode = gamemode != null && gamemode.ID != null && gamemode.ID.Length != 0;

                ModeSelectButton button = modeSelectButtons[i];
                if (button.gameObject.activeSelf != hasMode) button.gameObject.SetActive(hasMode);

                if (!button.gameObject.activeSelf)
                {
                    button.enabled = false;
                    button.SetInfo("", "", false, null);
                    continue;
                }

                button.enabled = true;
                button.SetInfo(gamemode.ID, gamemode.DisplayName.ToUpper(), false, null);
                button.OnGameModeChanged(GorillaComputer.instance.currentGameMode.Value);

                //if (forceCheck) button.OnGameModeChanged(GorillaComputer.instance.currentGameMode.Value);
                //else button.OnGameModeChanged(GorillaComputer.instance.currentGameMode.Value);
            }
        }
    }
}
