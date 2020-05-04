using System.Linq;
using TMPro;

namespace Zat.ModMenu.UI.Entries
{
    public class SelectEntry : BaseEntry
    {
        private TMP_Dropdown dropdown;

        public string[] Options
        {
            get { return dropdown?.options.Select(o => o.text).ToArray(); }
            set { if (dropdown) dropdown.options = value.Select(o => new TMP_Dropdown.OptionData(o)).ToList(); }
        }
        public int Value
        {
            get { return dropdown?.value ?? -1; }
            set { if (dropdown) dropdown.value = value; }
        }
        public TMP_Dropdown.DropdownEvent OnValueChange
        {
            get { return dropdown?.onValueChanged; }
        }

        protected override void RetrieveControls()
        {
            base.RetrieveControls();

            dropdown = transform.Find("Dropdown")?.GetComponent<TMP_Dropdown>();
        }

        protected override void SetupControls()
        {
            base.SetupControls();
        }
    }
}
