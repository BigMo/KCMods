﻿using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.Productivity
{
    class Loader : MonoBehaviour
    {
        public static KCModHelper Helper;
        public static ProductivitySettings Settings { get; private set; }

        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            Helper = kcModHelper;
            //Load up Harmony
            var harmony = HarmonyInstance.Create("Productivity");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            Helper = helper;
            try
            {
                //Parse config
                var config = new Zat.Shared.ModMenu.Interactive.InteractiveConfiguration<ProductivitySettings>();
                Settings = config.Settings;

                Zat.Shared.Debugging.Active = true;
                Zat.Shared.Debugging.Helper = helper;

                Shared.ModMenu.API.ModSettingsBootstrapper.Register(config.ModConfig,
                    (proxy, oldSettings) => config.Install(proxy, oldSettings),
                    (ex) =>
                    {
                        Helper.Log($"Failed to register mod config: {ex.Message}");
                        Helper.Log(ex.StackTrace);
                    });

                Settings.Baker.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Baker.Factor));
                Settings.CharcoalMaker.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.CharcoalMaker.Factor));
                Settings.Field.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Field.Factor));
                Settings.IronMine.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.IronMine.Factor));
                Settings.Orchard.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Orchard.Factor));
                Settings.Quarry.Factor.OnUpdatedRemotely.AddListener((entry) => UpdateLabel(Settings.Quarry.Factor));
            }
            catch (Exception ex)
            {
                Helper.Log($"Failed to parse interactive mod config: {ex.Message}");
                Helper.Log(ex.StackTrace);
            }
        }

        private void UpdateLabel(InteractiveSliderSetting slider)
        {
            slider.Label = $"Factor: x{slider.Value.ToString("0.00")}";
        }
    }
}
