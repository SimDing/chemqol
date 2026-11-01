using HarmonyLib;

[HarmonyPatch(typeof(ChemFinisherUIManager), nameof(ChemFinisherUIManager.OnChemRecipeSelected))]
internal static class ChemFinisherCloseSelectorPatch
{
    private static void Postfix()
    {
        if (!ChemQOLConfig.CloseChemFinisherSelector)
        {
            return;
        }

        ChemFinisherUIManager manager = ChemFinisherUIManager.Instance;
        if (manager == null)
        {
            ModEntry.DebugLog("Chem finisher selector close skipped: UI manager instance is null.");
            return;
        }

        manager.HideUI();
    }
}
