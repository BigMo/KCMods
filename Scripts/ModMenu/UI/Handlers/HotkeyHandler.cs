using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;
using Zat.ModMenu.UI.Entries;
using Zat.Shared.ModMenu.API;

namespace Zat.ModMenu.UI.Handlers
{
    public class HotkeyHandler : IEntryHandler
    {
        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/ButtonEntry.prefab")) as GameObject;
            var button = go.AddComponent<HotkeyEntry>();
            button.Setup();
            AssignValue(button, data);
            button.OnKeyChanged.AddListener((key) => {
                data.hotkey = key;
                onUpdate();
            });
            return button;
        }

        private void SetRecording(SettingsEntry setting, HotkeyEntry control, bool recording)
        {
            control.recordKeys = true;
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var button = control as HotkeyEntry;
            if (button == null) throw new Exception($"Entry invalid or null");
            AssignValue(button, data);
        }
        private void AssignValue(HotkeyEntry button, SettingsEntry data)
        {
            button.Name = data.GetPathElements()?.Last();
            button.Description = data.description;
            button.Hotkey = data.hotkey;
        }
    }
}
