using TMPro;

namespace Zat.ModMenu.UI.Entries
{
    public class ToggleEntry : BaseEntry
    {
        private UnityEngine.UI.Toggle toggle;
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
        public UnityEngine.UI.Toggle.ToggleEvent OnValueChange
        {
            get { return toggle?.onValueChanged; }
        }

        protected override void RetrieveControls()
        {
            base.RetrieveControls();

            toggle = transform.Find("Toggle/Toggle")?.GetComponent<UnityEngine.UI.Toggle>();
            label = transform.Find("Toggle")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void SetupControls()
        {
            base.SetupControls();

            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

    }
}
