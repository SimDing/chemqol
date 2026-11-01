using HarmonyLib;

[HarmonyPatch(typeof(GameSlotInventory), nameof(GameSlotInventory.UncheckedAccept))]
internal static class HeatingPlateAutoOffPatch
{
    private static void Postfix(GameSlotInventory __instance, GameItem item, bool __result)
    {
        if (!ChemQOLConfig.HeatingPlateAutoOff || !__result || item == null || !item.IsTag("LIQUID_CONTAINER_TAG"))
        {
            return;
        }

        GameItem machine = __instance.GetParentItem();
        if (machine == null || machine.identifier != "heating_plate")
        {
            return;
        }

        int state = machine.GetTagReadonly(MachineHeatingPlate.HEATING_PLATE_STATE).GetInt();
        if (state == 0)
        {
            return;
        }

        machine.ModifyTag(MachineHeatingPlate.HEATING_PLATE_STATE, tag =>
        {
            tag.Enable().SetInt(0);
        });
        ModEntry.DebugLog("Heating plate switched off after a liquid container was added.");
    }
}
