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

[HarmonyPatch(typeof(WaterStripUIManager), nameof(WaterStripUIManager.OnMatch))]
internal static class PreserveWaterScanTagOnStripLabelMatchPatch
{
    private static void Prefix(WaterStripUIManager __instance, out string __state)
    {
        __state = Capture(__instance.currentWaterContainer);
    }

    private static void Postfix(WaterStripUIManager __instance, string __state)
    {
        Restore(__instance.currentWaterContainer, __state);
    }

    internal static string Capture(GameItem waterContainer)
    {
        if (!ChemQOLConfig.WaterTestStripPurityTag)
        {
            return null;
        }

        TagState scanTag = waterContainer?.GetTagReadonly("LAST_SCAN_RESULT_TAG");
        return scanTag != null && scanTag.IsEnabled() ? scanTag.GetString() : null;
    }

    internal static void Restore(GameItem waterContainer, string scanResult)
    {
        if (!ChemQOLConfig.WaterTestStripPurityTag || scanResult == null || waterContainer == null)
        {
            return;
        }

        waterContainer.ModifyTag("LAST_SCAN_RESULT_TAG", state =>
        {
            state.Enable().SetString(scanResult);
        }, skipValidate: true);
        waterContainer.SyncModifiedState();
    }
}