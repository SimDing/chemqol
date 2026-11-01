using HarmonyLib;

[HarmonyPatch(typeof(GameSlotInventory), nameof(GameSlotInventory.UncheckedAccept))]
internal static class ChemFinisherAutoSelectPatch
{
    private static void Postfix(GameSlotInventory __instance, GameItem item, bool __result)
    {
        if (!__result || item == null || !item.IsTag("LIQUID_CONTAINER_TAG"))
        {
            return;
        }

        GameItem machine = __instance.GetParentItem();
        if (machine == null || machine.identifier != "chem_finisher")
        {
            return;
        }

        ChemFinisherRecipe bestRecipe = null;
        int bestIngredientAmount = 0;

        foreach (ChemFinisherRecipe recipe in ChemFinisherRecipe.chemFinisherRecipes)
        {
            if (!recipe.CanOutput(item))
            {
                continue;
            }

            int ingredientAmount = item.GetTagReadonly(Liquid.GetCurrentPartFromId(recipe.activeIngredientId)).GetInt();
            if (ingredientAmount > bestIngredientAmount)
            {
                bestRecipe = recipe;
                bestIngredientAmount = ingredientAmount;
            }
        }

        if (bestRecipe == null)
        {
            machine.DisableTag(MachineChemFinisher.SELECTED_CHEM_ID_TAG);
            UnityEngine.Debug.Log("[ChemQOL] Chem finisher found no eligible output matching the input container.");
            return;
        }

        machine.ModifyTag(MachineChemFinisher.SELECTED_CHEM_ID_TAG, state =>
        {
            state.Enable().SetString(bestRecipe.identifier);
        });
        UnityEngine.Debug.Log($"[ChemQOL] Chem finisher selected '{bestRecipe.identifier}' from input ingredient volume {bestIngredientAmount}.");
    }
}
