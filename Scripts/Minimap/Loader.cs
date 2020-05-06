using System.Reflection;
using Harmony;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.AssetLoading;

namespace Zat.Minimap
{
    public class Loader : MonoBehaviour
    {
        public static AssetManager Assets;

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            Debugging.Active = true;
            Debugging.Helper = helper;

            if (Minimap.Instantiated)
                return;
            if (Assets == null)
                Assets = new AssetManager(helper.modPath + "/Assets", "minimap002");
            if (!Assets.BundleLoaded)
            {
                Debugging.Log("Loader", "Failed to load assetbundle!");
                return;
            }
            var prefab = Assets.GetPrefab("assets/workspace/Minimap/MapCanvas.prefab");
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
            var minimap = canvas.AddComponent<Minimap>();
            Debugging.Log("Loader", "Initialized Minimap");
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
