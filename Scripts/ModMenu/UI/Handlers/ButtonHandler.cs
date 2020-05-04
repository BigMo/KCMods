using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;
using Zat.ModMenu.UI.Entries;
using Zat.Shared.ModMenu.API;

namespace Zat.ModMenu.UI.Handlers
{
    public class ButtonHandler : IEntryHandler
    {
        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/ButtonEntry.prefab")) as GameObject;
            var button = go.AddComponent<ButtonEntry>();
            button.Setup();
            AssignValue(button, data);
            button.OnStateChanged.AddListener((state) =>
            {
                data.button.state = state;
                onUpdate?.Invoke();
            });
            return button;
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var button = control as ButtonEntry;
            if (button == null) throw new Exception($"Entry invalid or null");
            AssignValue(button, data);
        }
        private void AssignValue(ButtonEntry button, SettingsEntry data)
        {
            button.Name = data.GetPathElements()?.Last();
            button.Description = data.description;
            button.Label = data.button.label;
            //button.State = data.button.state;
        }
    }
}
