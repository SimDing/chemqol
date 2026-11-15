internal sealed class PurifierXAxisTickViewModel
{
    public int Purity { get; set; }
    public string Label { get; set; }
    public bool IsVisible { get; set; }
}

internal sealed class PurifierLegendItemViewModel
{
    public int SeriesIndex { get; set; }
    public string Label { get; set; }
    public bool IsVisible { get; set; }
}

internal sealed class PurifierGridLevelViewModel
{
    public long Microliters { get; set; }
    public bool IsLabelVisible { get; set; }
    public bool IsGridLineVisible { get; set; }
    public string Label { get; set; }
}

internal sealed class PurifierPlotViewModel
{
    public string YAxisTitle { get; set; }
    public string XAxisTitle { get; set; }
    public int MinimumPurity { get; set; }
    public int MaximumPurity { get; set; }
    public int[][] XValues { get; set; }
    public long[][] YValues { get; set; }
    public int[] Purities { get; set; }
    public string[] SeriesColors { get; set; }
    public PurifierLegendItemViewModel[] LegendItems { get; set; }
    public PurifierXAxisTickViewModel[] XAxisTicks { get; set; }
    public PurifierGridLevelViewModel[] GridLevels { get; set; }
    public long MinimumMicroliters { get; set; }
    public long MaximumMicroliters { get; set; }
}

internal sealed class PurifierPlotDetailViewModel
{
    public int XValue { get; set; }
    public string[] Lines { get; set; }
    public bool IsVisible { get; set; }
}