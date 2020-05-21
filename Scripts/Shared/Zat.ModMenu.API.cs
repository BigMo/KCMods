using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;
using Zat.Shared.InterModComm;
using System.Reflection;
using Zat.Shared.ModMenu.API;

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
            catch (Exception ex)
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
        /// IMCPort to communicate through; set by ModSettingsBootstrapper
        /// </summary>
        public IMCPort port;
        /// <summary>
        /// ModConfig associated with this proxy, mirrors the state of the config in the central ModMenu
        /// </summary>
        public ModConfig Config;
        private readonly Dictionary<string, SettingsChangedEvent> settingsEvents = new Dictionary<string, SettingsChangedEvent>();
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
                    else
                        Debugging.Log("ModSettingsProxy", $"Received update for unregistered setting: {entry.path}");
                }
                catch (Exception ex)
                {
                    Debugging.Log("ModSettingsProxy", $"RegisterReceiveListener({ModSettingsNames.Events.SettingChanged}): Failed to process message. {ex.Message}");
                    Debugging.Log("ModSettingsProxy", ex.StackTrace);
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
                    Debugging.Log("ModSettingsProxy", $"[ModSettingsProxy] RegisterReceiveListener({ModSettingsNames.Events.ResetIssued}): Failed to process message. {ex.Message}");
                    Debugging.Log("ModSettingsProxy", ex.StackTrace);
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
    [Obsolete("Will be removed shortly; migrate to using InteractiveSettings")]
    public class ModConfigBuilder
    {
        private readonly ModConfig config;
        private readonly List<SettingsEntry> settings;

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
        /// Adds a hotkey-button setting to the config
        /// </summary>
        /// <param name="path">Path of the setting</param>
        /// <param name="description">Description of the setting</param>
        /// <param name="keyCode">Initial key</param>
        /// <returns></returns>
        public ModConfigBuilder AddHotkey(string path, string description, int keyCode)
        {
            settings.Add(new SettingsEntry()
            {
                path = path,
                description = description,
                type = EntryType.Hotkey,
                hotkey = new Hotkey()
                {
                    keyCode = keyCode
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
        public Hotkey hotkey;

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
                case EntryType.Hotkey:
                    if (hotkey != null) hotkey.CopyFrom(other.hotkey);
                    else hotkey = other.hotkey;
                    break;
            }
        }

        public bool UpdateableFrom(SettingsEntry other)
        {
            if (description != other.description || type != other.type) return true;
            switch (type)
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
                case EntryType.Hotkey:
                    if (hotkey != null) return hotkey.UpdateableFrom(other.hotkey);
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
        public ButtonState state;

        public void CopyFrom(Button other)
        {
            label = other.label;
            state = other.state;
        }

        public bool UpdateableFrom(Button other)
        {
            return label != other.label || state != other.state;
        }

        public override string ToString()
        {
            return $"[Button] label: \"{label}\", state: {state}";
        }
    }
    public enum ButtonState { Normal, Highlighted, Pressed };
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

        public override string ToString()
        {
            return $"[Slider] min: {min.ToString("0.00")}, max: {max.ToString("0.00")}, value: {value.ToString("0.00")}, wholeNumbers: {wholeNumbers}, label: \"{label}\"";
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

        public override string ToString()
        {
            return $"[Select] value: {value.ToString()}, options: {{{string.Join(", ", options.Select(o => $"\"{o}\"").ToArray())}}}";
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

        public override string ToString()
        {
            return $"[Toggle] value: {value.ToString()}, label: \"{label}\"";
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

        public UnityEngine.Color ToUnityColor()
        {
            return new UnityEngine.Color(r, g, b, a);
        }
        public static Color FromUnityColor(UnityEngine.Color color)
        {
            return new Color()
            {
                r = color.r,
                g = color.g,
                b = color.b,
                a = color.a
            };
        }
    }
    public class Hotkey : IEquatable<Hotkey>, Copyable<Hotkey>, Updatable<Hotkey>
    {
        public int keyCode;
        public bool ctrl;
        public bool alt;
        public bool shift;

        public void CopyFrom(Hotkey other)
        {
            keyCode = other.keyCode;
            ctrl = other.ctrl;
            alt = other.alt;
            shift = other.shift;
        }

        public bool Equals(Hotkey other)
        {
            return keyCode == other.keyCode && ctrl == other.ctrl && alt == other.alt && shift == other.shift;
        }

        public bool UpdateableFrom(Hotkey other)
        {
            return keyCode != other.keyCode || ctrl != other.ctrl || alt != other.alt || shift != other.shift;
        }

        public override string ToString()
        {
            var keys = new List<string>();
            if (ctrl) keys.Add($"[Control]");
            if (alt) keys.Add($"[Alt]");
            if (shift) keys.Add($"[Shift]");
            keys.Add($"[{(KeyCode)keyCode}]");
            return string.Join(" + ", keys.ToArray());
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
        Color = 4,
        Hotkey = 5
    }
    #endregion

}
namespace Zat.Shared.ModMenu.Interactive
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Author { get; private set; }
        public ModAttribute(string name, string version, string author)
        {
            Name = name;
            Version = version;
            Author = author;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class CategoryAttribute : Attribute
    {
        public string Name { get; private set; }
        public CategoryAttribute(string name)
        {
            if (name.Contains("/")) throw new ArgumentException("Category name must not contains a \"/\"!");
            Name = name;
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SettingAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public SettingAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SpecificSettingAttribute : Attribute { }
    public abstract class InteractiveSetting
    {
        public string Name
        {
            get { return Setting.GetName(); }
            set
            {
                if (Setting.GetName() == value) return;
                Setting.path = string.Join("/", Setting.GetCategoryPath().Concat(new string[] { value }));
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public string Description
        {
            get { return Setting.description; }
            set
            {
                if (Setting.description == value) return;
                Setting.description = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public string Path { get { return Setting.path; } }
        public abstract EntryType Type { get; }

        /// <summary>
        /// Invoked when a setting is updated programmatically via the mod itself
        /// </summary>
        public SettingsChangedEvent OnLocalUpdate { get; private set; }
        /// <summary>
        /// Invoked when a setting is updated by the UI and sent to the mod
        /// </summary>
        public SettingsChangedEvent OnUpdatedRemotely { get; private set; }
        /// <summary>
        /// Invoked when a setting is updated
        /// </summary>
        public SettingsChangedEvent OnUpdate { get; private set; }

        protected SettingsEntry Setting { get; private set; }

        protected InteractiveSetting(SettingsEntry entry)
        {
            Setting = entry;
            OnLocalUpdate = new SettingsChangedEvent();
            OnUpdatedRemotely = new SettingsChangedEvent();
            OnUpdate = new SettingsChangedEvent();

            OnLocalUpdate.AddListener((setting) => OnUpdate?.Invoke(setting));
            OnUpdatedRemotely.AddListener((setting) => OnUpdate?.Invoke(setting));
        }

        public abstract void UpdateFromRemote(SettingsEntry entry);
        public void RestoreSetting(SettingsEntry entry)
        {
            Debugging.Log("InteractiveSetting", $"Receiving saved setting for: {entry.path}");
            this.UpdateFromRemote(entry);
            OnLocalUpdate?.Invoke(Setting);
        }
        public void TriggerUpdate()
        {
            OnUpdate?.Invoke(Setting);
        }
        public abstract void Reset();
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SliderAttribute : SpecificSettingAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public float Value { get; private set; }
        public bool WholeNumbers { get; private set; }
        public string Label { get; private set; }
        public SliderAttribute(float min, float max, float value, string label = "", bool wholeNumbers = false)
        {
            Min = min;
            Max = max;
            Value = value;
            WholeNumbers = wholeNumbers;
            Label = label;
        }
    }
    public class InteractiveSliderSetting : InteractiveSetting
    {
        public float Min
        {
            get { return Setting.slider.min; }
            set
            {
                if (Setting.slider.min == value) return;
                Setting.slider.min = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public float Max
        {
            get { return Setting.slider.max; }
            set
            {
                if (Setting.slider.max == value) return;
                Setting.slider.max = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public float Value
        {
            get { return Setting.slider.value; }
            set
            {
                if (Setting.slider.value == value) return;
                Setting.slider.value = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public string Label
        {
            get { return Setting.slider.label; }
            set
            {
                if (Setting.slider.label == value) return;
                Setting.slider.label = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public bool WholeNumbers
        {
            get { return Setting.slider.wholeNumbers; }
            set
            {
                if (Setting.slider.wholeNumbers == value) return;
                Setting.slider.wholeNumbers = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public override EntryType Type { get { return EntryType.Slider; } }
        private readonly SliderAttribute defaultValues;
        public InteractiveSliderSetting(SettingsEntry entry, SliderAttribute values) : base(entry)
        {
            defaultValues = values;
            entry.slider = new API.Slider()
            {
                max = values.Max,
                min = values.Min,
                value = values.Value,
                wholeNumbers = values.WholeNumbers,
                label = values.Label
            };
            entry.type = EntryType.Slider;
        }

        public static implicit operator float(InteractiveSliderSetting slider)
        {
            return slider.Value;
        }

        public override void UpdateFromRemote(SettingsEntry entry)
        {
            Setting.slider.CopyFrom(entry.slider);
            OnUpdatedRemotely?.Invoke(Setting);
        }

        public override void Reset()
        {
            var slider = new API.Slider()
            {
                max = defaultValues.Max,
                min = defaultValues.Min,
                value = defaultValues.Value,
                wholeNumbers = defaultValues.WholeNumbers,
                label = defaultValues.Label
            };
            if (!Setting.slider.UpdateableFrom(slider)) return;
            Setting.slider.CopyFrom(slider);
            OnUpdatedRemotely?.Invoke(Setting);
            OnLocalUpdate?.Invoke(Setting);
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ToggleAttribute : SpecificSettingAttribute
    {
        public bool Value { get; private set; }
        public string Label { get; private set; }
        public ToggleAttribute(bool value, string label = "")
        {
            Value = value;
            Label = label;
        }
    }
    public class InteractiveToggleSetting : InteractiveSetting
    {
        public bool Value
        {
            get { return Setting.toggle.value; }
            set
            {
                if (Setting.toggle.value == value) return;
                Setting.toggle.value = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public string Label
        {
            get { return Setting.toggle.label; }
            set
            {
                if (Setting.toggle.label == value) return;
                Setting.toggle.label = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        private readonly ToggleAttribute defaultValues;
        public InteractiveToggleSetting(SettingsEntry entry, ToggleAttribute values) : base(entry)
        {
            defaultValues = values;
            entry.type = EntryType.Toggle;
            entry.toggle = new API.Toggle()
            {
                value = values.Value,
                label = values.Label
            };
        }

        public override EntryType Type { get { return EntryType.Toggle; } }

        public static implicit operator bool(InteractiveToggleSetting toggle)
        {
            return toggle.Value;
        }

        public override void UpdateFromRemote(SettingsEntry entry)
        {
            Setting.toggle.CopyFrom(entry.toggle);
            OnUpdatedRemotely?.Invoke(Setting);
        }

        public override void Reset()
        {
            var toggle = new API.Toggle()
            {
                value = defaultValues.Value,
                label = defaultValues.Label
            };
            if (!Setting.toggle.UpdateableFrom(toggle)) return;
            Setting.toggle.CopyFrom(toggle);
            OnLocalUpdate?.Invoke(Setting);
            OnUpdatedRemotely?.Invoke(Setting);
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SelectAttribute : SpecificSettingAttribute
    {
        public string[] Options { get; private set; }
        public int Value { get; private set; }

        public SelectAttribute(int value, params string[] options)
        {
            Options = options;
            Value = value;
        }
    }
    public class InteractiveSelectSetting : InteractiveSetting
    {
        public string[] Options
        {
            get { return Setting.select.options; }
            set
            {
                if (Setting.select.options.SequenceEqual(value)) return;
                Setting.select.options = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public int Value
        {
            get { return Setting.select.value; }
            set
            {
                if (Setting.select.value == value) return;
                Setting.select.value = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        private readonly SelectAttribute defaultValues;
        public InteractiveSelectSetting(SettingsEntry entry, SelectAttribute values) : base(entry)
        {
            defaultValues = values;
            entry.type = EntryType.Select;
            entry.select = new Select()
            {
                options = values.Options,
                value = values.Value
            };
        }

        public override EntryType Type { get { return EntryType.Select; } }

        public static implicit operator int(InteractiveSelectSetting select)
        {
            return select.Value;
        }

        public override void UpdateFromRemote(SettingsEntry entry)
        {
            Setting.select.CopyFrom(entry.select);
            OnUpdatedRemotely?.Invoke(Setting);
        }

        public override void Reset()
        {
            var select = new Select()
            {
                options = defaultValues.Options,
                value = defaultValues.Value
            };
            if (!Setting.select.UpdateableFrom(select)) return;
            Setting.select.CopyFrom(select);
            OnLocalUpdate?.Invoke(Setting);
            OnUpdatedRemotely?.Invoke(Setting);
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColorAttribute : SpecificSettingAttribute
    {
        public float A { get; private set; }
        public float R { get; private set; }
        public float G { get; private set; }
        public float B { get; private set; }

        public ColorAttribute(float r, float g, float b, float a = 1f)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }
    }
    public class InteractiveColorSetting : InteractiveSetting
    {
        public API.Color Color
        {
            get { return Setting.color; }
            set
            {
                if (!Setting.color.UpdateableFrom(value)) return;
                Setting.color = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        private readonly ColorAttribute defaultValues;
        public InteractiveColorSetting(SettingsEntry entry, ColorAttribute values) : base(entry)
        {
            defaultValues = values;
            entry.type = EntryType.Color;
            entry.color = new API.Color()
            {
                a = values.A,
                r = values.R,
                g = values.G,
                b = values.B
            };
        }

        public override EntryType Type { get { return EntryType.Color; } }

        public static implicit operator API.Color(InteractiveColorSetting color)
        {
            return color.Color;
        }

        public override void UpdateFromRemote(SettingsEntry entry)
        {
            Setting.color.CopyFrom(entry.color);
            OnUpdatedRemotely?.Invoke(Setting);
        }
        public override void Reset()
        {
            var color = new API.Color()
            {
                a = defaultValues.A,
                r = defaultValues.R,
                g = defaultValues.G,
                b = defaultValues.B
            };
            if (!Setting.color.UpdateableFrom(color)) return;
            Setting.color.CopyFrom(color);
            OnLocalUpdate?.Invoke(Setting);
            OnUpdatedRemotely?.Invoke(Setting);
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class HotkeyAttribute : SpecificSettingAttribute
    {
        public bool Ctrl { get; private set; }
        public bool Alt { get; private set; }
        public bool Shift { get; private set; }
        public KeyCode Key { get; private set; }

        public HotkeyAttribute(KeyCode key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Key = key;
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
        }
    }
    public class InteractiveHotkeySetting : InteractiveSetting, Copyable<Hotkey>
    {
        public KeyCode Key
        {
            get { return (KeyCode)Setting.hotkey.keyCode; }
            set
            {
                if (Setting.hotkey.keyCode == (int)value) return;
                Setting.hotkey.keyCode = (int)value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public bool Ctrl
        {
            get { return Setting.hotkey.ctrl; }
            set
            {
                if (Setting.hotkey.ctrl == value) return;
                Setting.hotkey.ctrl = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public bool Alt
        {
            get { return Setting.hotkey.alt; }
            set
            {
                if (Setting.hotkey.alt == value) return;
                Setting.hotkey.alt = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        public bool Shift
        {
            get { return Setting.hotkey.shift; }
            set
            {
                if (Setting.hotkey.shift == value) return;
                Setting.hotkey.shift = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }
        private readonly HotkeyAttribute defaultValues;
        public InteractiveHotkeySetting(SettingsEntry entry, HotkeyAttribute values) : base(entry)
        {
            defaultValues = values;
            entry.type = EntryType.Hotkey;
            entry.hotkey = new Hotkey()
            {
                keyCode = (int)values.Key,
                ctrl = values.Ctrl,
                alt = values.Alt,
                shift = values.Shift
            };
        }

        public override EntryType Type { get { return EntryType.Hotkey; } }

        public void CopyFrom(Hotkey other)
        {
            if (!Setting.hotkey.UpdateableFrom(other)) return;
            Setting.hotkey.CopyFrom(other);
        }

        public override void UpdateFromRemote(SettingsEntry entry)
        {
            Setting.hotkey.CopyFrom(entry.hotkey);
            OnUpdatedRemotely?.Invoke(Setting);
        }
        public override void Reset()
        {
            var hotkey = new Hotkey()
            {
                keyCode = (int)defaultValues.Key,
                ctrl = defaultValues.Ctrl,
                alt = defaultValues.Alt,
                shift = defaultValues.Shift
            };
            if (!Setting.hotkey.UpdateableFrom(hotkey)) return;
            Setting.hotkey.CopyFrom(hotkey);
            OnLocalUpdate?.Invoke(Setting);
            OnUpdatedRemotely?.Invoke(Setting);
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ButtonAttribute : SpecificSettingAttribute
    {
        public string Label { get; private set; }
        public ButtonAttribute(string label)
        {
            Label = label;
        }
    }
    public class InteractiveButtonSetting : InteractiveSetting
    {
        private readonly ButtonAttribute defaultValues;
        private ButtonState previousState;
        public string Label
        {
            get { return Setting.button.label; }
            set
            {
                if (Setting.button.label == value) return;
                Setting.button.label = value;
                OnLocalUpdate?.Invoke(Setting);
            }
        }

        public ButtonState State
        {
            get { return Setting.button.state; }
        }

        public UnityEvent OnMouseEntered { get; private set; }
        public UnityEvent OnMouseLeft { get; private set; }
        public UnityEvent OnButtonPressed { get; private set; }
        public UnityEvent MouseUp { get; private set; }

        public InteractiveButtonSetting(SettingsEntry entry, ButtonAttribute values) : base(entry)
        {
            defaultValues = values;
            entry.type = EntryType.Button;
            entry.button = new API.Button()
            {
                label = values.Label
            };
            OnMouseEntered = new UnityEvent();
            OnMouseLeft = new UnityEvent();
            OnButtonPressed = new UnityEvent();
        }

        public override EntryType Type { get { return EntryType.Button; } }

        public override void Reset()
        {
            previousState = ButtonState.Normal;
            var button = new API.Button()
            {
                label = defaultValues.Label
            };
            if (!Setting.button.UpdateableFrom(button)) return;
            Setting.button.CopyFrom(button);
            OnLocalUpdate?.Invoke(Setting);
            OnUpdatedRemotely?.Invoke(Setting);
        }

        public override void UpdateFromRemote(SettingsEntry entry)
        {
            bool stateChanged = previousState != Setting.button.state;
            if (stateChanged)
                previousState = Setting.button.state;
            Setting.button.CopyFrom(entry.button);
            if (stateChanged)
            {
                if (State == ButtonState.Pressed) OnButtonPressed?.Invoke();
                if (State == ButtonState.Normal) OnMouseLeft?.Invoke();
                if (State == ButtonState.Highlighted && previousState == ButtonState.Normal) OnMouseEntered?.Invoke();
            }
            OnUpdatedRemotely?.Invoke(Setting);
        }
    }
    public class InteractiveConfiguration<T> where T : class, new()
    {
        public T Settings { get; private set; }
        public ModConfig ModConfig { get; private set; }
        private List<InteractiveSetting> interactiveSettings;

        public InteractiveConfiguration()
        {
            Parse();
        }

        public void Install(ModSettingsProxy proxy, SettingsEntry[] oldSettings)
        {
            foreach (var setting in interactiveSettings)
            {
                proxy.AddSettingsChangedListener(setting.Path, (entry) =>
                {
                    setting.UpdateFromRemote(entry);
                });
                setting.OnLocalUpdate.AddListener((entry) => proxy.UpdateSetting(entry, null, null));
            }

            proxy.AddResetIssuedListener(() =>
            {
                Debugging.Log("InteractiveConfig", $"Received reset command, resetting {interactiveSettings.Count} settings...");
                foreach (var setting in interactiveSettings)
                    setting.Reset();
            });

            foreach (var setting in oldSettings)
            {
                var currentSetting = interactiveSettings.Where(s => s.Path == setting.path).FirstOrDefault();
                if (currentSetting == null)
                {
                    Debugging.Log("InteractiveConfig", $"Received outdated setting: {setting.path}");
                    continue;
                }
                currentSetting.RestoreSetting(setting);
            }
        }
        private void Parse()
        {
            var type = typeof(T);
            var obj = Activator.CreateInstance(type);
            var mod = type.GetCustomAttributes(typeof(ModAttribute), false).Cast<ModAttribute>().FirstOrDefault();
            var modSettings = new List<SettingsEntry>();
            interactiveSettings = new List<InteractiveSetting>();

            DissectObject(mod.Name, obj, modSettings);

            ModConfig = new ModConfig()
            {
                name = mod.Name,
                version = mod.Version,
                author = mod.Author,
                settings = modSettings.ToArray()
            };
            Settings = obj as T;
        }

        private void DissectObject(string categoryPath, object obj, List<SettingsEntry> modSettings)
        {
            var type = obj.GetType();

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var categories = props
                .Where(p => p.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                .Select(p => new Tuple<PropertyInfo, CategoryAttribute>(p, p.GetCustomAttributes(typeof(CategoryAttribute), false).Cast<CategoryAttribute>().FirstOrDefault()))
                .Where(t => t.Item2 != null);
            var settings = props
                .Where(p => p.PropertyType.IsSubclassOf(typeof(InteractiveSetting)))
                .Select(p => new Tuple<PropertyInfo, SettingAttribute, SpecificSettingAttribute>(
                    p,
                    p.GetCustomAttributes(typeof(SettingAttribute), false).Cast<SettingAttribute>().FirstOrDefault(),
                    p.GetCustomAttributes(typeof(SpecificSettingAttribute), false).Cast<SpecificSettingAttribute>().FirstOrDefault()))
                .Where(t => t.Item2 != null);

            //First, populate categories
            foreach (var category in categories)
            {
                var value = Activator.CreateInstance(category.Item1.PropertyType);
                category.Item1.SetValue(obj, value, null);
                DissectObject(
                    string.Join("/", new string[] { categoryPath, category.Item2.Name }),
                    value,
                    modSettings
                );
            }
            //Then, initialize & setup settings...
            foreach (var setting in settings)
            {
                var entry = new SettingsEntry()
                {
                    description = setting.Item2.Description,
                    path = string.Join("/", new string[] { categoryPath, setting.Item2.Name })
                };
                if (modSettings.Any(s => s.path == entry.path))
                {
                    Debugging.Log("InteractiveConfig", $"Failed to initialize interactive setting of \"{entry.path}\": already registered");
                    continue;
                }
                modSettings.Add(entry);
                var interactiveSetting = Activator.CreateInstance(setting.Item1.PropertyType, entry, setting.Item3) as InteractiveSetting;
                if (interactiveSetting == null)
                {
                    Debugging.Log("InteractiveConfig", $"Failed to initialize interactive setting of \"{entry.path}\": Activator returned NULL");
                    continue;
                }
                interactiveSettings.Add(interactiveSetting);
                setting.Item1.SetValue(obj, interactiveSetting, null);
            }
        }
    }
}

