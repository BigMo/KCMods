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

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var hotKey = control as HotkeyEntry;
            if (hotKey == null) throw new Exception($"Entry invalid or null");
            AssignValue(hotKey, data);
        }
        private void AssignValue(HotkeyEntry hotKey, SettingsEntry data)
        {
            hotKey.Name = data.GetPathElements()?.Last();
            hotKey.Description = data.description;
            hotKey.Hotkey = data.hotkey;
        }
    }
}
