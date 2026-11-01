using HarmonyLib;

[HarmonyPatch(typeof(GameSlotInventory), nameof(GameSlotInventory.UncheckedAccept))]
internal static class ChemFinisherAutoSelectPatch
{
    private static void Postfix(GameSlotInventory __instance, GameItem item, bool __result)
    {
        if (!ChemQOLConfig.ChemFinisherAutoSelect || !__result || item == null || !item.IsTag("LIQUID_CONTAINER_TAG"))
        {
            return;
        }

        GameItem machine = __instance.GetParentItem();
        if (machine == null || machine.identifier != "chem_finisher")
        {
            return;
        }

        ChemFinisherRecipe bestRecipe = FindBestRecipe(item, out int bestIngredientAmount);

        if (bestRecipe == null)
        {
            machine.DisableTag(MachineChemFinisher.SELECTED_CHEM_ID_TAG);
            ModEntry.Logger.Log("Chem finisher found no output matching the input container.");
            return;
        }

        machine.ModifyTag(MachineChemFinisher.SELECTED_CHEM_ID_TAG, state =>
        {
            state.Enable().SetString(bestRecipe.identifier);
        });
        ModEntry.Logger.Log($"Chem finisher selected '{bestRecipe.identifier}' from input ingredient volume {bestIngredientAmount}.");
    }

    private static ChemFinisherRecipe FindBestRecipe(GameItem item, out int bestIngredientAmount)
    {
        ChemFinisherRecipe bestRecipe = null;
        bestIngredientAmount = 0;

        foreach (ChemFinisherRecipe recipe in ChemFinisherRecipe.chemFinisherRecipes)
        {
            int ingredientAmount = item.GetTagReadonly(Liquid.GetCurrentPartFromId(recipe.activeIngredientId)).GetInt();
            if (ingredientAmount > bestIngredientAmount)
            {
                bestRecipe = recipe;
                bestIngredientAmount = ingredientAmount;
            }
        }

        return bestRecipe;
    }
}
