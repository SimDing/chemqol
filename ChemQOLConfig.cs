internal static class ChemQOLConfig
{
    internal const string ModId = "chemqol";

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
        pourWhileSelected = config.Define("pour_while_selected", true, "Liquid Handling", () => ModHelper.GetLocalized(ModId, "option_scroll_to_pour"));
        pipetteDrawWhileSelected = config.Define("pipette_draw_while_selected", true, "Liquid Handling", () => ModHelper.GetLocalized(ModId, "option_pipette_draw"));
        heatingPlateAutoOff = config.Define("heating_plate_auto_off", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_heating_plate_auto_off"));
        chemFinisherAutoSelect = config.Define("chem_finisher_auto_select", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_chem_finisher_auto_select"));
        closeChemFinisherSelector = config.Define("close_chem_finisher_selector", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_close_chem_finisher_selector"));
    }

    internal static void LocalizeSections(string modId)
    {
        ModConfig config = ModConfig.Find(modId);
        if (config == null)
        {
            return;
        }

        var sectionProperty = typeof(ConfigEntry).GetProperty(nameof(ConfigEntry.Section));
        foreach (ConfigEntry entry in config.Entries)
        {
            string localizationKey = entry.Key switch
            {
                "pour_while_selected" => "section_liquid_handling",
                "pipette_draw_while_selected" => "section_liquid_handling",
                "heating_plate_auto_off" => "section_machines",
                "chem_finisher_auto_select" => "section_machines",
                "close_chem_finisher_selector" => "section_machines",
                _ => null
            };

            if (localizationKey != null)
            {
                sectionProperty.SetValue(entry, ModHelper.GetLocalized(modId, localizationKey));
            }
        }
    }
}