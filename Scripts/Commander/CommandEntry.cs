using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Zat.Shared;

namespace Zat.Commander
{
    public class CommandEntry : MonoBehaviour
    {
        private GameObject iconsSoldier, iconsArcher, iconsMixed, healthBar;
        private TextMeshProUGUI count, designationText;
        private UnityEngine.UI.Image healthColor;
        private UnityEngine.UI.Button button;
        private UnityEvent onGroupEmpty = new UnityEvent();
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

        private bool HasGroup { get { return group?.HasArmies ?? false; } }

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
                iconsSoldier = transform.Find("Icons/Soldier")?.gameObject;
                iconsArcher = transform.Find("Icons/Archer")?.gameObject;
                iconsMixed = transform.Find("Icons/Mixed")?.gameObject;
                count = transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
                designationText = transform.Find("Number")?.GetComponent<TextMeshProUGUI>();
                healthBar = transform.Find("Health/Bar")?.gameObject;
                healthColor = transform.Find("Health/Bar/BarFiller")?.GetComponent<UnityEngine.UI.Image>();
                button = transform.GetComponent<UnityEngine.UI.Button>();

                if (iconsSoldier == null) Debugging.Log("CommandEntry", $"{nameof(iconsSoldier)} NULL");
                if (iconsArcher == null) Debugging.Log("CommandEntry", $"{nameof(iconsArcher)} NULL");
                if (iconsMixed == null) Debugging.Log("CommandEntry", $"{nameof(iconsMixed)} NULL");
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
            catch(Exception ex)
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

            iconsSoldier.SetActive(false);
            iconsArcher.SetActive(false);
            iconsMixed.SetActive(false);

            if (HasGroup)
            {
                switch (Group.Type)
                {
                    case CommandGroup.GroupType.Soldiers:
                        iconsSoldier.SetActive(true);
                        break;
                    case CommandGroup.GroupType.Archers:
                        iconsArcher.SetActive(true);
                        break;
                    case CommandGroup.GroupType.Mixed:
                        iconsMixed.SetActive(true);
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
