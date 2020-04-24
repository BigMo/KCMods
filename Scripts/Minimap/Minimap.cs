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
using Zat.Shared.UI.Utilities;

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
        private RectTransform header, mapBody, mapTexture, arrowBody, arrowImageBody;
        private Image arrowImage;
        private RawImage mapImage;
        private Button headerButton;

        private ModSettingsProxy proxy;

        private Vector2 WorldSize { get { return new Vector2(World.inst?.GetField<int>("gridWidth") ?? 0, World.inst?.GetField<int>("gridHeight") ?? 0); } }
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
        private Vector3[] VikingBoats
        {
            get
            {
                return RaiderSystem.inst?.unitData?
                    .Where(u => u != null && u.unit != null)
                    .Select(u => u.unit.GetPos())
                    .ToArray()
                    ?? new Vector3[0];
            }
        }
        private Vector3[] VikingArmies
        {
            get
            {
                return UnitSystem.inst?.GetField<List<UnitSystem.Army>>("armies")?
                    .Where(a => a.CurrHealth() > 0 && a.teamId == 1)
                    .Select(a => a.GetPos())
                    .ToArray()
                    ?? new Vector3[0];
            }
        }
        private Vector3[] Vikings
        {
            get { return VikingBoats.Concat(VikingArmies).ToArray(); }
        }
        private Vector3[] Dragons
        {
            get
            {
                return DragonSpawn.inst?.currentDragons?
                    .Where(d => d != null && d.GetState() > DragonController.DragonState.Uninitialized && d.GetState() < DragonController.DragonState.Dead)
                    .Select(d => d.transform.position)
                    .ToArray()
                    ?? new Vector3[0];
            }
        }
        private Vector3[] Armies
        {
            get
            {
                return UnitSystem.inst?.GetField<List<UnitSystem.Army>>("armies")?
                    .Where(a => a.CurrHealth() > 0 && a.teamId == 0)
                    .Select(a => a.GetPos())
                    .ToArray()
                    ?? new Vector3[0];
            }
        }

        private UnitIndicatorPool pool = new UnitIndicatorPool();

        private KeyCode ToggleKey { get { return (KeyCode?)proxy?.Config["Minimap/Key"]?.hotkey.keyCode ?? KeyCode.M; } }
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
                arrowBody = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture/Arrow")?.GetComponent<RectTransform>();
                arrowImage = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture/Arrow/Image")?.GetComponent<Image>();
                arrowImageBody = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture/Arrow/Image")?.GetComponent<RectTransform>();
                mapImage = gameObject.transform.Find("MapUI/MapBody/Margin/MapTexture")?.GetComponent<RawImage>();
                headerButton = gameObject.transform.Find("MapUI/Header/Close")?.GetComponent<Button>();
                var headerText = gameObject.transform.Find("MapUI/Header/Text")?.GetComponent<TextMeshProUGUI>();
                headerText.alignment = TextAlignmentOptions.Midline;
                var events = mapImage.gameObject.AddComponent<EventTrigger>();
                var trigger = new EventTrigger.Entry();
                trigger.eventID = EventTriggerType.PointerClick;
                trigger.callback.AddListener(OnMapClick);
                events.triggers.Add(trigger);

                pool.parent = mapTexture?.transform;

                var drag = header.gameObject.AddComponent<DraggableRect>();
                drag.movable = mapUI?.GetComponent<RectTransform>();
                drag.onMoved.AddListener(OnMoved);

                SetSize(128);
                SetPos(0, 0);
                mapImage.texture = tex;

                ModSettingsBootstrapper.Register(ModConfigBuilder
                    .Create("Minimap", "v1.3", "Zat")
                    //General
                    .AddToggle("Minimap/Enabled", "Whether or not to show the map", "Visible", true)
                    .AddHotkey("Minimap/Key", "What button to press to toggle the map on/off\nSet to [M]", (int)KeyCode.M)
                    .AddSlider("Minimap/Update Interval", "Interval between minimap updates", "Every 5.00s", 1, 30, true, 5)

                    //Visual
                    .AddSlider("Minimap/Visual/Size", "Width and height of the map in pixels", "Size: 128px", 100, 1024, true, 128)
                    .AddSlider("Minimap/Visual/Position X", "Where the map is placed horizontally", "X: 0", 0, Screen.width, true, 0)
                    .AddSlider("Minimap/Visual/Position Y", "Where the map is placed vertically", "Y: 0", 0, Screen.height, true, 0)

                    //Visual Indicators Camera
                    .AddToggle("Minimap/Visual/Indicators/Camera/Enabled", "Show/hide the camera indicator (arrow)", "Visible", true)
                    .AddColor("Minimap/Visual/Indicators/Camera/Color", "The color of the camera indicator", 0, 0, 0, 0.7f)
                    .AddSlider("Minimap/Visual/Indicators/Camera/Size", "The size of the camera indicator", "Size: 16px", 4, 64, true, 16)
                    //Visual Indicators Army
                    .AddToggle("Minimap/Visual/Indicators/Armies/Enabled", "Show/hide armies as indicators", "Visible", true)
                    .AddColor("Minimap/Visual/Indicators/Armies/Color", "The color of the army indicators", 0, 0.88f, 1f, 0.9f)
                    .AddSlider("Minimap/Visual/Indicators/Armies/Size", "The size of the army indicators", "Size: 16px", 4, 64, true, 16)
                    //Visual Indicators Vikings
                    .AddToggle("Minimap/Visual/Indicators/Vikings/Enabled", "Show/hide vikings as indicators", "Visible", true)
                    .AddColor("Minimap/Visual/Indicators/Vikings/Color", "The color of the viking indicators", 1, 0.28f, 0, 0.9f)
                    .AddSlider("Minimap/Visual/Indicators/Vikings/Size", "The size of the viking indicators", "Size: 16px", 4, 64, true, 16)
                    //Visual Indicators Dragons
                    .AddToggle("Minimap/Visual/Indicators/Dragons/Enabled", "Show/hide dragons as indicators", "Visible", true)
                    .AddColor("Minimap/Visual/Indicators/Dragons/Color", "The color of the dragon indicators", 1, 0, 0, 0.9f)
                    .AddSlider("Minimap/Visual/Indicators/Dragons/Size", "The size of the dragon indicators", "Size: 16px", 4, 64, true, 16)

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
        private void OnMoved()
        {
            if (!proxy) return;
            var posx = proxy.Config["Minimap/Visual/Position X"];
            var posy = proxy.Config["Minimap/Visual/Position Y"];
            var rect = mapUI.GetComponent<RectTransform>();
            posx.slider.value = rect.anchoredPosition.x;
            posy.slider.value = -rect.anchoredPosition.y;
            proxy.UpdateSetting(posx, null, null);
            proxy.UpdateSetting(posy, null, null);
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
            proxy.AddSettingsChangedListener("Minimap/Key", (setting) => {
                setting.description = $"What button to press to toggle the map on/off\nSet to {setting.hotkey.ToString()}";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Update Interval", (setting) => {
                updateInterval = setting.slider.value;
                setting.slider.label = $"Every {(int)setting.slider.value}s";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Enabled", EnabledChanged);
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
            proxy.AddResetIssuedListener(()=> { /* Implement Update-calls that restore default values */ });

            //Indicators Camera
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Camera/Enabled", (setting) =>
            {
                arrowBody.gameObject.SetActive(setting.toggle.value);
                setting.toggle.label = setting.toggle.value ? "Visible" : "Hidden";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Camera/Color", (setting) => {
                arrowImage.color = new Color(setting.color.r, setting.color.g, setting.color.b, setting.color.a);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Camera/Size", (setting) => {
                setting.slider.label = $"Size: {(int)setting.slider.value}px";
                arrowImageBody.sizeDelta = new Vector2(setting.slider.value, setting.slider.value);
                proxy.UpdateSetting(setting, null, null);
            });
            //Indicators Army
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Armies/Enabled", (setting) =>
            {
                setting.toggle.label = setting.toggle.value ? "Visible" : "Hidden";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Armies/Size", (setting) => {
                setting.slider.label = $"Size: {(int)setting.slider.value}px";
                proxy.UpdateSetting(setting, null, null);
            });
            //Indicators Vikings
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Vikings/Enabled", (setting) =>
            {
                setting.toggle.label = setting.toggle.value ? "Visible" : "Hidden";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Vikings/Size", (setting) => {
                setting.slider.label = $"Size: {(int)setting.slider.value}px";
                proxy.UpdateSetting(setting, null, null);
            });
            //Indicators Army
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Dragons/Enabled", (setting) =>
            {
                setting.toggle.label = setting.toggle.value ? "Visible" : "Hidden";
                proxy.UpdateSetting(setting, null, null);
            });
            proxy.AddSettingsChangedListener("Minimap/Visual/Indicators/Dragons/Size", (setting) => {
                setting.slider.label = $"Size: {(int)setting.slider.value}px";
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
            if (Input.GetKeyDown(ToggleKey))
            {
                var setting = proxy.Config["Minimap/Enabled"];
                setting.toggle.value = !setting.toggle.value;
                EnabledChanged(setting);
            }
            UpdateArrow();
            try
            {
                UpdateIndicators();
            }catch(Exception ex)
            {
                if (Input.GetKey(KeyCode.I))
                {
                    Loader.Helper.Log(ex.Message);
                    Loader.Helper.Log(ex.StackTrace);
                }
            }

            if (Input.GetKey(KeyCode.I))
            {
                Loader.Helper.Log($"Armies: {Armies.Count()}");
                Loader.Helper.Log($"Dragons: {Dragons.Count()}");
                Loader.Helper.Log($"Vikings: {VikingBoats.Count()}");
                Loader.Helper.Log($"Indicators: {pool.Indicators}");
                if (Armies.Any())
                {
                    var worldSize = WorldSize;
                    var mapSize = proxy?.Config["Minimap/Visual/Size"]?.slider.value ?? 128;
                    var pos = Armies.First();
                    var mPos = ProjectToMap(new Vector2(pos.x, pos.z), worldSize, mapSize);
                    Loader.Helper.Log($"Army #1 W: {pos.ToString()}");
                    Loader.Helper.Log($"Army #1 M: {mPos.ToString()}");
                }
            }
        }

        private void UpdateIndicators()
        {
            pool.Start();

            var worldSize = WorldSize;
            var mapSize = proxy?.Config["Minimap/Visual/Size"]?.slider.value ?? 128;

            if (proxy?.Config["Minimap/Visual/Indicators/Vikings/Enabled"].toggle.value ?? false)
            {
                var size = new Vector2(proxy.Config["Minimap/Visual/Indicators/Vikings/Size"].slider.value, proxy.Config["Minimap/Visual/Indicators/Vikings/Size"].slider.value);
                var color = proxy.Config["Minimap/Visual/Indicators/Vikings/Color"].color.ToUnityColor();
                foreach (var obj in Vikings)
                {
                    var indicator = pool.GetNextIndicator();
                    if (!indicator) continue;
                    indicator.Color = color;
                    indicator.Size = size;
                    indicator.Position = ProjectToMap(new Vector2(obj.x, obj.z), worldSize, mapSize);
                }
            }
            if (proxy?.Config["Minimap/Visual/Indicators/Dragons/Enabled"].toggle.value ?? false)
            {
                var size = new Vector2(proxy.Config["Minimap/Visual/Indicators/Dragons/Size"].slider.value, proxy.Config["Minimap/Visual/Indicators/Dragons/Size"].slider.value);
                var color = proxy.Config["Minimap/Visual/Indicators/Dragons/Color"].color.ToUnityColor();
                foreach (var obj in Dragons)
                {
                    var indicator = pool.GetNextIndicator();
                    if (!indicator) continue;
                    indicator.Color = color;
                    indicator.Size = size;
                    indicator.Position = ProjectToMap(new Vector2(obj.x, obj.z), worldSize, mapSize);
                }
            }
            if (proxy?.Config["Minimap/Visual/Indicators/Armies/Enabled"].toggle.value ?? false)
            {
                var size = new Vector2(proxy.Config["Minimap/Visual/Indicators/Dragons/Size"].slider.value, proxy.Config["Minimap/Visual/Indicators/Armies/Size"].slider.value);
                var color = proxy.Config["Minimap/Visual/Indicators/Armies/Color"].color.ToUnityColor();
                foreach (var obj in Armies)
                {
                    var indicator = pool.GetNextIndicator();
                    if (!indicator) continue;
                    indicator.Color = color;
                    indicator.Size = size;
                    indicator.Position = ProjectToMap(new Vector2(obj.x, obj.z), worldSize, mapSize);
                }
            }
            pool.End();
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
            arrowBody.anchoredPosition = newPos;
            arrowBody.rotation = Quaternion.Euler(0, 0, -CamRotation - 270);
        }

        private Vector2 ProjectToMap(Vector2 worldPos, Vector2 worldSize, float mapSize)
        {
            var perc = (worldPos / worldSize) - Vector2.one * 0.5f;
            return perc * mapSize;
        }
    }
}