using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.Commander
{
    [Mod("Commander", "v2.0", "Zat")]
    public class CommanderSettings
    {
        [Setting("UI Visibility", "Whether or not to show the \"Armies\" UI")]
        [Toggle(true, "Visible")]
        public InteractiveToggleSetting Visibility { get; private set; }

        [Setting("Toggle Hotkey", "What button to press to toggle the \"Armies\" UI on/off")]
        [Hotkey(KeyCode.C)]
        public InteractiveHotkeySetting ToggleKey { get; private set; }


        [Category("GroupKeys")]
        public GroupKeys GroupKeys { get; private set; }
    }

    public class GroupKeys
    {
        [Setting("Group #1", "Hotkey to assign/select Group #1")]
        [Hotkey(KeyCode.Keypad1)]
        public InteractiveHotkeySetting Group1Key { get; private set; }

        [Setting("Group #2", "Hotkey to assign/select Group #2")]
        [Hotkey(KeyCode.Keypad2)]
        public InteractiveHotkeySetting Group2Key { get; private set; }

        [Setting("Group #3", "Hotkey to assign/select Group #3")]
        [Hotkey(KeyCode.Keypad3)]
        public InteractiveHotkeySetting Group3Key { get; private set; }

        [Setting("Group #4", "Hotkey to assign/select Group #4")]
        [Hotkey(KeyCode.Keypad4)]
        public InteractiveHotkeySetting Group4Key { get; private set; }

        [Setting("Group #5", "Hotkey to assign/select Group #5")]
        [Hotkey(KeyCode.Keypad5)]
        public InteractiveHotkeySetting Group5Key { get; private set; }
    }
}
