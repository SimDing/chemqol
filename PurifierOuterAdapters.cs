internal sealed class GamePurifierMachineStateProvider : IPurifierMachineStateProvider
{
    private readonly GameItem machine;

    public GamePurifierMachineStateProvider(GameItem machine)
    {
        this.machine = machine;
    }

    public PurifierMachineState ReadCurrent()
    {
        return new PurifierMachineState(
            MachineryHelper.GetCurrentPerformanceBonus(machine),
            MachineryHelper.GetCurrentQualityBonus(machine)
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
