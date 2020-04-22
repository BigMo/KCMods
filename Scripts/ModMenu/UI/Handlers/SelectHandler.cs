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
    public class SelectHandler : IEntryHandler
    {
        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/SelectEntry.prefab")) as GameObject;
            var select = go.AddComponent<SelectEntry>();
            select.Setup();
            AssignValue(select, data);
            select.OnValueChange?.AddListener((v) =>
            {
                data.select.value = v;
                onUpdate();
            });
            return select;
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            var select = control as SelectEntry;
            if (select == null) throw new Exception($"Entry invalid or null");
            AssignValue(select, data);
        }
        private void AssignValue(SelectEntry select, SettingsEntry data)
        {
            select.Name = data.GetName();
            select.Description = data.description;
            select.Options = data.select.options;
            select.Value = data.select.value;
        }
    }
}
