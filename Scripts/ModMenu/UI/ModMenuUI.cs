using System.Collections;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using Zat.Shared.InterModComm;
using Zat.Shared.ModMenu.API;
using Zat.ModMenu.UI.Handlers;
using Zat.ModMenu.UI.Entries;
using Newtonsoft.Json;
using Zat.Shared.UI.Utilities;
using Zat.Shared;
using Zat.Shared.ModMenu.Interactive;
using System.Collections.Generic;

namespace Zat.ModMenu.UI
{
    public class ModMenuUI : MonoBehaviour
    {
        public static ModMenuUI Instance { get; private set; }
        private TextMeshProUGUI header;
        private UnityEngine.UI.Button collapseExpand, reset, save, close;
        private TextMeshProUGUI saveText, resetText, noModsText, collapseExpandText;
        private GameObject content, noMods;
        private IMCPort port;
        private GameObject ui;
        private SettingsManager settingsManager;
        private SettingsEntry[] savedSettings;
        private bool isSaving = false;
        private bool collapseExpandState = false;
        private ModMenuSettings menuSettings;

        public string Header
        {
            get { return header?.text; }
            set { if (header) header.text = name; }
        }
        public UnityEngine.UI.Button.ButtonClickedEvent OnCollapseExpandClick { get { return collapseExpand?.onClick; } }
        public UnityEngine.UI.Button.ButtonClickedEvent OnResetClick { get { return reset?.onClick; } }
        public UnityEngine.UI.Button.ButtonClickedEvent OnSaveClick { get { return save?.onClick; } }
        public bool MenuVisible
        {
            get { return ui?.activeSelf ?? false; }
            set { if (ui) ui.SetActive(value); }
        }
        public IEnumerable<ModConfig> Configs { get { return settingsManager.Mods; } }
        public string[] Authors { get { return Configs.Select(m => m.author).Distinct().ToArray(); } }

        public void Start()
        {
            Debugging.Log("ModMenuUI", "Starting...");
            try
            {
                Debugging.Active = true;
                Debugging.Helper = Loader.Helper;
                Debugging.Log("ModMenuUI", "Acquiring ModMenu canvas objects...");
                transform.name = ModSettingsNames.Objects.ModMenuName;
                header = transform.Find("ModSettingsUI/Header/Text")?.GetComponent<TextMeshProUGUI>();
                collapseExpand = transform.Find("ModSettingsUI/CollapseExpand")?.GetComponent<UnityEngine.UI.Button>();
                collapseExpandText = transform.Find("ModSettingsUI/CollapseExpand/Text")?.GetComponent<TextMeshProUGUI>();
                reset = transform.Find("ModSettingsUI/Reset")?.GetComponent<UnityEngine.UI.Button>();
                resetText = transform.Find("ModSettingsUI/Reset/Text")?.GetComponent<TextMeshProUGUI>();
                save = transform.Find("ModSettingsUI/Save")?.GetComponent<UnityEngine.UI.Button>();
                saveText = transform.Find("ModSettingsUI/Save/Text")?.GetComponent<TextMeshProUGUI>();
                close = transform.Find("ModSettingsUI/Close")?.GetComponent<UnityEngine.UI.Button>();
                content = transform.Find("ModSettingsUI/Scroll View/Viewport/Content")?.gameObject;
                noMods = transform.Find("ModSettingsUI/NoMods")?.gameObject;
                noModsText = transform.Find("ModSettingsUI/NoMods/Text")?.GetComponent<TextMeshProUGUI>();
                port = gameObject.AddComponent<IMCPort>();
                settingsManager = new SettingsManager(content, OnUIUpdate);

                ui = transform.Find("ModSettingsUI")?.gameObject;
                Instance = this;

                var drag = header.gameObject.AddComponent<DraggableRect>();
                drag.movable = ui?.GetComponent<RectTransform>();

                Debugging.Log("ModMenuUI", "Adding UI listeners...");
                close.onClick.AddListener(() => ui.SetActive(false));
                reset.onClick.AddListener(() =>
                {
                    foreach (var mod in settingsManager.ModGameObjects)
                        port.RPC(mod, ModSettingsNames.Events.ResetIssued, 5f, null, null);
                });
                save.onClick.AddListener(() => { if (!isSaving) StartCoroutine(SaveSettingsAnim()); });
                collapseExpand.onClick.AddListener(() => ToggleCollapseExpand());

                if (gameObject.name != ModSettingsNames.Objects.ModMenuName)
                    Debugging.Log("ModMenuUI", $"{nameof(ModMenuUI)} is attached to \"{gameObject.name}\" instead of \"{ModSettingsNames.Objects.ModMenuName}\"!");

                Debugging.Log("ModMenuUI", "Fixing text alignments...");
                header.alignment = TextAlignmentOptions.Midline;
                saveText.alignment = TextAlignmentOptions.Midline;
                resetText.alignment = TextAlignmentOptions.Midline;
                collapseExpandText.alignment = TextAlignmentOptions.Midline;
                noModsText.alignment = TextAlignmentOptions.Midline;
                noMods.SetActive(true);

                Debugging.Log("ModMenuUI", "Registering ReceiveListeners...");
                port.RegisterReceiveListener<SettingsEntry>(ModSettingsNames.Methods.UpdateSetting, UpdateSettingHandler);
                port.RegisterReceiveListener<ModConfig>(ModSettingsNames.Methods.RegisterMod, RegisterModHandler);

                Debugging.Log("ModMenuUI", "Loading settings....");
                savedSettings = LoadSettings();
                ui.SetActive(false);
                SetCollapseExpand(false);

                Debugging.Log("ModMenuUI", $"Started: [{transform.parent?.name ?? "-"}] -> [{transform.name}] -> [{nameof(ModMenuUI)}]");

                var config = new InteractiveConfiguration<ModMenuSettings>();
                menuSettings = config.Settings;
                Debugging.Log("ModMenuUI", "Registering own meta-mod...");
                ModSettingsBootstrapper.Register(config.ModConfig,
                    (proxy, saved) => config.Install(proxy, saved),
                    (ex) =>
                    {
                        Debugging.Log("ModMenuUI", $"Failed to register meta-mod: {ex.Message}");
                        Debugging.Log("ModMenuUI", ex.StackTrace);
                    });
            }
            catch (Exception ex)
            {
                Debugging.Log("ModMenuUI", $"Failed to Start {nameof(ModMenuUI)}: {ex.Message}");
                Debugging.Log("ModMenuUI", ex.StackTrace);
            }
        }

        private void ToggleCollapseExpand()
        {
            SetCollapseExpand(!collapseExpandState);
        }
        private void SetCollapseExpand(bool value)
        {
            collapseExpandState = value;
            if (collapseExpandState)
                collapseExpandText.text = "Collapse all";
            else
                collapseExpandText.text = "Expand all";

            var categories = content.GetComponentsInChildren<CategoryEntry>(true);
            foreach (var cat in categories)
                cat.Expanded = collapseExpandState;
        }
        private void Update()
        {
            if (Input.GetKeyDown(menuSettings?.ToggleKey?.Key ?? KeyCode.O))
                if (ui != null) ui.SetActive(!ui.activeSelf);
        }

        private SettingsEntry[] LoadSettings()
        {
            if (PlayerPrefs.HasKey("ModMenu"))
                return JsonConvert.DeserializeObject<SettingsEntry[]>(PlayerPrefs.GetString("ModMenu"));
            return new SettingsEntry[0];
        }
        private bool SaveSettings()
        {
            isSaving = true;
            try
            {
                PlayerPrefs.SetString("ModMenu", JsonConvert.SerializeObject(settingsManager.Settings.ToArray()));
                PlayerPrefs.Save();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                isSaving = false;
            }
        }
        private IEnumerator SaveSettingsAnim()
        {
            if (SaveSettings())
            {
                saveText.text = "Saved!";
                yield return new WaitForSeconds(3f);
                saveText.text = "Save";
            }
            else
            {
                saveText.text = "Error!";
            }
        }

        #region API Implementation
        private void RegisterModHandler(IRequestHandler handler, string source, ModConfig mod)
        {
            try
            {
                settingsManager.RegisterMod(source, mod);
                var _saved = savedSettings != null ? savedSettings.Where(s => mod.settings.Any(m => m.path == s.path)).ToArray() : new SettingsEntry[0];
                handler.SendResponse(port.gameObject.name, _saved);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
                if (noMods != null) noMods.SetActive(!settingsManager.Mods.Any());
                SetCollapseExpand(false);
                if (Loader.CreditsPatch.CreditsNames != null)
                    Loader.CreditsPatch.CreditsNames.text = string.Join(", ", Authors);
            }
            catch (Exception ex)
            {
                Debugging.Log("ModMenu", $"Failed to register mod: {ex}");
                Debugging.Log("ModMenu", ex.StackTrace);
                handler.SendError(port.gameObject.name, ex);
            }
        }

        private void OnUIUpdate(SettingsEntry setting)
        {
            try
            {
                var associatedMods = settingsManager.GetAssociatedMods(setting).ToArray();
                if (associatedMods.Length == 0)
                {
                    Debugging.Log("ModMenu", $"Detected UI update for \"{setting.path}\" but it has no associated mods");
                    throw new Exception($"Detected UI update for \"{setting.path}\" but it has no associated mods");
                }
                foreach (var mod in associatedMods)
                    port.RPC(mod.GameObject, ModSettingsNames.Events.SettingChanged, setting, 5f, null, null);
            }
            catch (Exception ex)
            {
                var updateEx = new UpdateFailedException(ex.Message);
                Debugging.Log("ModMenu", ex.Message);
                Debugging.Log("ModMenu", ex.StackTrace);
                throw updateEx;
            }
        }

        private void UpdateSettingHandler(IRequestHandler handler, string source, SettingsEntry entry)
        {
            try
            {
                var context = settingsManager.GetSettingByPath(entry.path);
                if (context == null)
                {
                    Debugging.Log("ModMenu", $"Attempted to update unregistered entry \"{entry.path}\"!");
                    handler.SendError(port.gameObject.name, $"Entry \"{entry.path}\" not registered!");
                    return;
                }
                if (context.Setting.UpdateableFrom(entry))
                {
                    context.UpdateSetting(entry);
                    EntryHandler.Instance.UpdateEntry(context.Setting, context.UIElement);
                }
                else
                {
                    //Debugging.Log("ModMenu", $"Received update for {entry.path} that provides no new information");
                }
                handler.SendResponse(port.gameObject.name);
            }
            catch (Exception ex)
            {
                Debugging.Log("ModMenu", $"Failed to process update: {ex.Message}");
                Debugging.Log("ModMenu", ex.StackTrace);
                handler.SendError(port.gameObject.name, ex);
            }
        }

        public class UpdateFailedException : Exception
        {
            public UpdateFailedException(string reason) : base($"Failed to send UI update: {reason}") { }
        }
        #endregion
    }
}
