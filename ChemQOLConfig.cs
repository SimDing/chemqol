internal static class ChemQOLConfig
{
    internal const string ModId = "de.simonding.chemqol";

    private static ConfigEntry<bool> pourWhileSelected;
    private static ConfigEntry<bool> pipetteDrawWhileSelected;
    private static ConfigEntry<bool> heatingPlateAutoOff;
    private static ConfigEntry<bool> chemFinisherAutoSelect;
    private static ConfigEntry<bool> closeChemFinisherSelector;
    private static ConfigEntry<bool> exponentialPurifierBudget;
    private static ConfigEntry<bool> waterTestStripPurityTag;
    private static ConfigEntry<bool> compositionSpectrometerPurityTag;
    private static ConfigEntry<bool> requirePurifierPlotScanTag;
    private static ConfigEntry<bool> debugLogging;

    internal static bool PourWhileSelected => pourWhileSelected == null || pourWhileSelected.Value;
    internal static bool PipetteDrawWhileSelected => pipetteDrawWhileSelected == null || pipetteDrawWhileSelected.Value;
    internal static bool HeatingPlateAutoOff => heatingPlateAutoOff == null || heatingPlateAutoOff.Value;
    internal static bool ChemFinisherAutoSelect => chemFinisherAutoSelect == null || chemFinisherAutoSelect.Value;
    internal static bool CloseChemFinisherSelector => closeChemFinisherSelector == null || closeChemFinisherSelector.Value;
    internal static bool ExponentialPurifierBudget => exponentialPurifierBudget != null && exponentialPurifierBudget.Value;
    internal static bool WaterTestStripPurityTag => waterTestStripPurityTag == null || waterTestStripPurityTag.Value;
    internal static bool CompositionSpectrometerPurityTag => compositionSpectrometerPurityTag == null || compositionSpectrometerPurityTag.Value;
    internal static bool RequirePurifierPlotScanTag => requirePurifierPlotScanTag != null && requirePurifierPlotScanTag.Value;
    internal static bool DebugLogging => debugLogging != null && debugLogging.Value;

    internal static void Initialize(string modId)
    {
        ModConfig config = ModHelper.GetConfig(modId);
        pourWhileSelected = config.Define("pour_while_selected", true, "Liquid Handling", () => ModHelper.GetLocalized(ModId, "option_scroll_to_pour"));
        pipetteDrawWhileSelected = config.Define("pipette_draw_while_selected", true, "Liquid Handling", () => ModHelper.GetLocalized(ModId, "option_pipette_draw"));
        waterTestStripPurityTag = config.Define("water_test_strip_purity_tag", true, "Liquid Handling", () => ModHelper.GetLocalized(ModId, "option_water_test_strip_purity_tag"));
        compositionSpectrometerPurityTag = config.Define("composition_spectrometer_purity_tag", true, "Liquid Handling", () => ModHelper.GetLocalized(ModId, "option_composition_spectrometer_purity_tag"));
        heatingPlateAutoOff = config.Define("heating_plate_auto_off", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_heating_plate_auto_off"));
        chemFinisherAutoSelect = config.Define("chem_finisher_auto_select", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_chem_finisher_auto_select"));
        closeChemFinisherSelector = config.Define("close_chem_finisher_selector", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_close_chem_finisher_selector"));
        exponentialPurifierBudget = config.Define("exponential_purifier_budget", false, "Machines", () => ModHelper.GetLocalized(ModId, "option_exponential_purifier_budget"));
        requirePurifierPlotScanTag = config.Define("require_purifier_plot_scan_tag", true, "Machines", () => ModHelper.GetLocalized(ModId, "option_require_purifier_plot_scan_tag"));
        debugLogging = config.Define("debug_logging", false, "Debug", () => ModHelper.GetLocalized(ModId, "option_debug_logging"));
    }

    internal static void LocalizeSections(string modId)
    {
        ModConfig config = ModConfig.Find(modId);
        if (config == null)
        {
            return;
        }

        var sectionProperty = typeof(ConfigEntry).GetProperty(nameof(ConfigEntry.Section));
        if (sectionProperty == null || !sectionProperty.CanWrite)
        {
            ModEntry.Logger?.Warning("Config section localization skipped: ConfigEntry.Section is not writable.");
            return;
        }

        foreach (ConfigEntry entry in config.Entries)
        {
            string localizationKey = entry.Key switch
            {
                "pour_while_selected" => "section_liquid_handling",
                "pipette_draw_while_selected" => "section_liquid_handling",
                "water_test_strip_purity_tag" => "section_liquid_handling",
                "composition_spectrometer_purity_tag" => "section_liquid_handling",
                "heating_plate_auto_off" => "section_machines",
                "chem_finisher_auto_select" => "section_machines",
                "close_chem_finisher_selector" => "section_machines",
                "exponential_purifier_budget" => "section_machines",
                "require_purifier_plot_scan_tag" => "section_machines",
                "debug_logging" => "section_debug",
                _ => null
            };

            if (localizationKey != null)
            {
                try
                {
                    sectionProperty.SetValue(entry, ModHelper.GetLocalized(modId, localizationKey));
                }
                catch (System.Exception exception)
                {
                    ModEntry.Logger?.Warning($"Config section localization failed for '{entry.Key}': {exception.Message}");
                }
            }
        }
    }
}