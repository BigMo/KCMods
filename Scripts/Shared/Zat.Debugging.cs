using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zat.Shared
{
    public static class Debugging
    {
        public static bool Active { get; set; }
        public static KCModHelper Helper { get; set; }

        public static void Log(string category, string content)
        {
            if (Helper == null || !Active) return;
            Helper.Log($"[{category}] {content}");
        }
    }
}
