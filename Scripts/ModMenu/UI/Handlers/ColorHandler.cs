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
    public class ColorHandler : IEntryHandler
    {
        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/ColorEntry.prefab")) as GameObject;
            var color = go.AddComponent<ColorEntry>();
            color.Setup();
            AssignValue(color, data);
            color.OnValueChange.AddListener((v) =>
            {
                data.color = v;
                onUpdate();
            });
            return color;
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var color = control as ColorEntry;
            if (color == null) throw new Exception($"Entry invalid or null");
            AssignValue(color, data);
        }

        private void AssignValue(ColorEntry color, SettingsEntry data)
        {
            color.Name = data.GetName();
            color.Description = data.description;
            color.Color = data.color;
        }
    }
}
