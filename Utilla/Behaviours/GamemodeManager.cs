using BepInEx;
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

namespace Utilla.Behaviours
{
    public class GamemodeManager : Singleton<GamemodeManager>
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

            var currentGameMode = PlayerPrefs.GetString("currentGameMode", GameModeType.Infection.ToString());
            GorillaComputer.instance.currentGameMode.Value = currentGameMode;

            var zone_names = Enum.GetNames(typeof(GTZone));
            HashSet<GameModeType> all_game_modes = [];
            zone_names
                .Select(zone_name => (GTZone)Enum.Parse(typeof(GTZone), zone_name))
                .Select(zone => GameMode.GameModeZoneMapping.GetModesForZone(zone, NetworkSystem.Instance.SessionIsPrivate))
                .ForEach(all_game_modes.UnionWith);
            ModdedGamemodesPerMode = all_game_modes
                .Where(game_mode => game_mode != GameModeType.Custom)
                .ToDictionary(game_mode => game_mode, game_mode => new Gamemode($"MODDED_{game_mode}", $"MODDED {GameMode.GameModeZoneMapping.GetModeName(game_mode)}", game_mode));
            DefaultModdedGamemodes = [.. ModdedGamemodesPerMode.Values];

            var game_mode_selector = Singleton<UtillaGamemodeSelector>.Instance;
            Gamemodes = game_mode_selector.GetBaseGameModes();
            pluginInfos = GetPluginInfos();
            CustomGameModes = GetGamemodes(pluginInfos);
            Gamemodes.AddRange(DefaultModdedGamemodes.Concat(CustomGameModes));
            Gamemodes.ForEach(AddGamemodeToPrefabPool);

            // re-determine game mode
            game_mode_selector.CheckGameMode();
            currentGameMode = GorillaComputer.instance.currentGameMode.Value;
            Logging.Info($"currentGameMode: {currentGameMode}");

            var highlightedIndex = Gamemodes.FindIndex(gm => gm.ID == currentGameMode);
            Logging.Info($"highlightedIndex: {highlightedIndex}");

            game_mode_selector.PageCount = Mathf.FloorToInt((float)game_mode_selector.GetSelectorGameModes().Count / game_mode_selector.GetBaseGameModes().Count);
            UtillaGamemodeSelector.PageNumber = (highlightedIndex >= 0) ? Mathf.FloorToInt((float)highlightedIndex / game_mode_selector.GetBaseGameModes().Count) : 0;
            game_mode_selector.ShowPage();
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

            GameObject prefab = new GameObject(gamemode.ID);
            prefab.SetActive(false);
            var gameMode = prefab.AddComponent(gamemode.GameManager) as GorillaGameManager;
            int gameModeKey = (int)gameMode.GameType();

            if (GameMode.gameModeTable.ContainsKey(gameModeKey))
            {
                Logging.Error($"Game Mode with name '{GameMode.gameModeTable[gameModeKey].GameModeName()}' is already using GameType '{gameModeKey}'.");
                GameObject.Destroy(prefab);
                return;
            }

            GameMode.gameModeTable[gameModeKey] = gameMode;
            GameMode.gameModeKeyByName[gamemode.ID] = gameModeKey;
            //GameMode.gameModeKeyByName[gamemode.DisplayName] = gameModeKey;
            gtGameModeNames.Add(gamemode.ID);
            GameMode.gameModes.Add(gameMode);

            prefab.transform.SetParent(moddedGameModesObject.transform);
            prefab.SetActive(true);

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
