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
    public class ButtonEntry : BaseEntry
    {
        private Button button;
        private TextMeshProUGUI label;

        public Button.ButtonClickedEvent OnClick
        {
            get { return button?.onClick; }
        }
        public string Label
        {
            get { return label?.text; }
            set { if (label) label.text = value; }
        }

        protected override void RetrieveControls()
        {
            base.RetrieveControls();

            button = transform.Find("Button")?.GetComponent<Button>();
            label = transform.Find("Button/Text")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void SetupControls()
        {
            base.SetupControls();

            label.alignment = TextAlignmentOptions.Midline;
        }
    }
}
