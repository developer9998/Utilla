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
    public class UtillaGamemodeSelector : Singleton<UtillaGamemodeSelector>
    {
        public override bool SingleInstance => false;

        // Layout

        public GameModeSelectorButtonLayout Layout => GetComponent<GameModeSelectorButtonLayout>();
        public GTZone Zone => Layout.zone;

        private ModeSelectButton[] modeSelectButtons = [];
        private static GameObject fallbackTemplateButton = null;

        // Pages

        public int PageCount = 1;
        public static int PageNumber;

        public override async void Initialize()
        {
            if (Instance == this && Zone != GTZone.forest) Instance = null;
            Logging.Info($"Added {(Instance == this ? "singleton for" : "instance of")} UtillaGameModeSelector ({Zone})");

            while (Layout.currentButtons.Count == 0) await Task.Delay(100);

            modeSelectButtons = [.. Layout.currentButtons.Take(GetBaseGameModes().Count)];
            
            foreach (var mb in modeSelectButtons)
            {
                TMP_Text gamemodeTitle = mb.gameModeTitle;
                gamemodeTitle.enableAutoSizing = true;
                gamemodeTitle.fontSizeMax = gamemodeTitle.fontSize;
                gamemodeTitle.fontSizeMin = 0f;
                gamemodeTitle.transform.localPosition = new Vector3(gamemodeTitle.transform.localPosition.x, 0f, gamemodeTitle.transform.localPosition.z + 0.08f);
            }

            CreatePageButtons(modeSelectButtons.First().gameObject);

            if (Instance == this && !Singleton<GamemodeManager>.HasInstance)
            {
                Plugin.PostInitialized();
                return;
            }

            bool check_mode = true;
            while (Singleton<GamemodeManager>.Instance.Gamemodes is null || Singleton<GamemodeManager>.Instance.ModdedGamemodesPerMode is null) 
            {
                check_mode = false;
                await Task.Delay(100);
            }
            if (check_mode) CheckGameMode();

            PageCount = Mathf.CeilToInt((float)GetSelectorGameModes().Count / GetBaseGameModes().Count);
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

        public List<Gamemode> GetBaseGameModes()
        {
            List<Gamemode> game_modes = [];
            var modesForZone = GameMode.GameModeZoneMapping.GetModesForZone(Zone, NetworkSystem.Instance.SessionIsPrivate);
            foreach (GameModeType game_mode_type in modesForZone)
            {
                game_modes.Add(new Gamemode(game_mode_type.ToString(), GameMode.GameModeZoneMapping.GetModeName(game_mode_type), game_mode_type));
            }
            return game_modes;
        }

        public List<Gamemode> GetSelectorGameModes()
        {
            var game_modes = GetBaseGameModes();
            game_modes = game_modes
                .Concat(game_modes.Select(game_mode => game_mode.BaseGamemode.HasValue && Singleton<GamemodeManager>.Instance.ModdedGamemodesPerMode.ContainsKey(game_mode.BaseGamemode.Value) ? Singleton<GamemodeManager>.Instance.ModdedGamemodesPerMode[game_mode.BaseGamemode.Value] : new Gamemode("", "", game_mode_type: null)))
                .Concat(GamemodeManager.Instance.CustomGameModes)
                .ToList();
            return game_modes;
        }

        public void CheckGameMode()
        {
            var game_mode_names = GetSelectorGameModes().Select(game_mode => game_mode.ID);
            var current_game_mode = GorillaComputer.instance.currentGameMode.Value;
            Logging.Info($"current mode: '{current_game_mode}' all modes: {string.Join(", ", game_mode_names.Select(game_mode => string.Format("'{0}'", game_mode)))}");
            if (!game_mode_names.Contains(current_game_mode))
            {
                var replacement_game_mode = current_game_mode.StartsWith(Constants.GamemodePrefix) ? string.Concat(Constants.GamemodePrefix, game_mode_names.ElementAt(0)) : game_mode_names.ElementAt(0);
                Logging.Info($"replacing current mode with '{replacement_game_mode}'");
                GorillaComputer.instance.SetGameModeWithoutButton(replacement_game_mode);
                return;
            }
            GorillaComputer.instance.SetGameModeWithoutButton(current_game_mode);
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
                else
                {
                    Text buttonText = button.GetComponentInChildren<Text>();
                    if (buttonText)
                    {
                        buttonText.text = text;
                        buttonText.transform.localScale = Vector3.Scale(buttonText.transform.localScale, new Vector3(2, 2, 1));
                    }
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
            Invoke("ShowPage",1);
        }

        public void NextPage()
        {
            PageNumber = (PageNumber + 1) % PageCount;

            ShowPage();
            GorillaComputer.instance.SetGameModeWithoutButton(GorillaComputer.instance.currentGameMode.Value);
        }

        public void PreviousPage()
        {
            PageNumber = (PageNumber <= 0) ? PageCount - 1 : PageNumber - 1;
            
            ShowPage();
            GorillaComputer.instance.SetGameModeWithoutButton(GorillaComputer.instance.currentGameMode.Value);
        }

        public void ShowPage()
        {
            if (PageCount == 0)
            {
                PageCount = 1;
            }
            var game_modes = GetSelectorGameModes();
            var currentGamemodes = game_modes.Skip(PageNumber * GetBaseGameModes().Count).Take(GetBaseGameModes().Count).ToList();

            for (int i = 0; i < modeSelectButtons.Length; i++)
            {
                Gamemode customMode = (i < currentGamemodes.Count) ? currentGamemodes[i] : null;
                if (customMode != null && !string.IsNullOrEmpty(customMode.ID))
                {
                    modeSelectButtons[i].enabled = true;
                    modeSelectButtons[i].SetInfo(customMode.ID, customMode.DisplayName, false, null);
                    continue;
                }
                modeSelectButtons[i].enabled = false;
                modeSelectButtons[i].SetInfo("", "", false, null);
            }
        }
    }
}
