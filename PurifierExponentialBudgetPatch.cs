using System;
using HarmonyLib;

[HarmonyPatch(typeof(MachinePurifier), nameof(MachinePurifier.PurifyContainer))]
internal static class PurifierExponentialBudgetPatch
{
    /* At performance 0, half-life in purifier cycles:
     * 0.375 L => 0.091203576389
     * 0.75 L  => 0.182407152779
     * 1 L     => 0.243209537039
     * 2 L     => 0.486419074077
     * 3 L     => 0.729628611116
     * 6 L     => 1.459257222231.
     */
    internal const double BudgetDecayA = 28500.0;

    private static readonly Func<GameItem, int, long, int> RemoveLargestFirst =
        AccessTools.MethodDelegate<Func<GameItem, int, long, int>>(
            AccessTools.Method(
                typeof(MachinePurifier),
                "RemoveLargestFirst",
                new[] { typeof(GameItem), typeof(int), typeof(long) }));

    private static bool Prefix(GameItem machine, GameItem waterContainer, bool ignoreBonus, ref int __result)
    {
        if (!ChemQOLConfig.ExponentialPurifierBudget)
        {
            return true;
        }

        __result = PurifyContainer(machine, waterContainer, ignoreBonus);
        return false;
    }

    private static int PurifyContainer(GameItem machine, GameItem waterContainer, bool ignoreBonus)
    {
        if (!waterContainer.IsTag("LIQUID_CONTAINER_TAG"))
        {
            return 0;
        }
        ModEntry.DebugLog("Purifying water container");
        int performance = MachineryHelper.GetCurrentPerformanceBonus(machine);
        int quality = MachineryHelper.GetCurrentQualityBonus(machine);
        int totalVolume = WaterHelper.GetTotalVolume(waterContainer);
        if (ignoreBonus)
        {
            performance = 0;
            quality = 0;
        }
        long removableImpurities = GetCurrentRemovableImpurities(waterContainer);
        double budgetFraction = GetBudgetFraction(totalVolume, performance);
        long budget = (long)(removableImpurities * budgetFraction);
        ModEntry.DebugLog("Exponential budget fraction: " + budgetFraction);

        int result = RemoveLargestFirst(waterContainer, MachinePurifier.GetMaxPurityCap(quality), budget);
        WaterFeatureHelper.UpdateWaterFeature(waterContainer, isExpose: false);
        return result;
    }

    internal static double GetBudgetFraction(long totalVolume, int performance)
    {
        if (totalVolume <= 0)
        {
            return 0.0;
        }
        if (performance <= 0)
        {
            return 0.0;
        }
        double fraction = 1.0 - Math.Exp(-BudgetDecayA * (100.0 + performance) / totalVolume);
        return Math.Max(0.0, Math.Min(1.0, fraction));
    }

    private static long GetCurrentRemovableImpurities(GameItem container)
    {
        int totalVolume = WaterHelper.GetTotalVolume(container);
        int purity = WaterHelper.GetWaterPurity(container);
        int impurityPermille = Math.Max(0, Math.Min(10000, 10000 - purity));
        return (long)totalVolume * impurityPermille / 10000;
    }

}