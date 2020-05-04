using TMPro;
using UnityEngine.Events;

namespace Zat.ModMenu.UI.Entries
{
    public class ColorEntry : BaseEntry
    {
        private UnityEngine.UI.Slider sliderA, sliderR, sliderG, sliderB;
        private UnityEngine.UI.Image image;
        private TextMeshProUGUI labelA, labelR, labelB, labelG;
        private ColorChangeEvent colorChange;
        public class ColorChangeEvent : UnityEvent<Zat.Shared.ModMenu.API.Color> { }
        
        public Zat.Shared.ModMenu.API.Color Color
        {
            get
            {
                if (image)
                    return new Zat.Shared.ModMenu.API.Color()
                    {
                        a = image.color.a,
                        r = image.color.r,
                        g = image.color.g,
                        b = image.color.b,
                    };
                return new Zat.Shared.ModMenu.API.Color();
            }
            set
            {
                if (!Color.Equals(value) && image)
                {
                    image.color = new UnityEngine.Color(value.r, value.g, value.b, value.a);
                    if (labelA) labelA.text = $"A: {value.a.ToString("0.00")}";
                    if (labelR) labelR.text = $"R: {value.r.ToString("0.00")}";
                    if (labelG) labelG.text = $"G: {value.g.ToString("0.00")}";
                    if (labelB) labelB.text = $"B: {value.b.ToString("0.00")}";
                    if (sliderA) sliderA.value = value.a;
                    if (sliderR) sliderR.value = value.r;
                    if (sliderG) sliderG.value = value.g;
                    if (sliderB) sliderB.value = value.b;
                    colorChange?.Invoke(value);
                }
            }
        }
        public ColorChangeEvent OnValueChange
        {
            get { return colorChange; }
        }
        
        protected override void RetrieveControls()
        {
            base.RetrieveControls();
            colorChange = new ColorChangeEvent();

            image = transform.Find("RGB/Color")?.GetComponent<UnityEngine.UI.Image>();
            sliderA = transform.Find("RGB/Channels/A/Value")?.GetComponent<UnityEngine.UI.Slider>();
            sliderR = transform.Find("RGB/Channels/R/Value")?.GetComponent<UnityEngine.UI.Slider>();
            sliderG = transform.Find("RGB/Channels/G/Value")?.GetComponent<UnityEngine.UI.Slider>();
            sliderB = transform.Find("RGB/Channels/B/Value")?.GetComponent<UnityEngine.UI.Slider>();
            labelA = transform.Find("RGB/Channels/A/Label")?.GetComponent<TextMeshProUGUI>();
            labelR = transform.Find("RGB/Channels/R/Label")?.GetComponent<TextMeshProUGUI>();
            labelG = transform.Find("RGB/Channels/G/Label")?.GetComponent<TextMeshProUGUI>();
            labelB = transform.Find("RGB/Channels/B/Label")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void SetupControls()
        {
            base.SetupControls();
            colorChange = new ColorChangeEvent();
            if (labelA) labelA.alignment = TextAlignmentOptions.MidlineRight;
            if (labelR) labelR.alignment = TextAlignmentOptions.MidlineRight;
            if (labelG) labelG.alignment = TextAlignmentOptions.MidlineRight;
            if (labelB) labelB.alignment = TextAlignmentOptions.MidlineRight;
            if (sliderA) sliderA.onValueChanged.AddListener(UpdateColor);
            if (sliderR) sliderR.onValueChanged.AddListener(UpdateColor);
            if (sliderG) sliderG.onValueChanged.AddListener(UpdateColor);
            if (sliderB) sliderB.onValueChanged.AddListener(UpdateColor);
        }

        private void UpdateColor(float v)
        {
            Color = new Zat.Shared.ModMenu.API.Color()
            {
                a = sliderA?.value ?? 0,
                r = sliderR?.value ?? 0,
                g = sliderG?.value ?? 0,
                b = sliderB?.value ?? 0
            };
        }
    }
}
