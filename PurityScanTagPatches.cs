using HarmonyLib;

[HarmonyPatch(typeof(WaterTestHelper), nameof(WaterTestHelper.HandleWaterCheckTestingStrip))]
internal static class WaterTestStripPurityTagPatch
{
    private static void Postfix(GameItem targetGameItem)
    {
        if (ChemQOLConfig.WaterTestStripPurityTag)
        {
            WaterHelper.SetLastScanResult(targetGameItem);
        }
    }
}

[HarmonyPatch(typeof(ChemScannerUIManager), nameof(ChemScannerUIManager.Open))]
internal static class CompositionSpectrometerPurityTagPatch
{
    private static void Postfix(GameItem water)
    {
        if (ChemQOLConfig.CompositionSpectrometerPurityTag && water != null && water.IsTag("LIQUID_CONTAINER_TAG"))
        {
            WaterHelper.SetLastScanResult(water);
        }
    }
}