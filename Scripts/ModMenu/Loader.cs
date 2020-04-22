using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using Zat.ModMenu.UI;
using Zat.Shared.AssetLoading;

namespace Zat.ModMenu
{
    public class Loader : MonoBehaviour
    {
        public static KCModHelper Helper { get; private set; }
        public static AssetManager Assets { get; private set; }
        public static readonly string VERSION = "002";

        private static Transform uiTransform;
        public static Transform UITransform
        {
            get { return uiTransform; }
            set
            {
                if (uiTransform != value)
                {
                    uiTransform = value;
                    if (ModMenuUI.Instance != null)
                    {
                        //ModMenuUI.Instance.gameObject.SetActive(true);
                        //ModMenuUI.Instance.transform.SetParent(uiTransform, false);
                    }
                }
            }
        }

        public void Preload(KCModHelper _helper)
        {
            Helper = _helper;
            Helper.Log("[ModMenu] Preload");
            var harmony = HarmonyInstance.Create("ModMenu");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Helper.Log("[ModMenu] Preload: Patched");
        }

        public void SceneLoaded(KCModHelper _helper)
        {
            Helper.Log("[ModMenu] SceneLoaded: Hey there!");
            Helper = _helper;
            Assets = new AssetManager(_helper.modPath + "/Assets", "modmenu002");
            if (!Assets.BundleLoaded)
            {
                Helper.Log("[ModMenu] SceneLoaded: Failed to load AssetBundle!");
                return;
            }
            if (ModMenuUI.Instance)
            {
                Helper.Log("[ModMenu] SceneLoaded: ModUI instance already exists!");
                return;
            }
            var parent = UITransform ?? GameState.inst?.playingMode?.GameUIParent?.transform;
            /*if (parent == null)
            {
                Helper.Log("[ModMenu] SceneLoaded: playingMode::GameUIParent/mainMenuMode::mainMenuUI null");
                return;
            }*/
            var canvasPrefab = Assets.GetPrefab("assets/workspace/ModMenu/ModUICanvas002.prefab");
            if (!canvasPrefab)
            {
                Helper.Log("[ModMenu] SceneLoaded: Missing canvasPrefab!");
                return;
            }
            var canvas = GameObject.Instantiate(canvasPrefab) as GameObject;
            //canvas.transform.SetParent(parent, false);
            canvas.AddComponent<ModMenuUI>();
            Helper.Log("[ModMenu] Running!");
        }

        [HarmonyPatch(typeof(MainMenuMode))]
        [HarmonyPatch("Init")]
        public static class MainMenuModePatch
        {
            static void Postfix()
            {
                var parent = GameState.inst?.mainMenuMode?.topLevelUI?.transform?.parent;
                if (parent) Loader.UITransform = parent;
            }
        }

        [HarmonyPatch(typeof(PlayingMode))]
        [HarmonyPatch("Init")]
        public static class PlayingModePatch
        {
            static void Postfix()
            {
                var parent = GameState.inst?.playingMode?.GameUIParent?.transform;
                if (parent) Loader.UITransform = parent;
            }
        }


    }
}