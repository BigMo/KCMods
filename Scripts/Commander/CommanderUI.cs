using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.Reflection;

namespace Zat.Commander
{
    public class CommanderUI : MonoBehaviour
    {
        public static bool Instantiated { get { return go != null; } }
        public static CommanderUI Instance { get; private set; }
        private static GameObject go;

        private GameObject groupContainer;
        private GameObject noneObject;
        private GameObject window;
        private CommandEntry[] entries;
        private readonly KeyCode[] numbers = new KeyCode[] {
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3,
            KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,
            KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9
        };

        private static IEnumerable<UnitSystem.Army> PlayerArmies
        {
            get
            {
                if (!UnitSystem.inst) return new UnitSystem.Army[0];
                return UnitSystem.inst.GetField<List<UnitSystem.Army>>("armies")
                        .Where(a => a.teamId == 0)
                        .Where(a => a.ValidToSelect());
            }
        }
        private static IEnumerable<ISelectable> SelectedObjects
        {
            get
            {
                var selected = GameUI.inst?.GetField<List<ISelectable>>("selectedObjs");
                return selected?.ToArray() ?? new ISelectable[0];
            }
        }
        private static IEnumerable<UnitSystem.Army> SelectedArmies
        {
            get { return SelectedObjects.Where(o => o is UnitSystem.Army).Cast<UnitSystem.Army>(); }
        }

        public void Start()
        {
            if (Instantiated) return;
            Instance = this;
            go = gameObject;

            window = transform.Find("CommanderWindow")?.gameObject;
            groupContainer = transform.Find("CommanderWindow/Groups")?.gameObject;
            noneObject = transform.Find("CommanderWindow/Groups/None")?.gameObject;

            var titleText = transform.Find("CommanderWindow/Header/Title")?.GetComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Midline;
            var noneText = transform.Find("CommanderWindow/Groups/None/Text")?.GetComponent<TextMeshProUGUI>();
            noneText.alignment = TextAlignmentOptions.Midline;

            entries = new CommandEntry[9];
            var prefab = Loader.Assets.GetPrefab("assets/workspace/Commander/SingleSmall.prefab");
            for (int i = 0; i < entries.Length; i++)
            {
                var entryObj = GameObject.Instantiate(prefab);
                var entry = entries[i] = entryObj.AddComponent<CommandEntry>();
                entry.Init();
                entry.Group = CommandGroup.Empty;
                entry.Designation = (i + 1);
                entry.Visible = false;
                entry.OnGroupEmpty.AddListener(() =>
                {
                    ClearGroup(entry);
                    SaveSlots();
                });
                entryObj.transform.SetParent(groupContainer.transform, false);
            }
            noneObject.gameObject.SetActive(true);
            LoadSlots();
        }

        private void ClearGroup(CommandEntry entry)
        {
            entry.Group = CommandGroup.Empty;
            entry.Visible = false;
            noneObject.SetActive(!entries.Any(e => e.Visible));
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(groupContainer.GetComponent<RectTransform>());
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(window.GetComponent<RectTransform>());
        }

        public void Update()
        {
            try
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    if (Input.GetKeyDown(numbers[i]))
                    {
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            var selectedArmies = SelectedArmies.ToArray();
                            var group = new CommandGroup(selectedArmies);
                            entries[i].Group = group;
                            entries[i].Visible = group.HasArmies;
                            SaveSlots();
                        }
                        else if (Input.GetKey(KeyCode.Delete))
                        {
                            ClearGroup(entries[i]);
                            SaveSlots();
                        }
                        else
                        {
                            entries[i].RegisterClick();
                        }
                    }
                }
                noneObject.SetActive(!entries.Any(e => e.Visible));
            }
            catch (Exception ex)
            {
                Debugging.Log("CommanderUI", $"Error: {ex.Message}");
                Debugging.Log("CommanderUI", ex.StackTrace);
            }
        }

        private static string SaveSlotsName { get { return TownNameUI.inst?.townName != null ? $"CommanderSlots-{TownNameUI.inst?.townName}" : null; } }
        private CommandEntrySaveSlot[] SavedSlots
        {
            get
            {
                if (SaveSlotsName == null || !PlayerPrefs.HasKey(SaveSlotsName)) return new CommandEntrySaveSlot[0];
                return JsonConvert.DeserializeObject<CommandEntrySaveSlot[]>(PlayerPrefs.GetString(SaveSlotsName));
            }
        }
        private void SaveSlots()
        {
            var slots = Enumerable.Range(0, 9)
                .Where(i => entries[i] != null && entries[i].Visible)
                .Select(i => new CommandEntrySaveSlot() { slot = i, armies = entries[i].Group.Guids.ToArray() });
            PlayerPrefs.SetString(SaveSlotsName, JsonConvert.SerializeObject(slots));
        }
        public void LoadSlots()
        {
            Debugging.Log("CommanderUI", $"Loading slots, prefsname: \"{SaveSlotsName ?? "null"}\"");
            var slots = SavedSlots;
            Debugging.Log("CommanderUI", $"Restoring Slots: {slots.Length}");
            foreach (var entry in entries) ClearGroup(entry);
            foreach (var slot in slots)
            {
                var armies = PlayerArmies.Where(pa => slot.armies.Contains(pa.guid)).ToArray();
                var group = new CommandGroup(armies);
                entries[slot.slot].Group = group;
                entries[slot.slot].Visible = group.HasArmies;
            }
            noneObject.SetActive(!entries.Any(e => e.Visible));
        }

        private class CommandEntrySaveSlot
        {
            public int slot;
            public Guid[] armies;
        }
    }
}
