using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using Zat.Shared.AssetLoading;

namespace Zat.Minimap
{ 
    public class Loader : MonoBehaviour
    {
        public static KCModHelper Helper;
        public static AssetManager Assets;

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            Helper = helper;
            if (Minimap.Instantiated)
                return;
            if (Assets == null)
                Assets = new AssetManager(helper.modPath + "/Assets", "minimap002");
            if (!Assets.BundleLoaded)
            {
                Helper.Log("Failed to load assetbundle!");
                return;
            }
            var prefab = Assets.GetPrefab("assets/workspace/Minimap/MapCanvas.prefab");
            if (!prefab)
            {
                Helper.Log("Failed to load prefab!");
                return;
            }
            var canvas = GameObject.Instantiate(prefab) as GameObject;
            if (!canvas)
            {
                helper.Log("Failed to instantiate prefab!");
                return;
            }
            var minimap = canvas.AddComponent<Minimap>();
            Helper.Log("Initialized Minimap");
        }

        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            Helper = kcModHelper;
            //Load up Harmony
            var harmony = HarmonyInstance.Create("MinimapLoader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
