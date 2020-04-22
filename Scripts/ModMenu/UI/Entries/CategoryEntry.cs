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
    public class CategoryEntry : MonoBehaviour
    {
        private Toggle toggle;
        private GameObject content;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI mods;
        private bool initialized = false;

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
        public CategoryEntry Parent { get; set; }

        public void Start()
        {
            Setup();
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
            toggle = transform.Find("Body/Toggle")?.GetComponent<Toggle>();
            _name = transform.Find("Body/Name")?.GetComponent<TextMeshProUGUI>();
            mods = transform.Find("Body/Mods")?.GetComponent<TextMeshProUGUI>();
        }
        private void SetupControls()
        {
            var catToggle = toggle.gameObject.AddComponent<CategoryToggle>();

            if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(ExpandCollapse);

            _name.alignment = TextAlignmentOptions.MidlineLeft;
            mods.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void ExpandCollapse(bool expanded)
        {
            content?.SetActive(expanded);
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.GetComponent<RectTransform>());
            if (Parent != null) Parent.UpdateLayout();
            else LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent?.GetComponent<RectTransform>());
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
