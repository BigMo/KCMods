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
    internal class UnitIndicatorPool
    {
        private ArrayExt<UnitIndicator> indicators = new ArrayExt<UnitIndicator>(100);
        private int currentIndex = 0, lastIndex = 0;
        public int Indicators { get { return currentIndex; } }
        public Transform parent;

        public UnitIndicator GetNextIndicator()
        {
            UnitIndicator indicator = null;
            if (currentIndex >= indicators.Count)
            {
                indicator = UnitIndicator.Create();
                indicators.Add(indicator);
                indicator.transform.SetParent(parent, false);
                indicator.transform.position = Vector2.zero;
            }
            else
            {
                indicator = indicators.data[currentIndex];
            }

            if (indicator == null) return indicator;

            if (!indicator.gameObject.activeSelf) indicator.gameObject.SetActive(true);
            currentIndex++;

            return indicator;
        }

        public void End()
        {
            var diff = currentIndex - lastIndex;
            
            if (lastIndex > currentIndex)
            {
                for (var i = currentIndex; i < lastIndex && i < indicators.Count; i++)
                {
                    var indicator = indicators.data[i];
                    indicator.gameObject.SetActive(false);
                }
            }
            lastIndex = currentIndex;
        }

        public void Start()
        {
            currentIndex = 0;
        }
    }
}
