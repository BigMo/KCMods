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
    public class EntryHandler : IEntryHandler
    {
        private Dictionary<EntryType, IEntryHandler> handlers;

        public static EntryHandler Instance { get { return instance ?? (instance = new EntryHandler()); } }
        private static EntryHandler instance;

        private EntryHandler()
        {
            handlers = new Dictionary<EntryType, IEntryHandler>();
            handlers[EntryType.Button] = new ButtonHandler();
            handlers[EntryType.Color] = new ColorHandler();
            handlers[EntryType.Select] = new SelectHandler();
            handlers[EntryType.Slider] = new SliderHandler();
            handlers[EntryType.Toggle] = new ToggleHandler();
            handlers[EntryType.Hotkey] = new HotkeyHandler();
        }

        public BaseEntry CreateEntry(SettingsEntry data, UnityAction onUpdate)
        {
            if (!handlers.ContainsKey(data.type)) throw new Exception($"Missing handler for value type \"{data.type}\"");
            return handlers[data.type].CreateEntry(data, onUpdate);
        }

        public void UpdateEntry(SettingsEntry data, BaseEntry control)
        {
            if (!handlers.ContainsKey(data.type)) throw new Exception($"Missing handler for value type \"{data.type}\"");
            handlers[data.type].UpdateEntry(data, control);
        }
    }
}
