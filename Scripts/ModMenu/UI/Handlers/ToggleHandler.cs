using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using Zat.ModMenu.UI.Entries;
using Zat.Shared.ModMenu.API;

namespace Zat.ModMenu.UI.Handlers
{
    public class ToggleHandler : IEntryHandler
    {
        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/ToggleEntry.prefab")) as GameObject;
            var toggle = go.AddComponent<ToggleEntry>();
            toggle.Setup();
            AssignValue(toggle, data);
            toggle.OnValueChange?.AddListener((v) =>
            {
                data.toggle.value = v;
                onUpdate();
            });
            return toggle;
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var toggle = control as ToggleEntry;
            if (toggle == null) throw new Exception($"Entry invalid or null");
            AssignValue(toggle, data);
        }
        private void AssignValue(ToggleEntry toggle, SettingsEntry data)
        {
            toggle.Name = data.GetName();
            toggle.Description = data.description;
            toggle.Label = data.toggle.label;
            toggle.Value = data.toggle.value;
        }
    }
}
