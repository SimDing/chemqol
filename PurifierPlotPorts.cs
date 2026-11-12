using System;

internal readonly struct PurifierMachineState
{
    public int Quality { get; }
    public int Performance { get; }
    public int? ContainerWaterPurity { get; }
    
    public PurifierMachineState(int quality, int performance, int? containerWaterPurity)
    {
        Quality = quality;
        Performance = performance;
        ContainerWaterPurity = containerWaterPurity;
    }
}

internal interface IPurifierMachineStateProvider
{
    PurifierMachineState ReadCurrent();
}

internal interface IPurifierPlotOutput
{
    event Action<PurifierPlotViewModel> ViewModelChanged;

    void Present(PurifierCalculatorResult calculation);
}

internal interface IPurifierPlotDetailOutput
{
    event Action<PurifierPlotDetailViewModel> DetailChanged;

    void Present(PurifierDetailCalculationResult calculation);
    void Clear();
}
