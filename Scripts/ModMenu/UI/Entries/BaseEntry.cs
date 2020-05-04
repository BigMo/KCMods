using UnityEngine;
using TMPro;

namespace Zat.ModMenu.UI.Entries
{
    public class BaseEntry : MonoBehaviour
    {
        private TextMeshProUGUI _name;
        private TextMeshProUGUI description;
        private bool initialized = false;

        public string Name
        {
            get { return _name?.text; }
            set { if (_name) _name.text = value; }
        }
        public string Description
        {
            get { return description?.text; }
            set { if (description) description.text = value; }
        }

        private void Start()
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

        protected virtual void RetrieveControls()
        {
            _name = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            description = transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        }

        protected virtual void SetupControls()
        {
            if (_name) _name.alignment = TextAlignmentOptions.MidlineLeft;
            if (description) description.alignment = TextAlignmentOptions.Midline;
        }
    }
}
