using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using Zat.Shared.InterModComm;

namespace Zat.Shared.ModMenu.API
{
    /// <summary>
    /// Invoked when a SettingsEntry was changed
    /// </summary>
    public class SettingsChangedEvent : UnityEvent<SettingsEntry> { }
    /// <summary>
    /// Invoked when a reset was triggered
    /// </summary>
    public class ResetIssuedEvent : UnityEvent { }
    /// <summary>
    /// Used to refer to specific events, methods or objects.
    /// </summary>
    public static class ModSettingsNames
    {
        public static class Events
        {
            /// <summary>
            /// Sent by the mod menu when a setting was changed_
            /// Parameter: SettingsEntry
            /// </summary>
            public static readonly string SettingChanged = "ModMenu::Events::SettingChanged";
            /// <summary>
            /// Sent by the mod menu when the user clicks the "Reset" button
            /// </summary>
            public static readonly string ResetIssued = "ModMenu::Events::ResetIssued";
        }
        public static class Methods
        {
            /// <summary>
            /// Sent by mods when a setting was changed locally:
            /// Parameter: SettingsEntry
            /// Return value: none
            /// </summary>
            public static readonly string UpdateSetting = "ModMenu::Methods::UpdateSetting";
            /// <summary>
            /// Send by mods to register themselves, menu returns a list of saved settings:
            /// Parameter: ModConfig
            /// Return value: SettingsEntry
            /// </summary>
            public static readonly string RegisterMod = "ModMenu::Methods::RegisterMod";
        }
        public static class Objects
        {
            /// <summary>
            /// The name of the gameobject the mod menu is attached to
            /// </summary>
            public static readonly string ModMenuName = "ModUICanvas002";
        }
    }
    /// <summary>
    /// Used to connect to the mod menu and register your mod
    /// </summary>
    public class ModSettingsBootstrapper : MonoBehaviour
    {
        public IMCPort port;
        /// <summary>
        /// Locates the mod menu GameObject, its port and registers the specified mod in the menu.
        /// </summary>
        /// <param name="config">The ModConfig to register in the menu</param>
        /// <param name="onRegistered">Called when the registration was successful</param>
        /// <param name="onError">Called when the registration failed</param>
        /// <param name="retries">The number of registration attempts before failing entirely</param>
        /// <param name="delay">Delay (in seconds) between registration attempts</param>
        public static void Register(ModConfig config, UnityAction<ModSettingsProxy, SettingsEntry[]> onRegistered, UnityAction<Exception> onError, int retries = 30, float delay = 1f)
        {
            var host = new GameObject($"ModMenu::Client::{Guid.NewGuid().ToString()}");
            var bootstrapper = host.AddComponent<ModSettingsBootstrapper>();
            bootstrapper.InitPort(config, onRegistered, onError, retries, delay);
        }

        private void InitPort(ModConfig config, UnityAction<ModSettingsProxy, SettingsEntry[]> onRegistered, UnityAction<Exception> onError, int retries, float delay)
        {
            StartCoroutine(WaitForTarget(config, onRegistered, onError, retries, delay));
        }

        private System.Collections.IEnumerator WaitForTarget(ModConfig config, UnityAction<ModSettingsProxy, SettingsEntry[]> onRegistered, UnityAction<Exception> onError, int retries, float delay)
        {
            port = gameObject.AddComponent<IMCPort>();
            var _retries = retries;
            GameObject modMenu = null;
            do
            {
                yield return new WaitForSeconds(delay);
                modMenu = GameObject.Find(ModSettingsNames.Objects.ModMenuName);
            } while (_retries-- > 0 && modMenu == null);
            if (modMenu == null)
            {
                onError(new Exception($"WaitForTarget: Missing ModMenu GameObject \"{ModSettingsNames.Objects.ModMenuName}\"!"));
                yield break;
            }
            yield return RegisterOnTarget(config, onRegistered, onError, retries, delay);
        }

        private System.Collections.IEnumerator RegisterOnTarget(ModConfig config, UnityAction<ModSettingsProxy, SettingsEntry[]> onRegistered, UnityAction<Exception> onError, int retries, float delay)
        {
            yield return new WaitForSeconds(delay);
            try
            {
                port.RPC<ModConfig, SettingsEntry[]>(ModSettingsNames.Objects.ModMenuName, ModSettingsNames.Methods.RegisterMod, config, 3f, (settings) =>
                    {
                        var proxy = gameObject.AddComponent<ModSettingsProxy>();
                        proxy.port = port;
                        proxy.Config = config;
                        proxy.Setup();
                        onRegistered?.Invoke(proxy, settings);
                        Destroy(this);
                    },
                    (ex) =>
                    {
                        if (retries > 0)
                            StartCoroutine(RegisterOnTarget(config, onRegistered, onError, retries - 1, delay));
                        else
                            onError?.Invoke(ex);
                    }
                );
            }
            catch(Exception ex)
            {
                if (retries > 0)
                    StartCoroutine(RegisterOnTarget(config, onRegistered, onError, retries - 1, delay));
                else
                    onError?.Invoke(ex);
            }
        }
    }
    
    /// <summary>
    /// The ModMenu API client used to interact with the ModMenu
    /// </summary>
    public class ModSettingsProxy : MonoBehaviour
    {
        /// <summary>
        /// Set this to output errors during when debugging
        /// </summary>
        public static KCModHelper Helper { get; set; }
        /// <summary>
        /// IMCPort to communicate through; set by ModSettingsBootstrapper
        /// </summary>
        public IMCPort port;
        /// <summary>
        /// ModConfig associated with this proxy, mirrors the state of the config in the central ModMenu
        /// </summary>
        public ModConfig Config;
        private Dictionary<string, SettingsChangedEvent> settingsEvents = new Dictionary<string, SettingsChangedEvent>();
        private ResetIssuedEvent resetIssuedEvent = new ResetIssuedEvent();

        /// <summary>
        /// Called once by ModSettingsBootstrapper to set up various listeners on the port, effectively registering callbacks for events raised by the ModMenu
        /// </summary>
        internal void Setup()
        {
            port.RegisterReceiveListener<SettingsEntry>(ModSettingsNames.Events.SettingChanged, (handler, source, entry) =>
            {
                try
                {
                    Config.UpdateInternalSetting(entry);
                    if (settingsEvents.ContainsKey(entry.path))
                        settingsEvents[entry.path]?.Invoke(entry);
                }
                catch (Exception ex)
                {
                    if (Helper != null)
                    {
                        Helper.Log($"[ModSettingsProxy] RegisterReceiveListener({ModSettingsNames.Events.SettingChanged}): Failed to process message. {ex.Message}");
                        Helper.Log(ex.StackTrace);
                    }
                }
            });
            port.RegisterReceiveListener(ModSettingsNames.Events.ResetIssued, (handler, source) =>
            {
                try
                {
                    resetIssuedEvent?.Invoke();
                }
                catch (Exception ex)
                {
                    if (Helper != null)
                    {
                        Helper.Log($"[ModSettingsProxy] RegisterReceiveListener({ModSettingsNames.Events.ResetIssued}): Failed to process message. {ex.Message}");
                        Helper.Log(ex.StackTrace);
                    }
                }
            });
        }
        /// <summary>
        /// Registers a callback to be invoked when a setting was changed in the ModMenu by the user.
        /// </summary>
        /// <param name="settingsPath">The path of the setting to register a callback for</param>
        /// <param name="callback">An invokable callback receiving the changed setting</param>
        public void AddSettingsChangedListener(string settingsPath, UnityAction<SettingsEntry> callback)
        {
            if (!settingsEvents.ContainsKey(settingsPath)) settingsEvents[settingsPath] = new SettingsChangedEvent();
            settingsEvents[settingsPath].AddListener(callback);
        }
        public void AddResetIssuedListener(UnityAction callback)
        {
            if (resetIssuedEvent == null) resetIssuedEvent = new ResetIssuedEvent();
            resetIssuedEvent.AddListener(callback);
        }
        /// <summary>
        /// Submits the change of a setting to the ModMenu
        /// </summary>
        /// <param name="entry">The setting to submit</param>
        /// <param name="callback">Invoked when the submission was successful</param>
        /// <param name="error">Invoked when an error occured</param>
        public void UpdateSetting(SettingsEntry entry, UnityAction callback, UnityAction<Exception> error)
        {
            port.RPC(ModSettingsNames.Objects.ModMenuName, ModSettingsNames.Methods.UpdateSetting, entry, 5f, callback, error);
        }
    }

    /// <summary>
    /// Allows for simple construction of a ModConfig using the Builder pattern
    /// </summary>
    public class ModConfigBuilder
    {
        private ModConfig config;
        private List<SettingsEntry> settings;

        private ModConfigBuilder(string name, string version, string author)
        {
            config = new ModConfig()
            {
                name = name,
                version = version,
                author = author
            };
            settings = new List<SettingsEntry>();
        }

        /// <summary>
        /// Create a new Builder by passing along basic information about a mod
        /// </summary>
        /// <param name="name">Name of the mod (must be unique!)</param>
        /// <param name="version">Version of the mod</param>
        /// <param name="author">The mod's author</param>
        /// <returns></returns>
        public static ModConfigBuilder Create(string name, string version, string author)
        {
            return new ModConfigBuilder(name, version, author);
        }
        /// <summary>
        /// Adds a button setting to the config
        /// </summary>
        /// <param name="path">Path of the setting</param>
        /// <param name="description">Description of the setting</param>
        /// <param name="label">Label of the button</param>
        /// <returns></returns>
        public ModConfigBuilder AddButton(string path, string description, string label)
        {
            settings.Add(new SettingsEntry()
            {
                path = path,
                description = description,
                type = EntryType.Button,
                button = new Button()
                {
                    label = label
                }
            });
            return this;
        }
        /// <summary>
        /// Adds a select (dropdown) setting to the config
        /// </summary>
        /// <param name="path">Path of the setting</param>
        /// <param name="description">Description of the setting</param>
        /// <param name="value">The selected option index</param>
        /// <param name="options">The options to allow the user to choose from</param>
        /// <returns></returns>
        public ModConfigBuilder AddSelect(string path, string description, int value, string[] options)
        {
            settings.Add(new SettingsEntry()
            {
                path = path,
                description = description,
                type = EntryType.Select,
                select = new Select()
                {
                    options = options,
                    value = value
                }
            });
            return this;
        }
        /// <summary>
        /// Adds a slider setting to the config
        /// </summary>
        /// <param name="path">Path of the setting</param>
        /// <param name="description">Description of the setting</param>
        /// <param name="label">Label of the slider</param>
        /// <param name="min">Minimum configurable value</param>
        /// <param name="max">Maximum configurable value</param>
        /// <param name="wholeNumbers">Whether the slider allows for picking whole numbers only (default false)</param>
        /// <param name="value">Value of the slider</param> //TODO: wholeNumbers!
        /// <returns></returns>
        public ModConfigBuilder AddSlider(string path, string description, string label, float min, float max, bool wholeNumbers, float value)
        {
            settings.Add(new SettingsEntry()
            {
                path = path,
                description = description,
                type = EntryType.Slider,
                slider = new Slider()
                {
                    label = label,
                    min = min,
                    max = max,
                    value = value,
                    wholeNumbers = wholeNumbers
                }
            });
            return this;
        }
        /// <summary>
        /// Adds a toggle (checkbox) setting to the config
        /// </summary>
        /// <param name="path">Path of the setting</param>
        /// <param name="description">Description of the setting</param>
        /// <param name="label">Label of the toggle</param>
        /// <param name="value">State of the toggle (true=on, false=off)</param>
        /// <returns></returns>
        public ModConfigBuilder AddToggle(string path, string description, string label, bool value)
        {
            settings.Add(new SettingsEntry()
            {
                path = path,
                description = description,
                type = EntryType.Toggle,
                toggle = new Toggle()
                {
                    label = label,
                    value = value
                }
            });
            return this;
        }
        /// <summary>
        /// Adds a color (picker) setting to the config
        /// </summary>
        /// <param name="path">Path of the setting</param>
        /// <param name="description">Description of the setting</param>
        /// <param name="r">Value of the R channel of the color</param>
        /// <param name="g">Value of the G channel of the color</param>
        /// <param name="b">Value of the B channel of the color</param>
        /// <param name="a">Value of the alpha channel of the color</param>
        /// <returns></returns>
        public ModConfigBuilder AddColor(string path, string description, float r, float g, float b, float a)
        {
            settings.Add(new SettingsEntry()
            {
                path = path,
                description = description,
                type = EntryType.Color,
                color = new Color()
                {
                    r = r,
                    g = g,
                    b = b,
                    a = a
                }
            });
            return this;
        }
        /// <summary>
        /// Compiles the accumulated information into a ModConfig
        /// </summary>
        /// <returns></returns>
        public ModConfig Build()
        {
            config.settings = settings.ToArray();
            return config;
        }
    }

    #region DTOs
    /// <summary>
    /// Represents a mod in the ModMenu, holding information about the mod and the settings it makes use of.
    /// </summary>
    public class ModConfig : Copyable<ModConfig>
    {
        public string name;
        public string version;
        public string author;
        public SettingsEntry[] settings;

        public SettingsEntry this[string path]
        {
            get { return settings?.FirstOrDefault(s => s.path == path); }
        }

        public void CopyFrom(ModConfig other)
        {
            name = other.name;
            version = other.version;
            author = other.author;
            settings = other.settings.Select(s => SettingsEntry.Clone(s)).ToArray();
        }

        /// <summary>
        /// Updates the data of its local setting by copying data from the specified entry
        /// </summary>
        /// <param name="entry">An entry holding new information</param>
        public void UpdateInternalSetting(SettingsEntry entry)
        {
            var own = this[entry.path];
            if (own == null) return;
            own.CopyFrom(entry);
        }

        public override string ToString()
        {
            return $"{name} {version} by {author}";
        }
    }
    public class SettingsEntry : Copyable<SettingsEntry>, Updatable<SettingsEntry>
    {
        /// <summary>
        /// Consists of the category/categories the entry resides in and its name.
        /// e.g. "MyAwesomeMod/Colors/Color #1" resides in the subcategory "Colors" of top-level category "MyAwesomeMod" and is called "Color #1"
        /// Entries with only a name in its path will be put into the "Unspecified" category.
        /// </summary>
        public string path;
        public string description;
        public EntryType type;
        // Data used by individual UI controls
        public Button button;
        public Slider slider;
        public Select select;
        public Toggle toggle;
        public Color color;

        /// <summary>
        /// Returns the individual path elements of this setting's path (split by '/')
        /// </summary>
        /// <returns></returns>
        public string[] GetPathElements() { return path?.Split('/'); }
        /// <summary>
        /// Returns the name component of this setting's path (last path element)
        /// </summary>
        /// <returns></returns>
        public string GetName() { return GetPathElements()?.Last(); }
        /// <summary>
        /// Returns the category path element of this setting's path (path elements except last element)
        /// </summary>
        /// <returns></returns>
        public string[] GetCategoryPath()
        {
            var path = GetPathElements();
            if (path == null || path.Length == 0) return null;
            return path.Take(path.Length - 1).ToArray();
        }
        /// <summary>
        /// Clones the specified setting
        /// </summary>
        /// <param name="other">The setting to clone</param>
        /// <returns></returns>
        public static SettingsEntry Clone(SettingsEntry other)
        {
            var entry = new SettingsEntry();
            entry.CopyFrom(other);
            return entry;
        }
        /// <summary>
        /// Copies information from the specified setting
        /// </summary>
        /// <param name="other">The setting to copy information from</param>
        public void CopyFrom(SettingsEntry other)
        {
            path = other.path;
            description = other.description;
            type = other.type;
            switch (type)
            {
                case EntryType.Button:
                    if (button != null) button.CopyFrom(other.button);
                    else button = other.button;
                    break;
                case EntryType.Color:
                    if (color != null) color.CopyFrom(other.color);
                    else color = other.color;
                    break;
                case EntryType.Select:
                    if (select != null) select.CopyFrom(other.select);
                    else select = other.select;
                    break;
                case EntryType.Slider:
                    if (slider != null) slider.CopyFrom(other.slider);
                    else slider = other.slider;
                    break;
                case EntryType.Toggle:
                    if (toggle != null) toggle.CopyFrom(other.toggle);
                    else toggle = other.toggle;
                    break;
            }
        }

        public bool UpdateableFrom(SettingsEntry other)
        {
            if (description != other.description || type != other.type) return true;
            switch(type)
            {
                case EntryType.Button:
                    if (button != null) return button.UpdateableFrom(other.button);
                    break;
                case EntryType.Color:
                    if (color != null) return color.UpdateableFrom(other.color);
                    break;
                case EntryType.Select:
                    if (select != null) return select.UpdateableFrom(other.select);
                    break;
                case EntryType.Slider:
                    if (slider != null) return slider.UpdateableFrom(other.slider);
                    break;
                case EntryType.Toggle:
                    if (toggle != null) return toggle.UpdateableFrom(other.toggle);
                    break;
            }
            return false;
        }
    }
    /// <summary>
    /// Defines functionality to copy information from other objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface Copyable<T>
    {
        void CopyFrom(T other);
    }
    /// <summary>
    /// Defines functionality to determine whether another object provides any information different from its own
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface Updatable<T>
    {
        bool UpdateableFrom(T other);
    }
    public class Button : Copyable<Button>, Updatable<Button>
    {
        public string label;

        public void CopyFrom(Button other)
        {
            label = other.label;
        }

        public bool UpdateableFrom(Button other)
        {
            return label != other.label;
        }
    }
    public class Slider : Copyable<Slider>, Updatable<Slider>
    {
        public float min, max, value;
        public bool wholeNumbers;
        public string label;

        public void CopyFrom(Slider other)
        {
            min = other.min;
            max = other.max;
            value = other.value;
            label = other.label;
            wholeNumbers = other.wholeNumbers;
        }

        public bool UpdateableFrom(Slider other)
        {
            return min != other.min || max != other.max || value != other.value || wholeNumbers != other.wholeNumbers || label != other.label;
        }
    }
    public class Select : Copyable<Select>, Updatable<Select>
    {
        public string[] options;
        public int value;

        public void CopyFrom(Select other)
        {
            options = other.options;
            value = other.value;
        }

        public bool UpdateableFrom(Select other)
        {
            return value != other.value || !options.SequenceEqual(other.options);
        }
    }
    public class Toggle : Copyable<Toggle>, Updatable<Toggle>
    {
        public bool value;
        public string label;

        public void CopyFrom(Toggle other)
        {
            value = other.value;
            label = other.label;
        }

        public bool UpdateableFrom(Toggle other)
        {
            return value != other.value || label != other.label;
        }
    }
    public class Color : IEquatable<Color>, Copyable<Color>, Updatable<Color>
    {
        public float r, g, b, a;

        public void CopyFrom(Color other)
        {
            r = other.r;
            g = other.g;
            b = other.b;
            a = other.a;
        }

        public bool Equals(Color other)
        {
            return r == other.r && g == other.g && b == other.b && a == other.a;
        }

        public override string ToString()
        {
            return $"R({r.ToString("0.00")}) G({g.ToString("0.00")}) B({b.ToString("0.00")}) A({a.ToString("0.00")})";
        }

        public bool UpdateableFrom(Color other)
        {
            return !Equals(other);
        }
    }
    /// <summary>
    /// The type of a setting, determines data type and UI entry type
    /// </summary>
    public enum EntryType : int
    {
        Button = 0,
        Slider = 1,
        Select = 2,
        Toggle = 3,
        Color = 4
    }
    #endregion
}
