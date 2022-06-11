using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.ModMenu
{
    [Mod("ModMenu", "v2.0", "Zat")]
    public class ModMenuSettings
    {
        [Setting("Toggle Key", "The key to toggle the menu on/off")]
        [Hotkey(UnityEngine.KeyCode.O)]
        public InteractiveHotkeySetting ToggleKey { get; private set; }
    }
}
