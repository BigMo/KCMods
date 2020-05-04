using UnityEngine;

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
