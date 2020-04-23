using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Zat.Shared.ModMenu.API;
using Color = UnityEngine.Color;
using Zat.Shared.Rendering;
using Zat.Shared.Reflection;
using UnityEngine.UI;
using TMPro;
using Button = UnityEngine.UI.Button;
using UnityEngine.EventSystems;

namespace Zat.Minimap
{
    class Minimap : MonoBehaviour
    {
        private Vector2 mapPosition, mapSize;
        private float nextUpdate;
        
        private RenderTexture tex;
        private Camera renderCam;
        private static GameObject go;

        public static bool Instantiated { get { return go != null; } }
        
        private float zoomFactor = 1f;

        private float updateInterval = 5f;
        private bool fixedMap = false;
        private bool dynamicZoom = false;

        private GameObject mapUI;
        private RectTransform header, mapBody, mapTexture, arrow;
        private RawImage mapImage;
        private Button headerButton;

        private ModSettingsProxy proxy;

        private Vector2 WorldSize { get { return new Vector2(World.inst.GetField<int>("gridWidth"), World.inst.GetField<int>("gridHeight")); } }
        private Vector2 CamPosition { get { return new Vector2(Cam.inst?.TrackingPos.x ?? 0, Cam.inst?.TrackingPos.z ?? 0); } }
        private float CameraZoom
        {
            get
            {
                if (!Cam.inst) return 0;
                return 1f - (Cam.inst.dist - Cam.inst.zoomRange.Min) / (Cam.inst.zoomRange.Max - Cam.inst.zoomRange.Min);
            }
        }
        private Vector2 Scroll {
            get
            {
                var mapSize = WorldSize;
                var camPos = CamPosition;
                return new Vector2((camPos.x / mapSize.x) - 0.5f, (1f - (camPos.y / mapSize.y)) - 0.5f);
            }
        }
        private float CamRotation { get { return Cam.inst?.GetField<float>("Theta") ?? 0; } }
        public void Start()
        {
            try
            {
                if (go) return;
                go = new GameObject("MinimapCamera");
                go.transform.rotation = Quaternion.LookRotation(Vector3.down);
                renderCam = go.AddComponent<Camera>();
                renderCam.orthographic = true;
                renderCam.orthographicSize = 10;
                renderCam.clearFlags = CameraClearFlags.SolidColor;
                renderCam.backgroundColor = new UnityEngine.Color(0f, 0f, 0f, 0f);
                tex = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
                tex.Create();
                renderCam.targetTexture = tex;
                renderCam.enabled = false;

                mapUI = gameObject.transform.Find("MapUI")?.gameObject;
                header = gameObject.transform.Find("MapUI/Header")?.GetComponent<RectTransform>();
                mapBody = gameObject.transform.Find("MapUI/MapBody")?.GetComponent<RectTransform>();
                mapTexture = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture")?.GetComponent<RectTransform>();
                arrow = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture/Arrow")?.GetComponent<RectTransform>();
                mapImage = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture")?.GetComponent<RawImage>();
                headerButton = gameObject.transform.Find("MapUI/Header/Close")?.GetComponent<Button>();
                var headerText = gameObject.transform.Find("MapUI/Header/Text")?.GetComponent<TextMeshProUGUI>();
                headerText.alignment = TextAlignmentOptions.Midline;
                var events = mapImage.gameObject.AddComponent<EventTrigger>();
                var trigger = new EventTrigger.Entry();
                trigger.eventID = EventTriggerType.PointerClick;
                trigger.callback.AddListener(OnMapClick);
                events.triggers.Add(trigger);
                
                SetSize(128);
                SetPos(0, 0);
                mapImage.texture = tex;

                ModSettingsBootstrapper.Register(ModConfigBuilder
                    .Create("Minimap", "v1.3", "Zat")
                    .AddToggle("Minimap/Enabled", "Whether or not to show the map\n[Hotkey: M]", "Visible", true)
                    .AddSlider("Minimap/Update Interval", "Interval between minimap updates", "Every 5.00s", 1, 30, true, 5)
                    .AddToggle("Minimap/Visual/Indicator", "Show/hide the rotation indicator (arrow)", "Visible", true)
                    .AddSlider("Minimap/Visual/Size", "Width and height of the map in pixels", "Size: 128px", 100, 1024, true, 128)
                    .AddSlider("Minimap/Visual/Position X", "Where the map is placed horizontally", "X: 0", 0, Screen.width, true, 0)
                    .AddSlider("Minimap/Visual/Position Y", "Where the map is placed vertically", "Y: 0", 0, Screen.height, true, 0)
                    .Build(),
                    OnModRegistered, (ex) => { });
            }
            catch (Exception ex)
            {
                Loader.Helper.Log(ex.Message);
                Loader.Helper.Log(ex.StackTrace);
            }
        }

        private void OnMapClick(BaseEventData arg0)
        {
            var pointerData = (PointerEventData)arg0;
            var point = new Vector2();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapImage.gameObject.GetComponent<RectTransform>(), pointerData.position, pointerData.pressEventCamera, out point))
            {
                var perc = (point / (proxy?.Config["Minimap/Visual/Size"]?.slider?.value ?? 1)) + Vector2.one * 0.5f;
                var target = perc * WorldSize;
                //Cam.inst?.BringIntoView(new Vector3(target.x, 0, target.y), new ArrayExt<Vector3>(1));
                Cam.inst?.SetDesiredTrackingPos(new Vector3(target.x, 0, target.y)); //<- Doesn't work but instead warps the camera around the map
            }
        }
        
        private void SetSize(float size)
        {
            header.sizeDelta = new Vector2(size + 10, header.sizeDelta.y);
            mapBody.sizeDelta = new Vector2(size + 10, size + 10);
            UpdateArrow();
        }
        private void SetPos(float x, float y)
        {
            var rect = mapUI.GetComponent<RectTransform>();
            if (!rect) return;
            rect.anchoredPosition = new Vector2(x, -y);
        }


        private void OnModRegistered(ModSettingsProxy proxy, SettingsEntry[] saved)
        {
            this.proxy = proxy;
            if (!proxy)
            {
                Loader.Helper.Log("Failed to register proxy!");
                return;
            }

            proxy.AddSettingsChangedListener("Minimap/Visual/Size", (setting) => {
                SetSize(setting.slider.value);
                setting.slider.label = $"Size: {(int)setting.slider.value}px";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Update Interval", (setting) => {
                updateInterval = setting.slider.value;
                setting.slider.label = $"Every {(int)setting.slider.value}s";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Enabled", EnabledChanged);
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicator", (setting) =>
            {
                arrow.gameObject.SetActive(setting.toggle.value);
                setting.toggle.label = setting.toggle.value ? "Visible" : "Hidden";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Position X", (setting) =>
            {
                SetPos(setting.slider.value, proxy.Config["Minimap/Visual/Position Y"].slider.value);
                setting.slider.label = $"X: {setting.slider.value.ToString("0.00")}";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Position Y", (setting) =>
            {
                SetPos(proxy.Config["Minimap/Visual/Position X"].slider.value, setting.slider.value);
                setting.slider.label = $"Y: {setting.slider.value.ToString("0.00")}";
                proxy.UpdateSetting(setting, null, null);
            });

            //Apply saved values
            foreach (var setting in saved)
            {
                var own = proxy.Config[setting.path];
                if (own != null)
                {
                    own.CopyFrom(setting);
                    proxy.UpdateSetting(own, null, null);
                }
            }
            SetSize(proxy.Config["Minimap/Visual/Size"].slider.value);
            SetPos(
                proxy.Config["Minimap/Visual/Position X"].slider.value,
                proxy.Config["Minimap/Visual/Position Y"].slider.value
            );
        }

        private void EnabledChanged(SettingsEntry setting)
        {
            mapUI.SetActive(setting.toggle.value);
            setting.toggle.label = mapUI.activeSelf ? "Visible" : "Hidden";
            proxy.UpdateSetting(setting, null, null);
        }

        private void Update()
        {
            if (Time.time > nextUpdate)
                UpdateMap();
            if (Input.GetKeyDown(KeyCode.M))
            {
                var setting = proxy.Config["Minimap/Enabled"];
                setting.toggle.value = !setting.toggle.value;
                EnabledChanged(setting);
            }
            UpdateArrow();
        }

        public static Vector2 RotateVec(Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        private void UpdateMap()
        {
            var mapSize = WorldSize;
            if (renderCam)
            {
                renderCam.orthographicSize = mapSize.x / 2f;
                renderCam.gameObject.transform.position = new Vector3(mapSize.x / 2f, 5, mapSize.y / 2f);
                renderCam.enabled = true;
                renderCam.Render();
                renderCam.enabled = false;
            }
            nextUpdate = Time.time + updateInterval;
        }

        private void UpdateArrow()
        {
            var newPos = (Vector2.one * (proxy?.Config["Minimap/Visual/Size"]?.slider.value ?? 128)) * (Scroll * new Vector2(1,-1));
            arrow.anchoredPosition = newPos;
            arrow.rotation = Quaternion.Euler(0, 0, -CamRotation - 270);
        }
    }
}