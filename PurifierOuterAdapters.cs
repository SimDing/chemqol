using System;
using System.Collections.Generic;

internal sealed class GamePurifierMachineStateProvider : IPurifierMachineStateProvider
{
    private readonly GameItem machine;

    public GamePurifierMachineStateProvider(GameItem machine)
    {
        this.machine = machine;
    }

    public PurifierMachineState ReadCurrent()
    {
        List<GameSlotInventory> waterContainerSlots = GraphUtils.FindChildrenType<GameSlotInventory>(
            machine,
            slot => slot.childItem != null
                && slot.childItem.IsTag("LIQUID_CONTAINER_TAG")
                && slot.childItem.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt() > 0);
        int? containerWaterPurity = waterContainerSlots.Count > 0
            ? WaterHelper.GetWaterPurity(waterContainerSlots[0].childItem)
            : (int?)null;
        bool hasContainerScanResult = waterContainerSlots.Count > 0
            && waterContainerSlots[0].childItem.IsTag("LAST_SCAN_RESULT_TAG");

        return new PurifierMachineState(
            MachineryHelper.GetCurrentQualityBonus(machine),
            MachineryHelper.GetCurrentPerformanceBonus(machine),
            containerWaterPurity,
            hasContainerScanResult
        );
    }
}

internal sealed class MachinePurifierMathAdapter : IPurifierMath
{
    public int GetMaxPurityCap(int quality)
    {
        return MachinePurifier.GetMaxPurityCap(quality);
    }

    public long GetRemovalBudget(long totalVolume, int inputPurity, int performance)
    {
        if (ChemQOLConfig.ExponentialPurifierBudget)
        {
            long removableImpurities = totalVolume * (10000L - inputPurity) / 10000L;
            double budgetFraction = PurifierExponentialBudgetPatch.GetBudgetFraction(totalVolume, performance);
            return (long)(removableImpurities * budgetFraction);
        }

        double multiplier = (MachinePurifier.GetDirtBonus(inputPurity) + performance / 100.0)
            * MachinePurifier.REMOVAL_MULTIPLIER;
        long budgetVolume = Math.Min(totalVolume, MachinePurifier.BUDGET_VOLUME_CAP_UL);
        long baseBudget = budgetVolume * MachinePurifier.BUDGET_VOLUME_PERMILLE / 1000L
            + MachinePurifier.BUDGET_FLAT_UL;
        return (long)(baseBudget * multiplier);
    }
}
