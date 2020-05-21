using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Zat.Shared.ModMenu.API;
using Zat.ModMenu.UI.Entries;
using Zat.ModMenu.UI.Handlers;
using Zat.Shared;

namespace Zat.ModMenu
{
    public class SettingsManager
    {
        public class SettingContext
        {
            public SettingsEntry Setting { get; private set; }
            public BaseEntry UIElement { get; private set; }
            public List<ModContext> SubscribedMods { get; private set; }

            public SettingContext(SettingsEntry entry, BaseEntry ui)
            {
                Setting = entry;
                UIElement = ui;
                SubscribedMods = new List<ModContext>();
            }

            public void UpdateSetting(SettingsEntry entry)
            {
                Setting.CopyFrom(entry);
            }
        }
        public class ModContext
        {
            public ModConfig Config { get; private set; }
            public string GameObject { get; private set; }

            public ModContext(ModConfig config, string gameObject)
            {
                Config = config;
                GameObject = gameObject;
            }
        }
        public IEnumerable<string> ModGameObjects { get { return mods.Select(m => m.GameObject); } }
        public IEnumerable<ModConfig> Mods { get { return mods.Select(m => m.Config); } }
        public IEnumerable<BaseEntry> UIElements { get { return settings.Select(s => s.UIElement); } }
        public IEnumerable<SettingsEntry> Settings { get { return settings.Select(s => s.Setting); } }
        public IEnumerable<string> TopLevelCategories
        {
            get
            {
                return settings
                    .Select(s => s.Setting.GetPathElements().FirstOrDefault())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct();
            }
        }

        private readonly List<SettingContext> settings;
        private readonly List<ModContext> mods;
        private readonly GameObject container;
        private readonly UnityAction<SettingsEntry> onUpdate;

        public SettingsManager(GameObject container, UnityAction<SettingsEntry> onUpdate)
        {
            settings = new List<SettingContext>();
            mods = new List<ModContext>();
            this.container = container;
            this.onUpdate = onUpdate;
        }

        public void RegisterMod(string gameObject, ModConfig config)
        {
            try
            {
                if (GetModContextByName(config.name) != null) throw new Exception($"Mod \"{config.name}\" already registered");
                var modContext = new ModContext(config, gameObject);
                mods.Add(modContext);

                foreach (var setting in config.settings)
                {
                    var settingContext = GetSettingByPath(setting.path);
                    if (settingContext == null)
                    {
                        var entry = EntryHandler.Instance.CreateEntry(setting, () => onUpdate(setting));
                        settingContext = new SettingContext(setting, entry);
                        settings.Add(settingContext);
                        var category = GetCreateCategoryChain(setting);
                        category.AddContent(entry.gameObject);
                        category.Expanded = false;
                    }
                    settingContext.SubscribedMods.Add(modContext);
                }

                foreach (var settingContext in settings)
                {
                    var category = GetCategoryByPath(settingContext.Setting.GetCategoryPath());
                    if (!category) continue;
                    category.UpdateLayout();
                }
                Debugging.Log("SettingsManager", $"Registered \"{config.ToString()}\"");

                foreach (var topLevelCategory in TopLevelCategories)
                {
                    var category = GetCategoryByPath(topLevelCategory);
                    if (!category) continue;
                    var _mods = GetSettingsInCategory(topLevelCategory)
                        .SelectMany(s => GetAssociatedMods(s))
                        .Distinct()
                        .Select(m => m.Config.ToString())
                        .ToArray();
                    category.Mods = string.Join(", ", _mods);
                }
            }
            catch (Exception ex)
            {
                Debugging.Log("SettingsManager", $"Failed to register mod {config.ToString()}: {ex.Message}");
                Debugging.Log("SettingsManager", ex.StackTrace);
                throw ex;
            }
            finally
            {
                Canvas.ForceUpdateCanvases();
            }
        }

        public SettingContext GetSettingByPath(string path)
        {
            return settings.FirstOrDefault(s => s.Setting.path == path);
        }

        public ModContext GetModContextByName(string name)
        {
            return mods.FirstOrDefault(m => m.Config.name == name);
        }

        public IEnumerable<ModContext> GetAssociatedMods(SettingsEntry setting)
        {
            var context = settings.FirstOrDefault(s => s.Setting.path == setting.path);
            if (context == null) throw new Exception($"Setting \"{setting.path}\" was not registered!");
            return context.SubscribedMods;
        }

        public IEnumerable<SettingsEntry> GetSettingsInCategory(string toplevel)
        {
            return settings.Select(s => s.Setting).Where(s => s.GetCategoryPath().FirstOrDefault() == toplevel);
        }

        private CategoryEntry GetCreateCategoryChain(SettingsEntry setting)
        {
            CategoryEntry parent = null, child = null;
            var categoryPath = setting.GetCategoryPath();
            if (categoryPath == null || categoryPath.Length == 0)
            {
                parent = GetCreateUnspecifiedCategory();
            }
            else
            {
                var idx = 0;
                do
                {
                    child = idx == 0 ?
                        container?.transform.Find(categoryPath[0])?.GetComponent<CategoryEntry>()
                        : parent.GetCategory(categoryPath[idx]);
                    if (child == null)
                    {
                        child = CreateCategory(categoryPath[idx]);
                        if (idx == 0) child.transform.SetParent(container?.transform, false);
                        else parent.AddContent(child.gameObject);
                        child.Parent = parent;
                    }
                    parent = child;
                } while (++idx < categoryPath.Length);
            }

            return parent;
        }

        private CategoryEntry CreateCategory(string name)
        {
            var go = GameObject.Instantiate(Loader.Assets.GetPrefab("assets/workspace/ModMenu/Category.prefab")) as GameObject;
            if (!go) return null;
            go.name = name;
            var cat = go.AddComponent<CategoryEntry>();
            cat.Setup();
            cat.Name = name;
            return cat;
        }

        private CategoryEntry GetCreateUnspecifiedCategory()
        {
            var unspecified = GetCategoryByPath("Unspecified");
            if (unspecified == null)
                unspecified = CreateCategory("Unspecified");
            unspecified.transform.SetParent(container?.transform, false);
            return unspecified;
        }
        private CategoryEntry GetCategoryByPath(string categoryPath)
        {
            return categoryPath != null ? GetCategoryByPath(categoryPath.Split('/')) : null;
        }
        private CategoryEntry GetCategoryByPath(string[] categoryPath)
        {
            if (categoryPath == null || categoryPath.Length == 0) return null;
            var first = categoryPath[0];
            var cat = container?.transform.Find(first)?.GetComponent<CategoryEntry>();
            if (categoryPath.Length == 1) return cat;
            return cat.GetCategoryByPath(categoryPath.Skip(1).ToArray());
        }
    }
}
