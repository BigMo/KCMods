using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.AssetLoading;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.Commander
{
    class Loader
    {
        public static AssetManager Assets;
        public static CommanderSettings Settings { get; private set; }
        public static CommanderUI CommanderUI { get; private set; }

        public void Preload(KCModHelper _helper)
        {
            Debugging.Active = true;
            Debugging.Helper = _helper;
        }

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            Debugging.Active = true;
            Debugging.Helper = helper;
            try
            {
                //Parse config
                var config = new InteractiveConfiguration<CommanderSettings>();
                Settings = config.Settings;

                //Register mod
                Shared.ModMenu.API.ModSettingsBootstrapper.Register(config.ModConfig,
                    (proxy, oldSettings) => {
                        config.Install(proxy, oldSettings);
                        Settings.Visibility.OnUpdatedRemotely.AddListener((entry) => { CommanderUI?.window.SetActive(entry.toggle.value); });
                    },
                    (ex) =>
                    {
                        Debugging.Log("Loader", $"Failed to register mod config: {ex.Message}");
                        Debugging.Log("Loader", ex.StackTrace);
                    });


                //Load AssetBundle
                if (CommanderUI.Instantiated)
                    return;
                if (Assets == null)
                    Assets = new AssetManager(helper.modPath + "/Assets", "commander001");
                if (!Assets.BundleLoaded)
                {
                    Debugging.Log("Loader", "Failed to load assetbundle!");
                    return;
                }
                var prefab = Assets.GetPrefab("assets/workspace/Commander/CommanderCanvas.prefab");
                if (!prefab)
                {
                    Debugging.Log("Loader", "Failed to load prefab!");
                    return;
                }
                //Create canvas
                var canvas = GameObject.Instantiate(prefab) as GameObject;
                if (!canvas)
                {
                    Debugging.Log("Loader", "Failed to instantiate prefab!");
                    return;
                }
                var parent = GameState.inst?.playingMode?.GameUIParent?.transform;
                if (parent == null)
                {
                    if (GameState.inst == null) Debugging.Log("Loader", "GameState.inst NULL");
                    else if (GameState.inst.playingMode == null) Debugging.Log("Loader", "GameState.inst.playingMode NULL");
                    else if (GameState.inst.playingMode.GameUIParent == null) Debugging.Log("Loader", "GameState.inst.playingMode.GameUIParent NULL");
                    else if (GameState.inst.playingMode.GameUIParent.transform == null) Debugging.Log("Loader", "GameState.inst.playingMode.GameUIParent.transform NULL");
                    return;
                }
                //Integrate canvas
                canvas.transform.SetParent(parent, true);
                CommanderUI = canvas.AddComponent<CommanderUI>();
                Debugging.Log("Loader", "Initialized Commander");
            }
            catch (Exception ex)
            {
                Debugging.Log("Loader", $"Failed to parse interactive mod config: {ex.Message}");
                Debugging.Log("Loader", ex.StackTrace);
            }
        }

        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            //Load up Harmony
            var harmony = HarmonyInstance.Create("Commander");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(PlayingMode))]
        [HarmonyPatch("Init")]
        public static class PlayingModePatch
        {
            static void Postfix(PlayingMode __instance)
            {
                try
                {
                    if (!CommanderUI.Instantiated) throw new Exception("CommanderUI not initialized");

                    CommanderUI.Instance.LoadSlots();
                }
                catch (Exception ex)
                {
                    Debugging.Log("PlayingModePatch", $"Failed to patch: {ex.Message}");
                    Debugging.Log("PlayingModePatch", ex.StackTrace);
                }
            }
        }
    }
}
