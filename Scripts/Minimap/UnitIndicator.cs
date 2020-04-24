using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Zat.Shared.ModMenu.API;
using Color = UnityEngine.Color;
using Zat.Shared.Rendering;
using Zat.Shared.Reflection;
using UnityEngine.UI;
using TMPro;
using Button = UnityEngine.UI.Button;
using UnityEngine.EventSystems;
using Zat.Shared.UI.Utilities;

namespace Zat.Minimap
{
    internal class UnitIndicator : MonoBehaviour
    {
        public static UnitIndicator Create()
        {
            var prefab = Loader.Assets.GetPrefab("assets/workspace/Minimap/UnitIndicator.prefab");
            if (!prefab) return null;

            var go = GameObject.Instantiate(prefab) as GameObject;
            return go.AddComponent<UnitIndicator>();
        }

        public Color Color
        {
            get { return image?.color ?? Color.white; }
            set { if (image) image.color = value; }
        }
        public Vector2 Size
        {
            get { return rectSize?.sizeDelta ?? Vector2.zero; }
            set { if (rectSize) rectSize.sizeDelta = value; }
        }
        public Vector2 Position
        {
            get { return rectPos?.anchoredPosition ?? Vector2.zero; }
            set { if (rectPos) rectPos.anchoredPosition = value; }
        }

        private Image image;
        private RectTransform rectSize, rectPos;

        public void Start()
        {
            rectPos = GetComponent<RectTransform>();
            rectSize = transform.Find("Image")?.GetComponent<RectTransform>();
            image = transform.Find("Image")?.GetComponent<Image>();
        }
    }
}
