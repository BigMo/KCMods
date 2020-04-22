using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Zat.ModMenu.UI
{
    /// <summary>
    /// Sets the appropiate plus/minus images on CategorEntries
    /// </summary>
    public class CategoryToggle : MonoBehaviour
    {
        private Toggle toggle;
        private Image image;

        public Sprite imageOn, imageOff;
        
        public void Start()
        {
            toggle = GetComponent<Toggle>();
            image = transform.Find("Background/Checkmark")?.GetComponent<Image>();
            imageOn = Loader.Assets.GetSprite("assets/workspace/ModMenu/minus.png");
            imageOff = Loader.Assets.GetSprite("assets/workspace/ModMenu/plus.png");
            if (imageOn == null) Loader.Helper.Log("Missing sprite!");
            if (toggle)
            {
                if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
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
