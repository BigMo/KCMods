using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Zat.Shared;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.Commander
{
    public class CommandEntry : MonoBehaviour
    {
        private GameObject iconSoldier, iconArcher, iconTransportShip, healthBar;
        private TextMeshProUGUI count, designationText;
        private UnityEngine.UI.Image healthColor;
        private UnityEngine.UI.Button button;
        private readonly UnityEvent onGroupEmpty = new UnityEvent();
        private int designation;
        private float nextUpdate = 1;
        private CommandGroup group;
        private float lastClick = 0;
        private int numClicks = 0;
        private static Gradient HEALTH_GRADIENT;
        private static Gradient HealthGradient
        {
            get
            {
                if (HEALTH_GRADIENT == null)
                {
                    HEALTH_GRADIENT = new Gradient();
                    var colors = new GradientColorKey[3];
                    colors[0].color = UnityEngine.Color.red;
                    colors[0].time = 0f;
                    colors[1].color = UnityEngine.Color.yellow;
                    colors[1].time = 0.5f;
                    colors[2].color = UnityEngine.Color.green;
                    colors[2].time = 1f;
                    var alphas = new GradientAlphaKey[2];
                    alphas[0].alpha = 1f;
                    alphas[0].time = 0f;
                    alphas[0].alpha = 1f;
                    alphas[0].time = 1f;
                    HEALTH_GRADIENT.SetKeys(colors, alphas);
                }
                return HEALTH_GRADIENT;
            }
        }

        private bool HasGroup { get { return group?.HasUnits ?? false; } }

        public CommandGroup Group
        {
            get { return group; }
            set
            {
                if (group != value)
                {
                    group = value;
                    UpdateUI();
                }
            }
        }
        public UnityEvent OnGroupEmpty { get { return onGroupEmpty; } }
        public InteractiveHotkeySetting Hotkey { get; set; }
        public int Designation
        {
            get { return designation; }
            set
            {
                designation = value;
                if (designationText) designationText.text = $"#{value}";
            }
        }
        public bool Visible
        {
            get { return gameObject.activeSelf; }
            set { gameObject.SetActive(value); }
        }
        public float UpdateInterval { get; set; }

        public void Init()
        {
            try
            {
                iconSoldier = transform.Find("Icons/Soldier")?.gameObject;
                iconArcher = transform.Find("Icons/Archer")?.gameObject;
                iconTransportShip = transform.Find("Icons/TroopTransportShip")?.gameObject;
                count = transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
                designationText = transform.Find("Number")?.GetComponent<TextMeshProUGUI>();
                healthBar = transform.Find("Health/Bar")?.gameObject;
                healthColor = transform.Find("Health/Bar/BarFiller")?.GetComponent<UnityEngine.UI.Image>();
                button = transform.GetComponent<UnityEngine.UI.Button>();

                if (iconSoldier == null) Debugging.Log("CommandEntry", $"{nameof(iconSoldier)} NULL");
                if (iconArcher == null) Debugging.Log("CommandEntry", $"{nameof(iconArcher)} NULL");
                if (iconTransportShip == null) Debugging.Log("CommandEntry", $"{nameof(iconTransportShip)} NULL");
                if (count == null) Debugging.Log("CommandEntry", $"{nameof(count)} NULL");
                if (designationText == null) Debugging.Log("CommandEntry", $"{nameof(designationText)} NULL");
                if (healthBar == null) Debugging.Log("CommandEntry", $"{nameof(healthBar)} NULL");
                if (healthColor == null) Debugging.Log("CommandEntry", $"{nameof(healthColor)} NULL");
                if (button == null) Debugging.Log("CommandEntry", $"{nameof(button)} NULL");

                button.onClick.AddListener(() => RegisterClick());
                designationText.text = designation.ToString();
                designationText.alignment = TextAlignmentOptions.TopLeft;
                count.alignment = TextAlignmentOptions.BottomRight;
            }
            catch (Exception ex)
            {
                Debugging.Log("CommanderUI", $"Failed to init CommandEntry: {ex.Message}");
                Debugging.Log("CommanderUI", ex.StackTrace);
            }
        }

        public void Update()
        {
            try
            {

                if (!HasGroup) onGroupEmpty?.Invoke();
                else if (Time.time > nextUpdate) UpdateUI();

                if (Time.time - lastClick >= 0.3f && numClicks > 0 && HasGroup)
                {
                    if (numClicks == 1) group.Select();
                    if (numClicks == 2) group.MoveCamera();
                    numClicks = 0;
                }
            }
            catch (Exception ex)
            {
                Debugging.Log("CommanderUI", $"Failed to process Update: {ex.Message}");
                Debugging.Log("CommanderUI", ex.StackTrace);
            }
        }

        private void UpdateUI()
        {
            if (group != null) group.CheckForInvalidArmies();
            count.text = HasGroup ? Group.Count.ToString() : "-";
            var health = HasGroup ? Group.Health : 0;
            healthBar.transform.localScale = new Vector3(health, 1, 1);
            healthColor.color = HealthGradient.Evaluate(health);

            iconSoldier.SetActive(false);
            iconArcher.SetActive(false);
            iconTransportShip.SetActive(false);

            if (HasGroup)
            {
                var icons = new List<GameObject>();

                if ((Group.Type & CommandUnit.UnitType.Archer) == CommandUnit.UnitType.Archer) icons.Add(iconArcher);
                else iconArcher.SetActive(false);
                if ((Group.Type & CommandUnit.UnitType.Soldier) == CommandUnit.UnitType.Soldier) icons.Add(iconSoldier);
                else iconSoldier.SetActive(false);
                if ((Group.Type & CommandUnit.UnitType.TroopTransportShip) == CommandUnit.UnitType.TroopTransportShip) icons.Add(iconTransportShip);
                else iconTransportShip.SetActive(false);

                foreach (var icon in icons) icon.SetActive(true);

                switch (icons.Count)
                {
                    case 3:
                        icons[0].GetComponent<RectTransform>().anchoredPosition = new Vector3(-12, -12);
                        icons[0].GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                        icons[1].GetComponent<RectTransform>().anchoredPosition =new Vector3(0, 0);
                        icons[1].GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                        icons[2].GetComponent<RectTransform>().anchoredPosition =new Vector3(12, 12);
                        icons[2].GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                        break;
                    case 2:
                        icons[0].GetComponent<RectTransform>().anchoredPosition =new Vector3(-6, -6);
                        icons[0].GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
                        icons[1].GetComponent<RectTransform>().anchoredPosition =new Vector3(6, 6);
                        icons[1].GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
                        break;
                    case 1:
                        icons[0].GetComponent<RectTransform>().anchoredPosition =new Vector3(0, 0);
                        icons[0].GetComponent<RectTransform>().sizeDelta = new Vector2(48, 48);
                        break;
                }
            }

            nextUpdate = Time.time + UpdateInterval;
        }

        public void RegisterClick()
        {
            numClicks++;
            lastClick = Time.time;
        }
    }
}
