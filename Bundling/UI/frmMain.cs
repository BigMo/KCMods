using Newtonsoft.Json;
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

namespace Bundling
{
    public partial class frmMain : Form
    {
        private List<ModBundleDefinition> mods = new List<ModBundleDefinition>();
        private FileInfo saveFile;

        public FileInfo SaveFile
        {
            get => saveFile;
            private set
            {
                if (saveFile != value)
                {
                    saveFile = value;
                    saveToolStripMenuItem.Enabled = saveFile != null;
                }
            }
        }

        public List<ModBundleDefinition> Mods
        {
            get => mods;
            private set
            {
                if (mods != value)
                {
                    mods = value;
                    modCurrent.AllMods = mods;
                    UpdateListbox();
                }
            }
        }

        public frmMain()
        {
            InitializeComponent();
            modCurrent.AllMods = mods;
            modCurrent.ActiveMod = null;
            modCurrent.ModNameChanged += (s, e) => UpdateListbox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mods.Add(new ModBundleDefinition() { BundleName = "New mod" });
            UpdateListbox();
            modCurrent.UpdateDepList();
        }

        private void UpdateListbox()
        {
            var existing = modList.Items.Cast<ModBundleDefinition>().ToArray();
            var missing = mods.Except(existing).ToArray();
            var toDelete = existing.Where(e => !mods.Contains(e)).ToArray();
            foreach (var toDel in toDelete) modList.Items.Remove(toDel);
            modList.Items.AddRange(missing);
            modList.Refresh();
        }

        private void modList_SelectedValueChanged(object sender, EventArgs e)
        {
            modCurrent.ActiveMod = modList.SelectedItem as ModBundleDefinition;
            bundleToolStripMenuItem.Enabled = modList.SelectedItem != null;
        }

        private void bundleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var manager = new ModBundleManager(mods);
                manager.Bundle(modCurrent.ActiveMod);
                MessageBox.Show("Mod successfully deployed!", "Bundling", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bundling failed:\n" + ex.Message, "Bundling", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile = null;
            Mods = new List<ModBundleDefinition>();
            modCurrent.ActiveMod = null;
            modCurrent.AllMods = new List<ModBundleDefinition>();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var diag = new OpenFileDialog())
                {
                    diag.Filter = "json files (*.json)|*.json";
                    diag.Title = "Open mod bundle definition file";
                    diag.Multiselect = false;
                    if (diag.ShowDialog() == DialogResult.OK)
                    {
                        Mods = JsonConvert.DeserializeObject<List<ModBundleDefinition>>(File.ReadAllText(diag.FileName));
                        SaveFile = new FileInfo(diag.FileName);
                        Environment.CurrentDirectory = SaveFile.Directory.FullName;
                        modCurrent.ActiveMod = null;
                        modCurrent.AllMods = mods;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loading failed:\n" + ex.Message, "Bundling", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(saveFile.FullName, JsonConvert.SerializeObject(mods));
                MessageBox.Show("Mod bundle definitions saved!", "Bundling", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Saving failed:\n" + ex.Message, "Bundling", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var diag = new SaveFileDialog())
            {
                diag.Filter = "json files (*.json)|*.json";
                diag.Title = "Save mod bundle definition file";
                if (diag.ShowDialog() == DialogResult.OK)
                {
                    SaveFile = new FileInfo(diag.FileName);
                    Save();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
