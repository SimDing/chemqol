using HarmonyLib;

[HarmonyPatch(typeof(ChemFinisherUIManager), nameof(ChemFinisherUIManager.OnChemRecipeSelected))]
internal static class ChemFinisherCloseSelectorPatch
{
    private static void Postfix()
    {
        ChemFinisherUIManager.Instance.HideUI();
    }
}
