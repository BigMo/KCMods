using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zat.Shared.ModMenu.Interactive;

namespace Zat.AutoTrade
{
    [Mod("AutoTrade", "1.0", "Zat")]
    public class AutoTradeSettings
    {
        [Setting("Enabled", "Whether autotrading is enabled")]
        [Toggle(true, "Enabled")]
        public InteractiveToggleSetting Enabled { get; private set; }

        [Setting("Play sound", "Play sound effect after autotrade")]
        [Toggle(true, "SFX")]
        public InteractiveToggleSetting SFX { get; private set; }

        [Setting("Send away", "Send merchant ship away after autotrade")]
        [Toggle(true, "Send away")]
        public InteractiveToggleSetting SendAway { get; private set; }
    }
}
