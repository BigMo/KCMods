using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.AssetLoading;

namespace Zat.Commander
{
    class Loader
    {
        public static AssetManager Assets;

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            Debugging.Active = true;
            Debugging.Helper = helper;

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
            var canvas = GameObject.Instantiate(prefab) as GameObject;
            if (!canvas)
            {
                helper.Log("Failed to instantiate prefab!");
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
            canvas.transform.SetParent(parent, true);
            var commander = canvas.AddComponent<CommanderUI>();
            Debugging.Log("Loader", "Initialized Commander");
        }

        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            //Load up Harmony
            var harmony = HarmonyInstance.Create("MinimapLoader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
