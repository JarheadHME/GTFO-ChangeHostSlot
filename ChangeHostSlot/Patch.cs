using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime.Runtime;
using GTFO.API;
using SNetwork;
using Il2CppSystem;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ChangeHostSlot;

namespace ChangeHostSlot;
using SlotManager = SNet_PlayerSlotManager;
internal static class Patch
{
    // thanks Kasuromi :) absolute legend, helped a ton with getting this set up properly
    // as well as Auri, who pointed me towards NativeDetours in the first place and gave me an example to get started with

    private static INativeDetour Internal_ManageSlotDetour;
    private static d_Internal_ManageSlot orig_Internal_ManageSlot;
    internal unsafe static void ApplyNative()
    {
        Internal_ManageSlotDetour = INativeDetour.CreateAndApply<d_Internal_ManageSlot>((nint)Il2CppAPI.GetIl2CppMethod<SlotManager>(nameof(SlotManager.Internal_ManageSlot), "System.Boolean", false, new string[] { nameof(SNet_Player), nameof(SNet_Slot), typeof(SNet_Slot).FullName, nameof(SNet_SlotType), nameof(SNet_SlotHandleType), "System.Int32" }), Internal_ManageSlotPatch, out orig_Internal_ManageSlot);
    }

    private unsafe delegate bool d_Internal_ManageSlot(nint _this, nint player, nint* pSlot, nint slots, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo);

    public unsafe static bool Internal_ManageSlotPatch(nint _this_ptr, nint player_ptr, nint* pSlot, nint slots_ptr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo)
    {

        SlotManager __instance = new(_this_ptr);
        SNet_Player player = new(player_ptr);
        //SNet_Slot? slot = pSlot != null ? *pSlot != 0 ? new SNet_Slot(*pSlot) : null : null;
        Il2CppReferenceArray<SNet_Slot> slots = new(slots_ptr);

        if (player.IsMaster && player.IsLocal && handle == SNet_SlotHandleType.Assign)
        {

            int ChosenSlot = SlotConfig.Slot;

            SNet_Slot chosen_slot = slots[ChosenSlot];
            chosen_slot.player = player;
            if (type == SNet_SlotType.PlayerSlot)
                player.PlayerSlot = chosen_slot;
            else
                player.CharacterSlot = chosen_slot;

            if (!__instance.SlottedPlayers.Contains(player))
                __instance.SlottedPlayers.Add(player);

            return true;
        }

        return orig_Internal_ManageSlot(_this_ptr, player_ptr, pSlot, slots_ptr, type, handle, index, methodInfo);
    }
}

