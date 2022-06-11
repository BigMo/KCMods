using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.SpeedUp
{
    [Mod("SpeedUp", "v1.0", "Zat")]
    public class SpeedUpSettings
    {

        [Setting("Enabled", "Whether or not to enable speed-up")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Enabled { get; private set; }

        [Setting("Toggle Key", "The key to toggle the speed-up on/off")]
        [Hotkey(UnityEngine.KeyCode.O)]
        public InteractiveHotkeySetting ToggleKey { get; private set; }

        [Setting("Multiplier", "New speed to set")]
        [Slider(1.0f,10.0f,2.0f, "100.00%")]
        public InteractiveSliderSetting Multiplier { get; private set; }
    }
}
