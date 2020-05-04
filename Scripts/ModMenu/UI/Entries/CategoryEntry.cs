using UnityEngine;
using TMPro;

namespace Zat.ModMenu.UI.Entries
{
    public class CategoryEntry : MonoBehaviour
    {
        private UnityEngine.UI.Toggle toggle;
        private GameObject content;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI mods;
        private bool initialized = false;
        private CategoryToggle categoryToggle;

        public string Name
        {
            get { return _name?.text; }
            set { if (_name) _name.text = value; }
        }
        public string Mods
        {
            get { return mods?.text; }
            set { if (mods) mods.text = value; }
        }
        public bool Expanded
        {
            get { return toggle?.isOn ?? false; }
            set
            {
                if (toggle) toggle.isOn = value;
                categoryToggle.UpdateImage();
                ExpandCollapse(value);
            }
        }
        public bool Collapsed
        {
            get { return !Expanded; }
            set { Expanded = !value; }
        }
        public CategoryEntry Parent { get; set; }

        public void Start()
        {
            Setup();
            Expanded = false;
        }

        public void Setup()
        {
            if (initialized) return;
            initialized = true;

            RetrieveControls();
            SetupControls();
        }

        private void RetrieveControls()
        {
            content = transform.Find("Content")?.gameObject;
            foreach (Transform obj in content.transform)
                Destroy(obj.gameObject);
            toggle = transform.Find("Body/Toggle")?.GetComponent<UnityEngine.UI.Toggle>();
            _name = transform.Find("Body/Name")?.GetComponent<TextMeshProUGUI>();
            mods = transform.Find("Body/Mods")?.GetComponent<TextMeshProUGUI>();
        }
        private void SetupControls()
        {
            categoryToggle = toggle.gameObject.AddComponent<CategoryToggle>();

            if (toggle.onValueChanged == null) toggle.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(ExpandCollapse);

            _name.alignment = TextAlignmentOptions.MidlineLeft;
            mods.alignment = TextAlignmentOptions.MidlineRight;
            mods.text = "";
        }

        private void ExpandCollapse(bool expanded)
        {
            content?.SetActive(expanded);
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.GetComponent<RectTransform>());
            if (Parent != null) Parent.UpdateLayout();
            else UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent?.GetComponent<RectTransform>());
        }

        public T GetContentEntry<T>(string name) where T : Component
        {
            if (!content) return null;
            return content.transform.Find(name)?.GetComponent<T>();
        }
        public CategoryEntry GetCategory(string name)
        {
            if (name == null) return null;
            return content.transform.Find(name)?.GetComponent<CategoryEntry>();
        }
        public CategoryEntry GetCategoryByPath(string[] path)
        {
            if (path == null || path.Length == 0) return this;
            var cat = GetCategory(path[0]);
            for (var i = 1; i < path.Length && cat; i++)
                cat = cat.GetCategory(path[i]);
            return cat;
        }

        public void AddContent(GameObject obj)
        {
            if (!content) return;
            obj.transform.SetParent(content.transform, false);
        }
    }
}
