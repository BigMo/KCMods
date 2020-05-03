using Assets.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Zat.Productivity
{
    public class ResourceManipulation
    {
        public enum ModificationMode
        {
            Multiply = 0,
            Fixed = 1
        };
        public static string[] MODES = Enum.GetNames(typeof(ModificationMode));
        public static void SetYield(ref ResourceAmount yield, FreeResourceType type, ModificationMode mode, float num)
        {
            var count = yield.Get(FreeResourceType.Charcoal);
            switch (mode)
            {
                case ModificationMode.Fixed:
                    count = (int)Mathf.Ceil(num);
                    break;
                case ModificationMode.Multiply:
                    count = (int)Mathf.Ceil(count * num);
                    break;
            }
            yield.Set(type, count);
        }
    }
}
