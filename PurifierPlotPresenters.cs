using System;
using System.Collections.Generic;

internal sealed class PurifierPlotPresenter : IPurifierPlotOutput
{
    private const int MinTickPurity = 8000;
    private const int TickPurityStep = 500;
    private const int MaxTickCount = 5;
    private const int MaxLegendCount = 6;
    private const double MinimumFloorLiters = 0.1;
    private static readonly double[] DefaultGridLevels = { 3.0, 2.0, 1.0, 0.75, 0.375 };
    private PurifierPlotViewModel currentViewModel;

    public event Action<PurifierPlotViewModel> ViewModelChanged;

    public void Present(PurifierCalculatorResult calculation, double maximumLiters)
    {
        int[] legendPurities = (int[])calculation.Purities.Clone();
        Array.Sort(legendPurities);
        double minimumLiters = FindMinimumLiters(calculation.YValues, maximumLiters);
        int minimumPurity = calculation.XValues[0][0];
        int maximumPurity = FindMaximumPurity(calculation.Purities);

        currentViewModel = new PurifierPlotViewModel
        {
            YAxisTitle = "L/cycle",
            XAxisTitle = "Input purity (%)",
            MinimumPurity = minimumPurity,
            MaximumPurity = maximumPurity,
            XValues = calculation.XValues,
            YValues = calculation.YValues,
            Purities = calculation.Purities,
            SeriesColors = BuildSeriesColors(calculation.Purities),
            LegendItems = BuildLegendItems(legendPurities, calculation.Purities),
            XAxisTicks = BuildXAxisTicks(minimumPurity, maximumPurity),
            GridLevels = BuildGridLevels(minimumLiters, maximumLiters),
            MinimumLiters = minimumLiters,
            MaximumLiters = maximumLiters
        };

        ViewModelChanged?.Invoke(currentViewModel);
    }

    private static int FindMaximumPurity(int[] purities)
    {
        int maximumPurity = purities[0];
        for (int series = 1; series < purities.Length; series++)
        {
            maximumPurity = Math.Max(maximumPurity, purities[series]);
        }

        return maximumPurity - 1;
    }

    private PurifierLegendItemViewModel[] BuildLegendItems(int[] legendPurities, int[] seriesPurities)
    {
        var items = new PurifierLegendItemViewModel[MaxLegendCount];
        for (int i = 0; i < MaxLegendCount; i++)
        {
            bool isVisible = i < legendPurities.Length;
            int purity = isVisible ? legendPurities[i] : 0;
            items[i] = new PurifierLegendItemViewModel
            {
                IsVisible = isVisible,
                SeriesIndex = isVisible ? Array.IndexOf(seriesPurities, purity) : -1,
                Label = isVisible ? (purity / 100.0).ToString("0.##") + "%" : string.Empty
            };
        }

        return items;
    }

    private static string[] BuildSeriesColors(int[] purities)
    {
        var colors = new string[purities.Length];
        for (int i = 0; i < purities.Length; i++)
        {
            colors[i] = GetSeriesColorHex(purities[i]);
        }

        return colors;
    }

    private static string GetSeriesColorHex(int purity)
    {
        switch (purity)
        {
            case 9990: return "3d823f";
            case 9900: return "7ab053";
            case 9800: return "ccb987";
            case 9600: return "ba8953";
            case 9000: return "b26b4e";
            default: return "78c9df";
        }
    }

    private PurifierXAxisTickViewModel[] BuildXAxisTicks(int minimumPurity, int maximumPurity)
    {
        var ticks = new List<int>(MaxTickCount);

        for (int purity = MinTickPurity; purity < maximumPurity && ticks.Count < MaxTickCount - 1; purity += TickPurityStep)
        {
            if (purity >= minimumPurity)
            {
                ticks.Add(purity);
            }
        }

        if (ticks.Count == 0 || ticks[ticks.Count - 1] != maximumPurity)
        {
            ticks.Add(maximumPurity);
        }

        var viewModels = new PurifierXAxisTickViewModel[MaxTickCount];
        for (int i = 0; i < MaxTickCount; i++)
        {
            bool isVisible = i < ticks.Count;
            int purity = isVisible ? ticks[i] : 0;
            viewModels[i] = new PurifierXAxisTickViewModel
            {
                IsVisible = isVisible,
                Purity = purity,
                Label = isVisible ? (purity / 100.0).ToString("0.##") : string.Empty
            };
        }

        return viewModels;
    }

    private PurifierGridLevelViewModel[] BuildGridLevels(double minimumLiters, double maximumLiters)
    {
        var gridLevels = new PurifierGridLevelViewModel[DefaultGridLevels.Length + 2];
        gridLevels[0] = new PurifierGridLevelViewModel
        {
            Liters = maximumLiters,
            IsLabelVisible = true,
            IsGridLineVisible = false,
            Label = FormatLiters(maximumLiters)
        };

        for (int i = 0; i < DefaultGridLevels.Length; i++)
        {
            double liters = DefaultGridLevels[i];
            bool visible = liters >= minimumLiters * 4.0 / 3.0 && liters <= maximumLiters * 3.0 / 4.0;
            gridLevels[i + 1] = new PurifierGridLevelViewModel
            {
                Liters = liters,
                IsLabelVisible = visible,
                IsGridLineVisible = visible,
                Label = FormatGridLevel(liters)
            };
        }

        gridLevels[gridLevels.Length - 1] = new PurifierGridLevelViewModel
        {
            Liters = minimumLiters,
            IsLabelVisible = true,
            IsGridLineVisible = false,
            Label = FormatLiters(minimumLiters)
        };

        return gridLevels;
    }

    private double FindMinimumLiters(double[][] values, double maximumLiters)
    {
        double minimumLiters = double.PositiveInfinity;
        for (int series = 0; series < values.Length; series++)
        {
            for (int point = 0; point < values[series].Length; point++)
            {
                if (!double.IsNaN(values[series][point]))
                {
                    minimumLiters = Math.Min(minimumLiters, Math.Max(MinimumFloorLiters, values[series][point]));
                }
            }
        }

        if (double.IsInfinity(minimumLiters) || minimumLiters >= maximumLiters)
        {
            return MinimumFloorLiters;
        }

        return minimumLiters;
    }

    private static string FormatLiters(double liters)
    {
        return liters.ToString(liters < 1.0 ? "0.###" : "0.##")  + "L";
    }

    private static string FormatGridLevel(double liters)
    {
        return liters < 1.0
            ? (liters * 1000.0).ToString("0.###") + "ml"
            : FormatLiters(liters);
    }
}

internal sealed class PurifierPlotDetailPresenter : IPurifierPlotDetailOutput
{
    public event Action<PurifierPlotDetailViewModel> DetailChanged;

    public void Present(PurifierDetailCalculationResult calculation)
    {
        var detailLines = new List<string>(calculation.Purities.Length + 1)
        {
            "Input " + (calculation.InputPurity / 100.0).ToString("0.##") + "%"
        };

        for (int series = 0; series < calculation.Purities.Length; series++)
        {
            double capacity = calculation.Capacities[series];
            if (!double.IsNaN(capacity))
            {
                detailLines.Add((calculation.Purities[series] / 100.0).ToString("0.##") + "% " + FormatReadingLiters(capacity) + "L");
            }
        }

        DetailChanged?.Invoke(new PurifierPlotDetailViewModel
        {
            XValue = calculation.InputPurity,
            Lines = detailLines.ToArray(),
            IsVisible = true
        });
    }

    public void Clear()
    {
        DetailChanged?.Invoke(new PurifierPlotDetailViewModel
        {
            XValue = 0,
            Lines = Array.Empty<string>(),
            IsVisible = false
        });
    }

    private static string FormatReadingLiters(double liters)
    {
        if (liters <= 0.0)
        {
            return "0 ";
        }

        decimal value = (decimal)liters;
        int exponent = (int)Math.Floor(Math.Log10(liters));
        decimal placeValue = (decimal)Math.Pow(10.0, exponent - 2);
        decimal roundedDown = decimal.Floor(value / placeValue) * placeValue;
        return roundedDown.ToString("G");
    }

}
