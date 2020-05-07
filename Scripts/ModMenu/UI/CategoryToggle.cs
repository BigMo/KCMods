using UnityEngine;
using Zat.Shared;

namespace Zat.ModMenu.UI
{
    /// <summary>
    /// Sets the appropiate plus/minus images on CategorEntries
    /// </summary>
    public class CategoryToggle : MonoBehaviour
    {
        private UnityEngine.UI.Toggle toggle;
        private UnityEngine.UI.Image image;

        public Sprite imageOn, imageOff;
        
        public void Start()
        {
            toggle = GetComponent<UnityEngine.UI.Toggle>();
            image = transform.Find("Background/Checkmark")?.GetComponent<UnityEngine.UI.Image>();
            imageOn = Loader.Assets.GetSprite("assets/workspace/ModMenu/minus.png");
            imageOff = Loader.Assets.GetSprite("assets/workspace/ModMenu/plus.png");
            if (imageOn == null) Debugging.Log("CategoryToggle", "Missing sprite!");
            if (toggle)
            {
                if (toggle.onValueChanged == null) toggle.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
                toggle.onValueChanged.AddListener(UpdateImage);
                toggle.isOn = true;
                UpdateImage(toggle.isOn);
            }
        }

        public void UpdateImage()
        {
            UpdateImage(toggle ? toggle.isOn : false);
        }
        private void UpdateImage(bool isOn)
        {
            if (image)
            {
                var img = isOn ? imageOn : imageOff;
                if (img) image.sprite = img;
            }
        }
    }
}
