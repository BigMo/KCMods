using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using Zat.Shared.ModMenu.API;
using Button = UnityEngine.UI.Button;

namespace Zat.ModMenu.UI.Entries
{
    public class HotkeyEntry : BaseEntry
    {
        public class KeyEvent : UnityEvent<Hotkey> { }
        
        private Button button;
        private TextMeshProUGUI label;
        private KeyEvent keyChanged = new KeyEvent();
        public bool recordKeys = false;

        private static readonly KeyCode[] KEYS = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        public KeyEvent OnKeyChanged
        {
            get { return keyChanged; }
        }
        public string Label
        {
            get { return label?.text; }
            set { if (label) label.text = value; }
        }
        public Hotkey Hotkey
        {
            get { return hotkey; }
            set {
                if (hotkey != value)
                {
                    hotkey = value;
                    recordKeys = false;
                    UpdateLabel();
                    keyChanged?.Invoke(hotkey);
                }
            }
        }

        private void UpdateLabel()
        {
            if (recordKeys || hotkey == null)
                label.text = "-";
            else
                label.text = hotkey.ToString();
        }

        private Hotkey hotkey;

        protected override void RetrieveControls()
        {
            base.RetrieveControls();

            button = transform.Find("Button")?.GetComponent<Button>();
            label = transform.Find("Button/Text")?.GetComponent<TextMeshProUGUI>();
            button.onClick.AddListener(() =>
            {
                recordKeys = !recordKeys;
                UpdateLabel();
            });
        }

        public void Update()
        {
            if (recordKeys)
            {
                Hotkey hotkey = new Hotkey();
                bool any = false;
                foreach(var key in KEYS)
                {
                    if (Input.GetKey(key))
                    {
                        switch (key)
                        {
                            case KeyCode.LeftControl:
                            case KeyCode.RightControl:
                                hotkey.ctrl = true;
                                break;
                            case KeyCode.LeftAlt:
                            case KeyCode.RightAlt:
                                hotkey.alt = true;
                                break;
                            case KeyCode.LeftShift:
                            case KeyCode.RightShift:
                                hotkey.shift = true;
                                break;
                            case KeyCode.None:
                            case KeyCode.Escape:
                                recordKeys = false;
                                break;
                            default:
                                hotkey.keyCode = (int)key;
                                any = true;
                                break;
                        }
                    }
                }
                if (any) Hotkey = hotkey;
            }
        }

        protected override void SetupControls()
        {
            base.SetupControls();

            label.alignment = TextAlignmentOptions.Midline;
        }
    }
}
