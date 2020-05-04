using UnityEngine;
using UnityEngine.UI;

namespace Zat.Minimap
{
    internal class UnitIndicator : MonoBehaviour
    {
        public static UnitIndicator Create()
        {
            var prefab = Loader.Assets.GetPrefab("assets/workspace/Minimap/UnitIndicatorFrame.prefab");
            if (!prefab) return null;

            var go = GameObject.Instantiate(prefab) as GameObject;
            return go.AddComponent<UnitIndicator>();
        }

        public UnityEngine.Color Color
        {
            get { return image?.color ?? UnityEngine.Color.white; }
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
