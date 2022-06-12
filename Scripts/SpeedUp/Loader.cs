using UnityEngine;
using Zat.Shared;

namespace Zat.SpeedUp
{
    public class Loader : MonoBehaviour
    {
        public static KCModHelper Helper { get; private set; }
        private static GameObject container;

        public void Preload(KCModHelper _helper)
        {
            Debugging.Active = true;
            Helper = Debugging.Helper = _helper;
        }

        public void SceneLoaded(KCModHelper _helper)
        {
            Debugging.Active = true;
            Helper = Debugging.Helper = _helper;
            Debugging.Log("Loader", "SceneLoaded: Instantiating GameObject & SpeedUpComponent...");
            if (container != null)
            {
                Debugging.Log("Loader", "GameObject container already exists; aborting");
                return;
            }
            container = new GameObject("SpeedUp");
            GameObject.DontDestroyOnLoad(container);
            container.AddComponent<SpeedUpComponent>();
            Debugging.Log("Loader", "Running!");
        }
    }
}