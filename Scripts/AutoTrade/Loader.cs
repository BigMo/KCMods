using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.AutoTrade
{
    class Loader : MonoBehaviour
    {
        public static AutoTradeSettings Settings { get; private set; }

        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            Debugging.Helper = kcModHelper;
            //Load up Harmony
            var harmony = HarmonyInstance.Create("AutoTrade");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            try
            {
                //Parse config
                var config = new InteractiveConfiguration<AutoTradeSettings>();
                Settings = config.Settings;

                Debugging.Active = true;
                Debugging.Helper = helper;

                Shared.ModMenu.API.ModSettingsBootstrapper.Register(config.ModConfig,
                    (proxy, oldSettings) =>
                    {
                        config.Install(proxy, oldSettings);
                        Debugging.Log("Loader", "Installed in ModMenu");
                    },
                    (ex) =>
                    {
                        Debugging.Log("Loader", $"Failed to register mod config: {ex.Message}");
                        Debugging.Log("Loader", ex.StackTrace);
                    });
            }
            catch (Exception ex)
            {
                Debugging.Log("Loader", $"Failed to parse interactive mod config: {ex.Message}");
                Debugging.Log("Loader", ex.StackTrace);
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home) && ShipSystem.inst)
                ShipSystem.inst.merchantSpawnTimer = -1f;
        }
    }
}
