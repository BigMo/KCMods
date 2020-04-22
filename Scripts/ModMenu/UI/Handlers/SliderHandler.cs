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
    public class SliderHandler : IEntryHandler
    {
        private GameObject prefab;

        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/SliderEntry.prefab")) as GameObject;
            var slider = go.AddComponent<SliderEntry>();
            slider.Setup();
            AssignValue(slider, data);
            slider.OnValueChange?.AddListener((v) =>
            {
                data.slider.value = v;
                onUpdate();
            });
            return slider;
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var slider = control as SliderEntry;
            if (slider == null) throw new Exception($"Entry invalid or null");
            AssignValue(slider, data);
        }
        private void AssignValue(SliderEntry slider, SettingsEntry data)
        {
            slider.Name = data.GetName();
            slider.Description = data.description;
            slider.Minimum = data.slider.min;
            slider.Maximum = data.slider.max;
            slider.Label = data.slider.label;
            slider.Value = data.slider.value;
            slider.WholeNumbers = data.slider.wholeNumbers;
        }
    }
}
