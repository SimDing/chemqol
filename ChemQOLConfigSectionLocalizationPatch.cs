using HarmonyLib;

[HarmonyPatch(typeof(ModConfigWindow), nameof(ModConfigWindow.Open))]
internal static class ChemQOLConfigSectionLocalizationPatch
{
    private static void Prefix(LoadedMod mod)
    {
        if (mod != null && mod.Manifest.ID == ChemQOLConfig.ModId)
        {
            ChemQOLConfig.LocalizeSections(mod.Manifest.ID);
        }
    }
}