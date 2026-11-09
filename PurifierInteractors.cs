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
    private readonly IPurifierPlotDetailOutput output;

    public PurifierCreatePlotDetailInteractor(IPurifierPlotDetailOutput output)
    {
        this.output = output;
    }

    public void TryCreate(float normalizedX)
    {
        output.TryPresent(normalizedX);
    }

    public void Clear()
    {
        output.Clear();
    }
}
