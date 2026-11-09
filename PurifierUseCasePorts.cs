using System;

internal readonly struct PurifierMachineState
{
    public int Quality { get; }
    public int Performance { get; }
    
    public PurifierMachineState(int quality, int performance)
    {
        Quality = quality;
        Performance = performance;
    }
}

internal interface IPurifierMachineStateProvider
{
    PurifierMachineState ReadCurrent();
}

internal interface IPurifierPlotOutput
{
    event Action<PurifierGraphViewModel> ViewModelChanged;

    void Present(PurifierCalculatorResult calculation);
}

internal interface IPurifierPlotDetailOutput
{
    event Action<PurifierPlotDetailViewModel> DetailChanged;

    void Present(PurifierDetailCalculationResult calculation);
    void Clear();
}
