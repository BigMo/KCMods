using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zat.Shared.ModMenu.API;
using Zat.Shared.Reflection;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Zat.Shared.UI.Utilities;
using Zat.Shared.ModMenu.Interactive;
using Zat.Shared.InterModComm;
using Zat.Shared;

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

        private GameObject mapUI;
        private RectTransform header, mapBody, mapTexture, arrowBody, arrowImageBody;
        private Image arrowImage;
        private RawImage mapImage;
        private UnityEngine.UI.Button headerButton;

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

        private MinimapSettings settings;
        private bool showFullscreen = false;

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
                headerButton = gameObject.transform.Find("MapUI/Header/Close")?.GetComponent<UnityEngine.UI.Button>();
                var headerText = gameObject.transform.Find("MapUI/Header/Text")?.GetComponent<TextMeshProUGUI>();
                headerText.alignment = TextAlignmentOptions.Midline;
                var events = mapImage.gameObject.AddComponent<EventTrigger>();
                var click = new EventTrigger.Entry();
                click.eventID = EventTriggerType.PointerClick;
                click.callback.AddListener(OnMapClick);
                var scroll = new EventTrigger.Entry();
                scroll.eventID = EventTriggerType.Scroll;
                scroll.callback.AddListener(OnMapScroll);
                events.triggers.Add(click);
                events.triggers.Add(scroll);

                pool.parent = mapTexture?.transform;

                var drag = header.gameObject.AddComponent<DraggableRect>();
                drag.movable = mapUI?.GetComponent<RectTransform>();
                drag.onMoved.AddListener(OnMoved);

                SetSize(128);
                SetPos(0, 0);
                mapImage.texture = tex;


                Debugging.Active = true;
                Debugging.Helper = Loader.Helper;
                var config = new InteractiveConfiguration<MinimapSettings>();
                settings = config.Settings;
                ModSettingsBootstrapper.Register(config.ModConfig, (proxy, saved) =>
                {
                    config.Install(proxy, saved);
                    OnModRegistered(proxy, saved);
                }, (ex) =>
                {
                    Loader.Helper.Log($"Failed to register mod: {ex.Message}");
                    Loader.Helper.Log(ex.StackTrace);
                });
            }
            catch (Exception ex)
            {
                Loader.Helper.Log(ex.Message);
                Loader.Helper.Log(ex.StackTrace);
            }
        }

        private void OnMapScroll(BaseEventData arg0)
        {
            var scrollData = (PointerEventData)arg0;
            if (settings == null) return;
            settings.Visual.Size.Value += scrollData.scrollDelta.y * 4f;
        }

        private void OnMapClick(BaseEventData arg0)
        {
            var pointerData = (PointerEventData)arg0;
            var point = new Vector2();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapImage.gameObject.GetComponent<RectTransform>(), pointerData.position, pointerData.pressEventCamera, out point))
            {
                var perc = (point / settings.Visual.Size.Value) + Vector2.one * 0.5f;
                var target = perc * WorldSize;
                Cam.inst?.SetDesiredTrackingPos(new Vector3(target.x, 0, target.y));
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
            var rect = mapUI.GetComponent<RectTransform>();
            settings.Visual.PositionX.Value = rect.anchoredPosition.x;
            settings.Visual.PositionY.Value = -rect.anchoredPosition.y;
        }


        private void OnModRegistered(ModSettingsProxy proxy, SettingsEntry[] saved)
        {
            try
            {
                this.proxy = proxy;
                if (!proxy)
                {
                    Loader.Helper.Log("Failed to register proxy!");
                    return;
                }

                settings.Enabled.OnUpdate.AddListener((setting) =>
                {
                    mapUI.SetActive(settings.Enabled.Value);
                    settings.Enabled.Label = mapUI.activeSelf ? "Visible" : "Hidden";
                });
                settings.UpdateInterval.OnUpdate.AddListener((setting) => settings.UpdateInterval.Label = $"Every {(int)setting.slider.value}s");
                //Visuals
                settings.Visual.Size.OnUpdate.AddListener((setting) =>
                {
                    SetSize(setting.slider.value);
                    settings.Visual.Size.Label = $"Size: {(int)setting.slider.value}px";
                });
                settings.Visual.PositionX.OnUpdate.AddListener((setting) =>
                {
                    SetPos(setting.slider.value, settings.Visual.PositionY.Value);
                    settings.Visual.PositionX.Label = $"X: {(int)setting.slider.value}";
                });
                settings.Visual.PositionY.OnUpdate.AddListener((setting) =>
                {
                    SetPos(settings.Visual.PositionX.Value, setting.slider.value);
                    settings.Visual.PositionY.Label = $"Y: {(int)setting.slider.value}";
                });
                //Camera
                settings.Visual.Indicators.Camera.Enabled.OnUpdate.AddListener((setting) =>
                {
                    arrowBody.gameObject.SetActive(setting.toggle.value);
                });
                settings.Visual.Indicators.Camera.Color.OnUpdate.AddListener((setting) => arrowImage.color = setting.color.ToUnityColor());
                settings.Visual.Indicators.Camera.Size.OnUpdate.AddListener((setting) =>
                {
                    arrowImageBody.sizeDelta = new Vector2(setting.slider.value, setting.slider.value);
                });

                //Indicators
                SetupResponsiveIndicatorEntry(settings.Visual.Indicators.Camera);
                SetupResponsiveIndicatorEntry(settings.Visual.Indicators.Armies);
                SetupResponsiveIndicatorEntry(settings.Visual.Indicators.Dragons);
                SetupResponsiveIndicatorEntry(settings.Visual.Indicators.Vikings);

                SetSize(settings.Visual.Size.Value);
                SetPos(
                    settings.Visual.PositionX.Value,
                    settings.Visual.PositionY.Value
                );

                Loader.Helper.Log("OnRegisterMod finished");
            }
            catch(Exception ex)
            {
                Loader.Helper.Log($"OnRegisterMod failed: {ex.Message}");
                Loader.Helper.Log(ex.StackTrace);
            }
        }

        private void SetupResponsiveIndicatorEntry(IndicatorEntry entry)
        {
            entry.Enabled.OnUpdate.AddListener((setting) => entry.Enabled.Label = setting.toggle.value ? "Visible" : "Hidden");
            entry.Size.OnUpdate.AddListener((setting) => entry.Size.Label = $"Size: {(int)setting.slider.value}px" );

            entry.Enabled.TriggerUpdate();
            entry.Color.TriggerUpdate();
            entry.Size.TriggerUpdate();
        }

        private void Update()
        {
            if (Time.time > nextUpdate)
                UpdateMap();
            if (settings == null) return;
            if (Input.GetKeyDown(settings.MapKey.Key))
                settings.Enabled.Value = !settings.Enabled.Value;
            if (Input.GetKeyDown(settings.FullscreenKey.Key))
                showFullscreen = !showFullscreen;
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
        }

        private void OnGUI()
        {
            if (!showFullscreen || !tex) return;
            var size = Mathf.Min(Screen.width, Screen.height);
            var pos = new Vector2(Screen.width / 2 - size / 2, Screen.height / 2 - size / 2);
            GUI.DrawTexture(new Rect(pos.x, pos.y, size, size), tex);
        }

        private void UpdateIndicators()
        {
            pool.Start();
            var worldSize = WorldSize;
            DrawIndicators(settings.Visual.Indicators.Vikings, worldSize, Vikings);
            DrawIndicators(settings.Visual.Indicators.Dragons, worldSize, Dragons);
            DrawIndicators(settings.Visual.Indicators.Armies, worldSize, Armies);
            pool.End();
        }

        private void DrawIndicators(IndicatorEntry entry, Vector2 worldSize, IEnumerable<Vector3> positions)
        {
            if (!entry.Enabled) return;

            var size = new Vector2(entry.Size, entry.Size);
            var color = entry.Color.Color.ToUnityColor();
            foreach (var obj in positions)
            {
                var indicator = pool.GetNextIndicator();
                if (!indicator) continue;
                indicator.Color = color;
                indicator.Size = size;
                indicator.Position = ProjectToMap(new Vector2(obj.x, obj.z), worldSize, settings.Visual.Size.Value);
            }
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
            nextUpdate = Time.time + (settings?.UpdateInterval?.Value ?? 5);
        }

        private void UpdateArrow()
        {
            if (settings == null) return;
            var newPos = (Vector2.one * settings.Visual.Size) * (Scroll * new Vector2(1,-1));
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