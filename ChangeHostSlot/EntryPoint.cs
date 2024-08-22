using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChangeHostSlot
{
    [BepInPlugin("JarheadHME.ChangeHostSlot", "ChangeHostSlot", VersionInfo.Version)]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    // unwritten soft dependency on Hikaria.Core, it's not a bepinex plugin so it wouldn't work here anyway
    // and harmony patching over it works anyway, maybe it's just load order
    internal class EntryPoint : BasePlugin
    {
        private Harmony _Harmony = null;
        private static readonly string CustomPlayerSlotsPath = Path.Join(Paths.ConfigPath, "ChangeHostSlotPlayers.json");
        public override void Load()
        {
            SlotConfig.CreateBind();
            Logger.Info($"Patching on slot {SlotConfig.Slot} ({new string[] { "Pink/Woods", "Green/Dauda", "Blue/Hackett", "Purple/Bishop" }[SlotConfig.Slot]})");

            _Harmony = new Harmony($"{VersionInfo.RootNamespace}.Harmony");
            try
            {
                _Harmony.PatchAll();
            }
            catch (HarmonyException) {} // don't do anything if it fails to do the patch

            if (_Harmony.GetPatchedMethods().Count() > 0)
            {
                Logger.Info("Harmony Patch succeeded, skipping detour");
            }
            else
            {
                Logger.Info("Harmony Patch failed, applying detour");
                Patches.DetourPatch.ApplyNative();
            }
            Logger.Info("Loaded successfully");

            ParsePlayerSlotsFile();
        }

        public static void ParsePlayerSlotsFile()
        {
            if (!File.Exists(CustomPlayerSlotsPath)) CreateDefaultCustomSlotsFile();

            PlayerSlotsJSON[] slotsConfig = GTFO.API.JSON.JsonSerializer.Deserialize<PlayerSlotsJSON[]>(File.ReadAllText(CustomPlayerSlotsPath));
            foreach (var slotConfig in slotsConfig)
            {
                if (slotConfig.PlayerID != 0) // skip over the default
                    Patches.DetourPatch.PlayerSlots.Add(slotConfig.PlayerID, slotConfig.Slot);
            }
        }

        public static void CreateDefaultCustomSlotsFile()
        {
            Logger.Info("File doesn't exist, creating default");

            PlayerSlotsJSON[] defaultSlotsConfig = new PlayerSlotsJSON[1];
            defaultSlotsConfig[0] = new()
            {
                PlayerName = "PlayerNameGoesHere",
                PlayerID = 0,
                Slot = 0
            };

            string json = GTFO.API.JSON.JsonSerializer.Serialize(defaultSlotsConfig, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true });
            File.WriteAllText(CustomPlayerSlotsPath, json);
        }

    }

    internal class PlayerSlotsJSON
    {
        public string PlayerName { get; set; } // just for keeping track of which is which
        public ulong PlayerID { get; set; }
        public int Slot { get; set; }
    }
}