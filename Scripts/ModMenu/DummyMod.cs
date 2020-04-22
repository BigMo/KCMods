using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zat.Shared.ModMenu.API;

namespace Zat.DummyMod
{
    public class DummyMod : MonoBehaviour
    {
        public static KCModHelper helper;
        // This reference allows you to interact with the UI
        public static ModSettingsProxy SettingsProxy;

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            DummyMod.helper = helper;
            Shared.InterModComm.IMCPort.helper = helper;
            if (!SettingsProxy) //Have the API bootstrap your settings proxy
                ModSettingsBootstrapper.Register(ModConfigBuilder.Create("DummyMod", "v1", "Zat")
                    .AddButton("Dummy Mod/Dummy Button", "Does very little to nothing", "Surprise!")
                    .AddSlider("Dummy Mod/Dummy Slider 1", "Slidey slidey (float)", "x: 0.50", 0, 1f, false, 0.5f)
                    .AddSlider("Dummy Mod/Dummy Slider 2", "Slidey slidey (int)", "x: 50", 0, 100f, true, 50f)
                    .AddColor("Dummy Mod/Dummy Color", "Color, noice.", 1f,0f,0f,1f)
                    .AddSelect("Dummy Mod/Dummy Select", "Select something", 0, new string[] { "oof", "Oof", "OOF", "(big oof)"})
                    .AddToggle("Dummy Mod/Dummy Toggle", "Turning stuff on/off", "Status: on", true)
                    .AddButton("Dummy Mod/Level 1/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1.2/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2.1/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2.1/Useless Button2", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2.1/Useless Button3", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2.1/Useless Button4", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2.2/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2.3/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2/Level 3/Useless Button", "Not wired up", "")
                    .AddButton("Dummy Mod/Level 1/Level 2/Level 3/Level 4/Useless Button", "Not wired up", "")
                    .Build(),
                     OnProxyRegistered,
                     OnProxyRegisterError);
        }

        private void OnProxyRegisterError(Exception ex)
        {
            helper.Log($"Failed to register proxy: {ex.Message}\n{ex.StackTrace}");
        }

        // Gets called when the mod was registered in the UI
        private void OnProxyRegistered(ModSettingsProxy proxy, SettingsEntry[] saved)
        {
            try
            {
                SettingsProxy = proxy;
                helper.Log("Registered proxy!");
                proxy.AddSettingsChangedListener("Dummy Mod/Dummy Button", (setting) =>
                {
                    setting.button.label = UnityEngine.Random.Range(10, 99).ToString("00");
                    setting.description = $"Set value to {setting.button.label}";
                    proxy.UpdateSetting(setting, () => PrintSuccessUpdate(setting), (ex) => PrintFailUpdate(setting, ex));

                });
                proxy.AddSettingsChangedListener("Dummy Mod/Dummy Slider 1", (setting) =>
                {
                    setting.slider.label = $"x: {setting.slider.value.ToString("0.00")}";
                    proxy.UpdateSetting(setting, () => PrintSuccessUpdate(setting), (ex) => PrintFailUpdate(setting, ex));
                });
                proxy.AddSettingsChangedListener("Dummy Mod/Dummy Slider 2", (setting) =>
                {
                    setting.slider.label = $"x: {setting.slider.value.ToString()}";
                    proxy.UpdateSetting(setting, () => PrintSuccessUpdate(setting), (ex) => PrintFailUpdate(setting, ex));
                });
                proxy.AddSettingsChangedListener("Dummy Mod/Dummy Color", (setting) =>
                {
                    setting.description = $"Color: {setting.color.r.ToString("0.00")}/{setting.color.g.ToString("0.00")}/{setting.color.b.ToString("0.00")}/{setting.color.a.ToString("0.00")}";
                    proxy.UpdateSetting(setting, () => PrintSuccessUpdate(setting), (ex) => PrintFailUpdate(setting, ex));
                });
                proxy.AddSettingsChangedListener("Dummy Mod/Dummy Toggle", (setting) =>
                {
                    setting.toggle.label = $"Status: {(setting.toggle.value ? "On" : "Off")}";
                    proxy.UpdateSetting(setting, () => PrintSuccessUpdate(setting), (ex) => PrintFailUpdate(setting, ex));
                });
                proxy.AddSettingsChangedListener("Dummy Mod/Dummy Select", (setting) =>
                {
                    setting.description = $"Oof size: {setting.select.options[setting.select.value]}";
                    proxy.UpdateSetting(setting, () => PrintSuccessUpdate(setting), (ex) => PrintFailUpdate(setting, ex));

                });

                //Apply saved values
                foreach(var setting in saved)
                {
                    var own = proxy.Config[setting.path];
                    if (own != null)
                    {
                        own.CopyFrom(setting);
                        proxy.UpdateSetting(own, null, (ex) => PrintFailUpdate(own, ex));
                    }
                }
            }
            catch(Exception ex)
            {
                helper.Log($"Failed to configure proxy: {ex.Message}");
                helper.Log(ex.StackTrace);
            }
        }

        private void PrintSuccessUpdate(SettingsEntry entry)
        {
        }
        private void PrintFailUpdate(SettingsEntry entry, Exception ex)
        {
            DummyMod.helper.Log($"Failed to update \"{entry.path}\": {ex.Message}");
        }


        //Before scene loads
        void PreScriptLoad(KCModHelper kcModHelper)
        {
            helper = kcModHelper;
            //Load up Harmony
            var harmony = HarmonyInstance.Create("MinimapLoader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
