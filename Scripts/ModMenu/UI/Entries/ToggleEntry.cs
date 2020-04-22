using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Zat.ModMenu.UI.Entries
{
    public class ToggleEntry : BaseEntry
    {
        private Toggle toggle;
        private TextMeshProUGUI label;

        public bool Value
        {
            get { return toggle?.isOn ?? false; }
            set { if (toggle) toggle.isOn = value; }
        }
        public string Label
        {
            get { return label?.text; }
            set { if (label) label.text = value; }
        }
        public Toggle.ToggleEvent OnValueChange
        {
            get { return toggle?.onValueChanged; }
        }

        protected override void RetrieveControls()
        {
            base.RetrieveControls();

            toggle = transform.Find("Toggle/Toggle")?.GetComponent<Toggle>();
            label = transform.Find("Toggle")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void SetupControls()
        {
            base.SetupControls();

            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

    }
}
