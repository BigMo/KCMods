using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.Minimap
{
    [Mod("Minimap", "v1.3.2", "Zat")]
    public class MinimapSettings
    {

        [Setting("Enabled", "Whether or not to show the map")]
        [Toggle(true, "Visible")]
        public InteractiveToggleSetting Enabled { get; private set; }

        [Setting("Key", "What button to press to toggle the map on/off")]
        [Hotkey(KeyCode.M)]
        public InteractiveHotkeySetting Key { get; private set; }

        [Setting("Update Interval", "Interval between minimap updates")]
        [Slider(1, 30, 5, "Every 5s", true)]
        public InteractiveSliderSetting UpdateInterval { get; private set; }

        [Category("Visual")]
        public VisualSettings Visual { get; private set; }
    }

    public class VisualSettings
    {
        [Setting("Size", "Width and height of the map in pixels")]
        [Slider(100,1024,30, "Size: 128px", true)]
        public InteractiveSliderSetting Size { get; private set; }

        [Setting("Position X", "Where the map is placed horizontally")]
        [Slider(0, 1920, 0, "X: 0", true)]
        public InteractiveSliderSetting PositionX { get; private set; }

        [Setting("Position Y", "Where the map is placed vertically")]
        [Slider(0, 1080, 0, "Y: 0", true)]
        public InteractiveSliderSetting PositionY { get; private set; }

        [Category("Indicators")]
        public IndicatorSettings Indicators { get; private set; }
    }

    public class IndicatorSettings
    {

        [Category("Camera")]
        public IndicatorEntry Camera { get; private set; }

        [Category("Armies")]
        public IndicatorEntry Armies { get; private set; }

        [Category("Dragons")]
        public IndicatorEntry Dragons { get; private set; }

        [Category("Vikings")]
        public IndicatorEntry Vikings { get; private set; }
    }

    public class IndicatorEntry
    {
        [Setting("Enabled", "Show/hide armies the indicator(s)")]
        [Toggle(true, "Visible")]
        public InteractiveToggleSetting Enabled { get; private set; }

        [Setting("Color", "Color of the indicator(s)")]
        [Color(1, 1, 1)]
        public InteractiveColorSetting Color { get; private set; }

        [Setting("Size", "Size of the indicator(s)")]
        [Slider(4, 64, 24, "Size: 24px", true)]
        public InteractiveSliderSetting Size { get; private set; }
    }
}
