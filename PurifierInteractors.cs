using System;

internal sealed class PurifierCreatePlotInteractor
{
    private readonly IPurifierMath purifierMath;
    private readonly IPurifierPlotOutput output;

    public PurifierCreatePlotInteractor(
        IPurifierMath purifierMath,
        IPurifierPlotOutput output)
    {
        this.purifierMath = purifierMath;
        this.output = output;
    }

    public void TryRefresh(PurifierMachineState state)
    {

        PurifierCalculatorResult result = PurifierCalculator.Calculate(
            new PurifierCalculatorInput
            {
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
        if (!mouseoverPurity.HasValue)
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
            mouseoverPurity.Value,
            purifierMath);

        output.Present(result);
    }
}
