using TMPro;

namespace Zat.ModMenu.UI.Entries
{
    public class SliderEntry : BaseEntry
    {
        private UnityEngine.UI.Slider slider;
        private TextMeshProUGUI label;

        public float Minimum
        {
            get { return slider?.minValue ?? -1; }
            set { if (slider) slider.minValue = value; }
        }
        public float Maximum
        {
            get { return slider?.maxValue ?? -1; }
            set { if (slider) slider.maxValue = value; }
        }
        public float Value
        {
            get { return slider?.value ?? -1; }
            set { if (slider) slider.value = value; }
        }
        public string Label
        {
            get { return label?.text; }
            set { if (label) label.text = value; }
        }
        public bool WholeNumbers
        {
            get { return slider?.wholeNumbers ?? false; }
            set { if (slider) slider.wholeNumbers = value; }
        }
        public UnityEngine.UI.Slider.SliderEvent OnValueChange
        {
            get { return slider?.onValueChanged; }
        }

        protected override void RetrieveControls()
        {
            base.RetrieveControls();

            slider = transform.Find("Slider/Value")?.GetComponent<UnityEngine.UI.Slider>();
            label = transform.Find("Slider/Label")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void SetupControls()
        {
            base.SetupControls();
            if (label) label.alignment = TextAlignmentOptions.MidlineRight;
        }
    }
}
