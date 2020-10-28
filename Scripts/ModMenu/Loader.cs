﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zat.ModMenu.UI;
using Zat.Shared;
using Zat.Shared.AssetLoading;
using Zat.Shared.ModMenu.API;
using Zat.Shared.Reflection;

namespace Zat.ModMenu
{
    public class Loader : MonoBehaviour
    {
        public static AssetManager Assets { get; private set; }
        public static KCModHelper Helper { get; private set; }
        public static readonly string VERSION = "002";

        public void Preload(KCModHelper _helper)
        {
            Debugging.Active = true;
            Helper = Debugging.Helper = _helper;
            var harmony = HarmonyInstance.Create("ModMenu");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void SceneLoaded(KCModHelper _helper)
        {
            Debugging.Active = true;
            Helper = Debugging.Helper = _helper;
            Debugging.Log("ModMenu", "SceneLoaded: Hey there!");
            Assets = new AssetManager(_helper.modPath + "/Assets", "modmenu002");
            if (!Assets.BundleLoaded)
            {
                Debugging.Log("ModMenu", "SceneLoaded: Failed to load AssetBundle!");
                return;
            }
            if (ModMenuUI.Instance)
            {
                Debugging.Log("ModMenu", "SceneLoaded: ModUI instance already exists!");
                return;
            }
            var canvasPrefab = Assets.GetPrefab("assets/workspace/ModMenu/ModUICanvas002.prefab");
            if (!canvasPrefab)
            {
                Debugging.Log("ModMenu", "SceneLoaded: Missing canvasPrefab!");
                return;
            }
            var canvas = GameObject.Instantiate(canvasPrefab) as GameObject;
            //canvas.transform.SetParent(parent, false);
            canvas.transform.Find("ModSettingsUI").Find("Scroll View").GetComponent<ScrollRect>().scrollSensitivity = 50;
            canvas.AddComponent<ModMenuUI>();
            Debugging.Log("ModMenu", "Running!");
        }

        [HarmonyPatch(typeof(MainMenuMode))]
        [HarmonyPatch("Init")]
        internal static class MainMenuModePatch
        {
            private static bool once = true;
            static void Postfix(MainMenuMode __instance)
            {
                try
                {
                    if (!once) return;
                    var mmButtons = __instance.mainMenuUI.transform.Find("MainMenu/TopLevel/Body/ButtonContainer");
                    var mmSettingsButton = mmButtons?.transform?.Find("Settings");

                    if (mmSettingsButton == null)
                    {
                        Debugging.Log("MainMenuPatch", "Could not find Main Menu Settings button");
                        return;
                    }

                    var mmModMenuGO = GameObject.Instantiate(mmSettingsButton, mmButtons, false);
                    mmModMenuGO.name = "ModSettings";
                    var mmModMenuText = mmModMenuGO.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    mmModMenuText.text = "Mod Settings";
                    var mmModMenuButton = mmModMenuGO.GetComponent<UnityEngine.UI.Button>();
                    mmModMenuButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    mmModMenuButton.onClick.AddListener(() => { if (ModMenuUI.Instance != null) ModMenuUI.Instance.MenuVisible = true; });
                    mmModMenuGO.SetSiblingIndex(mmSettingsButton.transform.GetSiblingIndex() + 1);

                    Debugging.Log("MainMenuPatch", "Installed main menu button");

                    var pauseMenu = __instance.mainMenuUI.transform.Find("MainMenu/InGamePauseMenu");
                    var pmSettingsButton = pauseMenu?.transform?.Find("Settings");

                    if (pmSettingsButton == null)
                    {
                        Debugging.Log("MainMenuPatch", "Could not find Pause Menu Settings button");
                        return;
                    }

                    var pmModMenuGO = GameObject.Instantiate(pmSettingsButton, pauseMenu, false);
                    pmModMenuGO.name = "Mods";
                    var pmModMenuText = pmModMenuGO.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    GameObject.DestroyImmediate(pmModMenuText.gameObject.GetComponent<Localize>());

                    pmModMenuText.text = "Mods";
                    var pmModMenuButton = pmModMenuGO.GetComponent<UnityEngine.UI.Button>();
                    pmModMenuButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    pmModMenuButton.onClick.AddListener(() => { if (ModMenuUI.Instance != null) ModMenuUI.Instance.MenuVisible = true; });
                    pmModMenuGO.SetSiblingIndex(pmSettingsButton.transform.GetSiblingIndex() + 1);

                    var settingsRect = pmSettingsButton.GetComponent<RectTransform>();
                    var modMenuRect = pmModMenuGO.GetComponent<RectTransform>();

                    settingsRect.sizeDelta = new Vector2(154, 30);
                    settingsRect.localPosition = new Vector3(-32.48318f, -134.3f, 0.0005999557f);

                    modMenuRect.sizeDelta = new Vector2(68, 30);
                    modMenuRect.localPosition = new Vector3(81.4808f, -134.3f, 0.0005999557f);

                    once = false;
                    Debugging.Log("MainMenuPatch", "Installed pause menu button");
                }
                catch (Exception ex)
                {
                    Debugging.Log("MainMenuPatch", $"Failed to patch: {ex.Message}");
                    Debugging.Log("MainMenuPatch", ex.StackTrace);
                }
            }
        }

        [HarmonyPatch(typeof(CreditsUI))]
        [HarmonyPatch("OnEnable")]
        internal static class CreditsPatch
        {
            public static TextMeshProUGUI CreditsNames { get; private set; }
            static void Postfix(CreditsUI __instance)
            {
                try
                {
                    //Dump(__instance.CreditsContainer.transform, ">");
                    var mods = ModMenuUI.Instance?.Configs?.ToArray() ?? new Shared.ModMenu.API.ModConfig[0];
                    var steve = __instance.CreditsContainer.transform.Find("Credits1/Steve");
                    var modDevs = GameObject.Instantiate(steve, __instance.CreditsContainer, false);
                    var steveRect = __instance.CreditsContainer.transform.Find("Credits1/Steve").GetComponent<RectTransform>();
                    var transRect = __instance.CreditsContainer.transform.Find("Credits1/And our translators").GetComponent<RectTransform>();

                    modDevs.GetComponent<RectTransform>().localPosition = steveRect.localPosition + (transRect.localPosition - steveRect.localPosition) / 2f;

                    var title = modDevs.gameObject.GetComponent<TextMeshProUGUI>();
                    CreditsNames = modDevs.transform.Find("Credit (1)")?.GetComponent<TextMeshProUGUI>();
                    GameObject.DestroyImmediate(CreditsNames.gameObject.GetComponent<Localize>());

                    title.text = "Mod Developers";
                    CreditsNames.text = string.Join(", ", ModMenuUI.Instance?.Authors ?? new string[] { "-" });
                    Debugging.Log("CreditsUIPatch", "Installed credits");
                }
                catch (Exception ex)
                {
                    Debugging.Log("CreditsUIPatch", $"Failed to patch: {ex.Message}");
                    Debugging.Log("CreditsUIPatch", ex.StackTrace);
                }
            }

            private static void Dump(Transform p, string indent = "")
            {
                Debugging.Log("[DUMP]", $"{indent} {p.name} ({p.GetComponent<RectTransform>()?.localPosition})");
                //foreach (var comp in p.gameObject.GetComponents<Component>())
                //    Debugging.Log("[DUMP]", $"{indent} COMP {comp.GetType().Name}");

                foreach (Transform t in p)
                    Dump(t, "-" + indent);
            }
        }
    }
}