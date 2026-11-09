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

        return new PurifierMachineState(
            MachineryHelper.GetCurrentQualityBonus(machine),
            MachineryHelper.GetCurrentPerformanceBonus(machine),
            containerWaterPurity
        );
    }
}

internal sealed class MachinePurifierMathAdapter : IPurifierMath
{
    public int GetMaxPurityCap(int quality)
    {
        return MachinePurifier.GetMaxPurityCap(quality);
    }

    public double GetDirtBonus(int inputPurity)
    {
        return MachinePurifier.GetDirtBonus(inputPurity);
    }
}
