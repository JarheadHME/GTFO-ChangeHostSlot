using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace ChangeHostSlot
{
    public static class SlotConfig
    {

        public static int Slot;

        public static void CreateBind()
        {
            BindSlot(new ConfigFile(Path.Combine(Paths.ConfigPath, "ChangeHostSlot.cfg"), true));
            if (Slot < 0 || Slot > 3)
            {
                Logger.Error("Slot option not in valid range (0-3), defaulting to 0");
                Slot = 0;
            }
        }

        private static void BindSlot(ConfigFile config)
        {
            string section = "ChangeHostSlot";
            string key = "Slot";
            int DefaultVal = 0;
            string description = "Slot to host in. 0 = Pink/Woods, 1 = Green/Dauda, 2 = Blue/Hackett, 3 = Purple/Bishop";
            Slot = config.Bind<int>(new ConfigDefinition(section, key), DefaultVal, new ConfigDescription(description, null, Array.Empty<object>())).Value;
        }
    }
}
