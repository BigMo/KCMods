using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.Productivity
{
    class Loader : MonoBehaviour
    {
        public static ProductivitySettings Settings { get; private set; }

        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            Debugging.Helper = kcModHelper;
            //Load up Harmony
            var harmony = HarmonyInstance.Create("Productivity");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            try
            {
                //Parse config
                var config = new InteractiveConfiguration<ProductivitySettings>();
                Settings = config.Settings;

                Debugging.Active = true;
                Debugging.Helper = helper;

                Shared.ModMenu.API.ModSettingsBootstrapper.Register(config.ModConfig,
                    (proxy, oldSettings) => {
                        config.Install(proxy, oldSettings);
                        //Foods
                        Settings.Food.Baker.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Food.Baker.Factor));
                        Settings.Food.Orchard.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Food.Orchard.Factor));
                        Settings.Food.Field.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Food.Field.Factor));
                        Settings.Food.FishingHut.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Food.FishingHut.Factor));
                        //Goods
                        Settings.Goods.CharcoalMaker.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Goods.CharcoalMaker.Factor));
                        Settings.Goods.Blacksmith.Armament.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Goods.Blacksmith.Armament.Factor));
                        Settings.Goods.Blacksmith.Tools.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Goods.Blacksmith.Tools.Factor));
                        //Resources
                        Settings.Resources.IronMine.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Resources.IronMine.Factor));
                        Settings.Resources.Quarry.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Resources.Quarry.Factor));
                        Settings.Resources.Forester.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Resources.Forester.Factor));
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

        private void UpdateLabel(InteractiveSliderSetting slider)
        {
            slider.Label = $"Factor: x{slider.Value.ToString("0.00")}";
        }
    }
}
