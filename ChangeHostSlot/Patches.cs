using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime.Runtime;
using GTFO.API;
using SNetwork;
using System.Reflection;
using System.Collections.Generic;
using Il2CppSystem;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ChangeHostSlot;
using HarmonyLib;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;

namespace ChangeHostSlot.Patches;
using SlotManager = SNet_PlayerSlotManager;
internal static class DetourPatch
{
    // thanks Kasuromi :) absolute legend, helped a ton with getting this set up properly
    // as well as Auri, who pointed me towards NativeDetours in the first place and gave me an example to get started with

    private static INativeDetour Internal_ManageSlotDetour;
    private static d_Internal_ManageSlot orig_Internal_ManageSlot;
    internal unsafe static void ApplyNative()
    {
        Internal_ManageSlotDetour = INativeDetour.CreateAndApply<d_Internal_ManageSlot>((nint)Il2CppAPI.GetIl2CppMethod<SlotManager>(nameof(SlotManager.Internal_ManageSlot), "System.Boolean", false, new string[] { nameof(SNet_Player), nameof(SNet_Slot), typeof(SNet_Slot).FullName, nameof(SNet_SlotType), nameof(SNet_SlotHandleType), "System.Int32" }), Internal_ManageSlotPatch, out orig_Internal_ManageSlot);
    }

    private unsafe delegate bool d_Internal_ManageSlot(System.IntPtr _this, System.IntPtr player, System.IntPtr pSlot, System.IntPtr slots, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo);

    public static Dictionary<ulong, int> PlayerSlots = new();

    public unsafe static bool Internal_ManageSlotPatch(System.IntPtr _this_ptr, System.IntPtr player_ptr, System.IntPtr pSlot, System.IntPtr slots_ptr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo)
    {
        bool runOrig = Internal_ManageSlot_InternalPatch(_this_ptr, player_ptr, pSlot, slots_ptr, type, handle, index, methodInfo);
        if (runOrig)
        {
            return orig_Internal_ManageSlot(_this_ptr, player_ptr, pSlot, slots_ptr, type, handle, index, methodInfo);
        }
        return true;

    }

    // returns true or false based on whether the original method should run
    public unsafe static bool Internal_ManageSlot_InternalPatch(System.IntPtr _this_ptr, System.IntPtr player_ptr, System.IntPtr pSlot, System.IntPtr slots_ptr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo)
    {

        SlotManager __instance = new(_this_ptr);
        SNet_Player player = new(player_ptr);
        //SNet_Slot? slot = pSlot != null ? *pSlot != 0 ? new SNet_Slot(*pSlot) : null : null;
        Il2CppReferenceArray<SNet_Slot> slots = new(slots_ptr);

        Logger.Info($"Player joining: {player.NickName} - {player.Lookup}");

        // checks for custom slot first otherwise CustomSlot may not be set
        // i'm pretty sure the IsMaster check is just a sanity thing, i'm pretty sure only host uses Assign
        if (SNet.IsMaster && handle == SNet_SlotHandleType.Assign && (PlayerSlots.TryGetValue(player.Lookup, out int CustomSlot) || player.IsLocal))
        {
            int ChosenSlot = player.IsLocal ? SlotConfig.Slot : CustomSlot;

            SNet_Slot chosen_slot = slots[ChosenSlot];
            chosen_slot.player = player;
            if (type == SNet_SlotType.PlayerSlot)
                player.PlayerSlot = chosen_slot;
            else
                player.CharacterSlot = chosen_slot;

            if (!__instance.SlottedPlayers.Contains(player))
                __instance.SlottedPlayers.Add(player);

            return false;
        }

        return true;
    }
}

[HarmonyPatch]
internal class HikariaCoreHarmonyPatch
{

    static MethodBase TargetMethod()
    {
        var EventListener = System.Type.GetType("Hikaria.Core.Features.Dev.GameEventListener, Hikaria.Core");
        if (EventListener == null) throw new System.Exception("Could not find GameEventListener");

        var DetourClass = EventListener.GetNestedType("SNet_PlayerSlotManager__Internal_ManageSlot__NativeDetour", BindingFlags.NonPublic);
        if (DetourClass == null) throw new System.Exception("Could not find DetourMethod class");

        var DetourMethod = DetourClass.GetMethod("Detour", BindingFlags.NonPublic | BindingFlags.Instance);
        if (DetourMethod == null) throw new System.Exception("Could not find DetourMethod method");
        return DetourMethod;
    }

    static unsafe bool Prefix(ref bool __result, System.IntPtr instancePtr, System.IntPtr playerPtr, System.IntPtr slotPtr, System.IntPtr slotsPtr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo)
    {
        // Pretty much need to skip hikaria's method if changing the slot but it's either that or nothing so
        __result = true; // result gets set to true because if i skip the original, it would be false which means the player failed to be added (or smth like that lol)
        return DetourPatch.Internal_ManageSlot_InternalPatch(instancePtr, playerPtr, slotPtr, slotsPtr, type, handle, index, methodInfo);
    }
}