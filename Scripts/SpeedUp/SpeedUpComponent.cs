using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.InterModComm;
using Zat.Shared.ModMenu.API;
using Zat.Shared.ModMenu.Interactive;
using Zat.Shared.Reflection;

namespace Zat.SpeedUp
{
    public class SpeedUpComponent : MonoBehaviour
    {
        public static SpeedUpComponent Instance { get; private set; }
        private SpeedUpSettings settings;
        private ModSettingsProxy proxy;
        private object timeManagerInstance;
        private bool customSpeedActive;

        public void Start()
        {
            try { 
                Debugging.Active = true;
                Debugging.Helper = Loader.Helper;
                Debugging.Log("SpeedUp", "Starting SpeedUpComponent...");

                if (Instance != null)
                    throw new Exception("Instance already exists; killing this instance");


                var t = Type.GetType("Assets.TimeManager, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (t == null)
                    throw new Exception("Could not find TimeManager!");
                Debugging.Log("SpeedUp", $"Found TimeManager class: {t.FullName}");
                timeManagerInstance = ZatsReflection.GetFieldInfo(t, "inst", false)?.GetValue(null);
                if (timeManagerInstance == null)
                    throw new Exception("TimeManager instance is null!");
                 
                Instance = this;

                var config = new InteractiveConfiguration<SpeedUpSettings>();
                settings = config.Settings;
                ModSettingsBootstrapper.Register(config.ModConfig, (proxy, saved) =>
                {
                    config.Install(proxy, saved);
                    OnModRegistered(proxy, saved);
                }, (ex) =>
                {
                    Debugging.Log("SpeedUp", $"Failed to register mod: {ex.Message}");
                    Debugging.Log("SpeedUp", ex.StackTrace);
                });
            }
            catch (Exception ex)
            {
                Debugging.Log("SpeedUp", ex.Message);
                Debugging.Log("SpeedUp", ex.StackTrace);
                Destroy(this);
            }
        }

        private void OnModRegistered(ModSettingsProxy proxy, SettingsEntry[] saved)
        {
            try
            {
                this.proxy = proxy;
                if (!proxy)
                {
                    Debugging.Log("SpeedUp", "Failed to register proxy!");
                    return;
                }

                settings.Enabled.OnUpdate.AddListener((setting) =>
                {
                    UpdateSpeed();
                });
                settings.Multiplier.OnUpdate.AddListener((setting) => {
                    settings.Multiplier.Label = $"{(setting.slider.value * 100).ToString("0.00")}%";
                    UpdateSpeed();
                });

                Debugging.Log("SpeedUp", "OnRegisterMod finished");
            }
            catch (Exception ex)
            {
                Debugging.Log("SpeedUp", $"OnRegisterMod failed: {ex.Message}");
                Debugging.Log("SpeedUp", ex.StackTrace);
            }
        }

        private void UpdateSpeed()
        {
            if (!settings.Enabled.Value)
            {
                if (customSpeedActive)
                {
                    customSpeedActive = false;
                    //Revert to regular game speed indices
                    var old = timeManagerInstance.GetFieldValue<int>("speedInUse");
                    timeManagerInstance.SetField("speedInUse", old - 1);
                    SpeedControlUI.inst.SetSpeed(old, false);
                }
            }
            else
            {
                customSpeedActive = true;
                Time.timeScale = settings.Multiplier;
                ClothTimescaleFix.CheckCloth();
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(settings.ToggleKey.Key))
                settings.Enabled.Value = !settings.Enabled.Value;
        }
    }
}
