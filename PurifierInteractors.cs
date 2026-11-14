using System;

internal sealed class PurifierCreatePlotInteractor
{
    private readonly IPurifierMath purifierMath;
    private readonly IPurifierPlotOutput output;
    private readonly long maxVolume;

    public PurifierCreatePlotInteractor(
        IPurifierMath purifierMath,
        IPurifierPlotOutput output,
        long maxVolume)
    {
        this.purifierMath = purifierMath;
        this.output = output;
        this.maxVolume = maxVolume;
    }

    public void TryRefresh(PurifierMachineState state)
    {

        PurifierCalculatorResult result = PurifierCalculator.Calculate(
            new PurifierCalculatorInput
            {
                MaxVolume = maxVolume,
                Quality = state.Quality,
                Performance = state.Performance
            },
            purifierMath);

        output.Present(result);
    }
}

internal sealed class PurifierCreatePlotDetailInteractor
{
    private readonly IPurifierMath purifierMath;
    private readonly IPurifierPlotDetailOutput output;

    public PurifierCreatePlotDetailInteractor(IPurifierMath purifierMath, IPurifierPlotDetailOutput output)
    {
        this.purifierMath = purifierMath;
        this.output = output;
    }

    public void TryCreate(PurifierMachineState state, int? mouseoverPurity)
    {
        int? readoutPurity = mouseoverPurity ?? (mayReadContainerPurity(state) ? state.ContainerWaterPurity : null);
        
        if (!readoutPurity.HasValue)
        {
            output.Clear();
            return;
        }

        PurifierDetailCalculationResult result = PurifierCalculator.CalculateDetail(
            new PurifierCalculatorInput
            {
                Quality = state.Quality,
                Performance = state.Performance
            },
            readoutPurity.Value,
            purifierMath);

        output.Present(result);
    }

    private bool mayReadContainerPurity(PurifierMachineState state)
    {
        return state.HasContainerScanResult || !ChemQOLConfig.RequirePurifierPlotScanTag;
    }
}
