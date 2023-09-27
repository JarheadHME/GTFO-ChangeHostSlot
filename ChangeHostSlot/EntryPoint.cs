using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;

namespace ChangeHostSlot
{
    [BepInPlugin("HostSlot.GUID", "ChangeHostSlot", VersionInfo.Version)]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal class EntryPoint : BasePlugin
    {
        public override void Load()
        {
            SlotConfig.CreateBind();
            Logger.Info($"Patching on slot {SlotConfig.Slot} ({new string[] { "Pink/Woods", "Green/Dauda", "Blue/Hackett", "Purple/Bishop" }[SlotConfig.Slot]})");

            Patch.ApplyNative();
            Logger.Info("Loaded successfully");
        }

    }
}