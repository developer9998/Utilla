﻿using BepInEx;
using GorillaGameModes;
using GorillaNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Utilla.Attributes;
using Utilla.Models;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla.Behaviours
{
    internal class GamemodeManager : Singleton<GamemodeManager>
    {
        public Dictionary<GameModeType, Gamemode> ModdedGamemodesPerMode;
        public List<Gamemode> DefaultModdedGamemodes;
        public List<Gamemode> CustomGameModes;
        public List<Gamemode> Gamemodes { get; private set; }

        private List<PluginInfo> pluginInfos;

        /*
        FieldInfo fiGameModeInstance = typeof(GameMode).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        GameMode gtGameModeInstance;

        FieldInfo fiGameModeTable = typeof(GameMode).GetField("gameModeTable", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<int, GorillaGameManager> gtGameModeTable;

        FieldInfo fiGameModeKeyByName = typeof(GameMode).GetField("gameModeKeyByName", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<string, int> gtGameModeKeyByName;

        FieldInfo fiGameModes = typeof(GameMode).GetField("gameModes", BindingFlags.Static | BindingFlags.NonPublic);
        List<GorillaGameManager> gtGameModes;
        */

        List<string> gtGameModeNames;

        GameObject moddedGameModesObject;

        public override void Initialize()
        {
            base.Initialize();

            Events.RoomJoined += OnRoomJoin;
            Events.RoomLeft += OnRoomLeft;
        }

        public void Start()
        {
            gtGameModeNames = GameMode.gameModeNames;

            moddedGameModesObject = new GameObject("Modded Game Modes");
            moddedGameModesObject.transform.SetParent(GameMode.instance.gameObject.transform);

            string currentGameMode = PlayerPrefs.GetString("currentGameMode", GameModeType.Infection.ToString());
            GorillaComputer.instance.currentGameMode.Value = currentGameMode;

            IEnumerable<GTZone> zones = Enum.GetValues(typeof(GTZone)).Cast<GTZone>();
            HashSet<GameModeType> all_game_modes = [];
            zones.Select(zone => GameMode.GameModeZoneMapping.GetModesForZone(zone, NetworkSystem.Instance.SessionIsPrivate)).ForEach(all_game_modes.UnionWith);
            ModdedGamemodesPerMode = all_game_modes.ToDictionary(game_mode => game_mode, game_mode => new Gamemode(Constants.GamemodePrefix, $"MODDED {GameModeUtils.GetGameModeName(game_mode)}", game_mode));
            Logging.Info($"Modded Game Modes: {string.Join(", ", ModdedGamemodesPerMode.Select(item => item.Value).Select(mode => mode.DisplayName).Select(displayName => string.Format("\"{0}\"", displayName)))}");
            DefaultModdedGamemodes = [.. ModdedGamemodesPerMode.Values];

            var game_mode_selector = UtillaGamemodeSelector.SelectorLookup[PhotonNetworkController.Instance.StartZone];
            Gamemodes = [.. game_mode_selector.BaseGameModes];

            pluginInfos = GetPluginInfos();
            CustomGameModes = GetGamemodes(pluginInfos);
            Logging.Info($"Custom Game Modes: {string.Join(", ", CustomGameModes.Select(mode => mode.DisplayName).Select(displayName => string.Format("\"{0}\"", displayName)))}");
            Gamemodes.AddRange(DefaultModdedGamemodes.Concat(CustomGameModes));
            Gamemodes.ForEach(AddGamemodeToPrefabPool);
            Logging.Info($"Game Modes: {string.Join(", ", Gamemodes.Select(mode => mode.DisplayName).Select(displayName => string.Format("\"{0}\"", displayName)))}");

            game_mode_selector.CheckGameMode();
            currentGameMode = GorillaComputer.instance.currentGameMode.Value;

            int basePageCount = game_mode_selector.BaseGameModes.Count;
            var avaliableModes = game_mode_selector.GetSelectorGameModes();
            int selectedMode = avaliableModes.FindIndex(gm => gm.ID == currentGameMode);
            game_mode_selector.PageCount = Mathf.CeilToInt(avaliableModes.Count / (float)basePageCount);
            game_mode_selector.CurrentPage = (selectedMode != -1 && selectedMode < avaliableModes.Count) ? Mathf.FloorToInt(selectedMode / (float)basePageCount) : 0;
            game_mode_selector.ShowPage(true);
        }

        public List<Gamemode> GetGamemodes(List<PluginInfo> infos)
        {
            List<Gamemode> gamemodes = [];

            HashSet<Gamemode> additonalGamemodes = [];
            foreach (var info in infos)
            {
                additonalGamemodes.UnionWith(info.Gamemodes);
            }

            foreach (var gamemode in DefaultModdedGamemodes)
            {
                additonalGamemodes.Remove(gamemode);
            }

            gamemodes.AddRange(additonalGamemodes);

            return gamemodes;
        }

        List<PluginInfo> GetPluginInfos()
        {
            List<PluginInfo> infos = [];
            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                if (info.Value == null) continue;
                BaseUnityPlugin plugin = info.Value.Instance;
                if (plugin == null) continue;
                Type type = plugin.GetType();

                IEnumerable<Gamemode> gamemodes = GetGamemodes(type);

                if (gamemodes.Count() > 0)
                {
                    infos.Add(new PluginInfo
                    {
                        Plugin = plugin,
                        Gamemodes = gamemodes.ToArray(),
                        OnGamemodeJoin = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeJoinAttribute)),
                        OnGamemodeLeave = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeLeaveAttribute))
                    });
                }
            }

            return infos;
        }

        Action<string> CreateJoinLeaveAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression param = Expression.Parameter(typeof(string));
            ParameterExpression[] paramExpression = new ParameterExpression[] { param };
            ConstantExpression instance = Expression.Constant(plugin);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Action<string> action = null;
            foreach (var method in baseType.GetMethods(bindingFlags).Where(m => m.GetCustomAttribute(attribute) != null))
            {
                var parameters = method.GetParameters();
                MethodCallExpression methodCall;
                if (parameters.Length == 0)
                {
                    methodCall = Expression.Call(instance, method);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    methodCall = Expression.Call(instance, method, param);
                }
                else
                {
                    continue;
                }

                action += Expression.Lambda<Action<string>>(methodCall, paramExpression).Compile();
            }

            return action;
        }

        HashSet<Gamemode> GetGamemodes(Type type)
        {
            IEnumerable<ModdedGamemodeAttribute> attributes = type.GetCustomAttributes<ModdedGamemodeAttribute>();

            HashSet<Gamemode> gamemodes = new HashSet<Gamemode>();
            if (attributes != null)
            {
                foreach (ModdedGamemodeAttribute attribute in attributes)
                {
                    if (attribute.gamemode != null)
                    {
                        gamemodes.Add(attribute.gamemode);
                    }
                    else
                    {
                        gamemodes.UnionWith(DefaultModdedGamemodes);
                    }
                }
            }

            return gamemodes;
        }

        void AddGamemodeToPrefabPool(Gamemode gamemode)
        {
            if (gamemode.GameManager is null) return;
            if (GameMode.gameModeKeyByName.ContainsKey(gamemode.ID))
            {
                Logging.Warning($"Game Mode with name '{gamemode.ID}' already exists.");
                return;
            }

            Type gmType = gamemode.GameManager;
            if (gmType == null || !gmType.IsSubclassOf(typeof(GorillaGameManager)))
            {
                GameModeType? gmKey = gamemode.BaseGamemode;

                if (gmKey == null)
                {
                    return;
                }

                GameMode.gameModeKeyByName[gamemode.ID] = (int)gmKey;

                //GameMode.gameModeKeyByName[gamemode.DisplayName] = (int)gmKey;
                gtGameModeNames.Add(gamemode.ID);
                return;
            }

            GameObject prefab = new(gamemode.ID);
            prefab.SetActive(false);
            var gameMode = prefab.AddComponent(gamemode.GameManager) as GorillaGameManager;
            int gameModeKey = (int)gameMode.GameType();

            if (GameMode.gameModeTable.ContainsKey(gameModeKey))
            {
                Logging.Error($"Game Mode with name '{GameMode.gameModeTable[gameModeKey].GameModeName()}' is already using GameType '{gameModeKey}'.");
                Destroy(prefab);
                return;
            }

            GameMode.gameModeTable[gameModeKey] = gameMode;
            GameMode.gameModeKeyByName[gamemode.ID] = gameModeKey;
            //GameMode.gameModeKeyByName[gamemode.DisplayName] = gameModeKey;
            gtGameModeNames.Add(gamemode.ID);
            GameMode.gameModes.Add(gameMode);

            prefab.transform.SetParent(moddedGameModesObject.transform);
            prefab.SetActive(true);

            if (gameMode.fastJumpLimit == 0 || gameMode.fastJumpMultiplier == 0)
            {
                Logging.Warning($"FAST JUMP SPEED AREN'T ASSIGNED FOR {gamemode.GameManager.Name}!!! ASSIGN THESE ASAP");

                var speed = gameMode.LocalPlayerSpeed();
                gameMode.fastJumpLimit = speed[0];
                gameMode.fastJumpMultiplier = speed[1];
            }
        }

        internal void OnRoomJoin(object sender, Events.RoomJoinedArgs args)
        {
            string gamemode = args.Gamemode;

            Logging.Info($"Game Mode is set as {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                Logging.Info($"{pluginInfo.Plugin.GetType().Name}: {string.Join(", ", pluginInfo.Gamemodes.Select(gm => gm.ID))}");
                if (pluginInfo.Gamemodes.Any(x => gamemode.Contains(x.ID)))
                {
                    try
                    {
                        pluginInfo.OnGamemodeJoin?.Invoke(gamemode);
                        Logging.Info("yes");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else
                {
                    Logging.Info("no");
                }
            }
        }

        internal void OnRoomLeft(object sender, Events.RoomJoinedArgs args)
        {
            string gamemode = args.Gamemode;

            foreach (var pluginInfo in pluginInfos)
            {
                if (pluginInfo.Gamemodes.Any(x => gamemode.Contains(x.ID)))
                {
                    try
                    {
                        pluginInfo.OnGamemodeLeave?.Invoke(gamemode);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }
    }
}
