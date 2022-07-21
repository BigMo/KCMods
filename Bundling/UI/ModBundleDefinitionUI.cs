using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bundling.UI
{
    public partial class ModBundleDefinitionUI : UserControl
    {
        private ModBundleDefinition mod;
        private List<ModBundleDefinition> allMods = new List<ModBundleDefinition>();
        public List<ModBundleDefinition> AllMods
        {
            get => allMods;
            set
            {
                allMods = value;
                UpdateDepList();
            }
        }
        public ModBundleDefinition ActiveMod
        {
            get => mod;
            set
            {
                if (mod != value)
                {
                    mod = value;
                    Enabled = mod != null;
                    UpdateDepList();
                    UpdateElements();
                }
            }
        }
        public event EventHandler ModNameChanged;

        private void UpdateElements()
        {
            SetText(defGenBndl, mod?.BundleName);
            SetText(defGenSrc, mod?.SourceDirectory);
            SetText(defGenTgt, mod?.TargetDirectory);
            SetText(defAbName, mod?.AssetBundleName);
            SetText(defAbSrc, mod?.AssetBundleSource);
            //SetText(defMscExp, mod?.ExcludePatterns); //TODO: Implement
            defMscAdep.Checked = mod?.Deploy ?? false;
            defMscMin.Checked = mod?.Minify ?? false;
            foreach (var item in defDeps.Items.Cast<ListViewItem>())
                item.Checked = mod != null && mod.Dependencies.Contains((item.Tag as ModBundleDefinition).BundleName);

            Enabled = mod != null;
        }

        public void UpdateDepList()
        {
            if (AllMods == null)
            {
                defDeps.Items.Clear();
                return;
            }
            var existing = defDeps.Items
                .Cast<ListViewItem>()
                .ToArray();
            var missing = AllMods
                .Except(existing.Select(e=>e.Tag as ModBundleDefinition))
                .ToArray();
            var toDelete = existing
                .Where(e => !AllMods.Contains(e.Tag as ModBundleDefinition))
                .ToArray();

            foreach (var toDel in toDelete)
                defDeps.Items.Remove(toDel);
            //var selfItem = defDeps.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag != null && i.Tag == ActiveMod);
            //if (selfItem != null) defDeps.Items.Remove(selfItem);
            foreach (var ex in existing)
                ex.Text = (ex.Tag as ModBundleDefinition).BundleName;

            defDeps.Items.AddRange(missing.Where(m => m != mod).Select(m => new ListViewItem() { Text = m.BundleName, Tag = m, Checked = m.Dependencies.Contains(m.BundleName) }).ToArray());
        }

        private void SetText(TextBox tbx, string value)
        {
            tbx.Text = value ?? "";
        }

        private void BindButtonSelectDir(Button btn, TextBox output)
        {
            btn.Click += (s, e) =>
            {
                using (var select = new FolderBrowserDialog())
                {
                    if (Directory.Exists(output.Text)) select.SelectedPath = output.Text;
                    if (select.ShowDialog() == DialogResult.OK)
                        output.Text = select.SelectedPath;
                }
            };
        }

        public ModBundleDefinitionUI()
        {
            InitializeComponent();

            BindButtonSelectDir(defGenSrcBtn, defGenSrc);
            BindButtonSelectDir(defAbSrcBtn, defAbSrc);

            defGenBndl.TextChanged += (s, e) =>
            {
                if (mod != null)
                {
                    mod.BundleName = ((TextBox)s).Text;
                    ModNameChanged?.Invoke(this, null);
                }
            };
            defGenSrc.TextChanged += (s, e) => { if (mod != null) mod.SourceDirectory = ((TextBox)s).Text; };
            defGenTgt.TextChanged += (s, e) => { if (mod != null) mod.TargetDirectory = ((TextBox)s).Text; };
            defAbName.TextChanged += (s, e) => { if (mod != null) mod.AssetBundleName = ((TextBox)s).Text; };
            defAbSrc.TextChanged += (s, e) => { if (mod != null) mod.AssetBundleSource = ((TextBox)s).Text; };
            defMscAdep.CheckedChanged += (s, e) => { if (mod != null) mod.Deploy = ((CheckBox)s).Checked; };
            defMscMin.CheckedChanged += (s, e) => { if (mod != null) mod.Minify = ((CheckBox)s).Checked; };
            defDeps.ItemCheck += (s, e) =>
            {
                var item = defDeps.Items[e.Index];
                if (mod != null && item.Tag == mod)
                {
                    e.NewValue = CheckState.Unchecked;
                    MessageBox.Show("Mod cannot specify dependency to itself!", "Bundling", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            defDeps.ItemChecked += (s, e) =>
            {
                if (mod != null && e.Item.Checked) mod.Dependencies = mod.Dependencies.Concat(new string[] { e.Item.Text }).Distinct().ToArray();
                if (mod != null && !e.Item.Checked) mod.Dependencies = mod.Dependencies.Except(new string[] { e.Item.Text }).Distinct().ToArray();
            };
            //TODO: Implement excl patterns

            defDeps.Items.Clear();
        }
    }
}
