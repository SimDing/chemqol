internal static class ChemQOLConfig
{
    private static ConfigEntry<bool> pourWhileSelected;
    private static ConfigEntry<bool> pipetteDrawWhileSelected;
    private static ConfigEntry<bool> heatingPlateAutoOff;
    private static ConfigEntry<bool> chemFinisherAutoSelect;
    private static ConfigEntry<bool> closeChemFinisherSelector;

    internal static bool PourWhileSelected => pourWhileSelected == null || pourWhileSelected.Value;
    internal static bool PipetteDrawWhileSelected => pipetteDrawWhileSelected == null || pipetteDrawWhileSelected.Value;
    internal static bool HeatingPlateAutoOff => heatingPlateAutoOff == null || heatingPlateAutoOff.Value;
    internal static bool ChemFinisherAutoSelect => chemFinisherAutoSelect == null || chemFinisherAutoSelect.Value;
    internal static bool CloseChemFinisherSelector => closeChemFinisherSelector == null || closeChemFinisherSelector.Value;

    internal static void Initialize(string modId)
    {
        ModConfig config = ModHelper.GetConfig(modId);
        pourWhileSelected = config.Define("pour_while_selected", true, "Liquid Handling", () => "Scroll down to pour");
        pipetteDrawWhileSelected = config.Define("pipette_draw_while_selected", true, "Liquid Handling", () => "Scroll up to draw with pipette");
        heatingPlateAutoOff = config.Define("heating_plate_auto_off", true, "Machines", () => "Turn heating plate off when adding a container");
        chemFinisherAutoSelect = config.Define("chem_finisher_auto_select", true, "Machines", () => "Auto-select Chem Finisher recipe");
        closeChemFinisherSelector = config.Define("close_chem_finisher_selector", true, "Machines", () => "Close Chem Finisher recipe selector after selection");
    }
}