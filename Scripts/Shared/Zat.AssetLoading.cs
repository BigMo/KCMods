using System.Collections.Generic;
using UnityEngine;

namespace Zat.Shared.AssetLoading
{
    /// <summary>
    /// A simple wrapper that caches assets
    /// </summary>
    public class AssetManager
    {
        private readonly AssetBundle assetBundle;
        private readonly Dictionary<string, UnityEngine.Object> assets;
        public bool BundleLoaded { get { return assetBundle != null; } }

        /// <summary>
        /// Initializes a new AssetManager
        /// </summary>
        /// <param name="bundlePath">The path of the folder that contains the asset bundle</param>
        /// <param name="bundleName">The name of the asset bundle</param>
        public AssetManager(string bundlePath, string bundleName)
        {
            assetBundle = KCModHelper.LoadAssetBundle(bundlePath, bundleName);
            assets = new Dictionary<string, UnityEngine.Object>();
        }

        /// <summary>
        /// Loads an asset by path (e.g. "assets/workspace/villager.prefab")
        /// </summary>
        /// <typeparam name="T">The type of the asset (e.g. GameObject or Sprite)</typeparam>
        /// <param name="path">The path of the asset</param>
        /// <returns></returns>
        public T GetAsset<T>(string path) where T : UnityEngine.Object
        {
            if (assets.ContainsKey(path)) return assets[path] as T;
            var asset = assetBundle?.LoadAsset<T>(path);
            if (asset != null) assets[path] = asset;
            return asset;
        }

        /// <summary>
        /// Loads a sprite by path (e.g. "assets/workspace/arrow.png")
        /// </summary>
        /// <param name="path">The path of the sprite</param>
        /// <returns></returns>
        public Sprite GetSprite(string path)
        {
            return GetAsset<Sprite>(path);
        }

        /// <summary>
        /// Loads a prefab by path (e.g. "assets/workspace/villager.prefab")
        /// </summary>
        /// <param name="path">The path of the prefab</param>
        /// <returns></returns>
        public GameObject GetPrefab(string path)
        {
            return GetAsset<GameObject>(path);
        }
    }
}
