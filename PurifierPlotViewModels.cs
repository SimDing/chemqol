internal sealed class PurifierXAxisTickViewModel
{
    public int Purity { get; set; }
    public string Label { get; set; }
    public bool IsVisible { get; set; }
}

internal sealed class PurifierLegendItemViewModel
{
    public int Purity { get; set; }
    public string Label { get; set; }
    public bool IsVisible { get; set; }
}

internal sealed class PurifierGridLevelViewModel
{
    public double Liters { get; set; }
    public bool IsVisible { get; set; }
}

internal sealed class PurifierPlotViewModel
{
    public int MinimumPurity { get; set; }
    public int MaximumPurity { get; set; }
    public int[][] XValues { get; set; }
    public double[][] YValues { get; set; }
    public int[] Purities { get; set; }
    public PurifierLegendItemViewModel[] LegendItems { get; set; }
    public PurifierXAxisTickViewModel[] XAxisTicks { get; set; }
    public PurifierGridLevelViewModel[] GridLevels { get; set; }
    public double MinimumLiters { get; set; }
    public double MaximumLiters { get; set; }
    public string MaximumLitersLabel { get; set; }
    public string MinimumLitersLabel { get; set; }
}

internal sealed class PurifierPlotDetailViewModel
{
    public int XValue { get; set; }
    public string[] Lines { get; set; }
    public bool IsVisible { get; set; }
}