using System;
using System.Collections.Generic;

internal sealed class PurifierPlotPresenter : IPurifierPlotOutput
{
    private const long _1_L = 1000000;
    private const long _10_L = 10000000;
    private const long _1_mL = 1000;
    private const long _10_mL = 10000;

    private const int MinTickPurity = 8000;
    private const int TickPurityStep = 500;
    private const int MaxTickCount = 5;
    private const int MaxLegendCount = 6;
    private const long MinimumFloorMicroliters = 100000;
    private static readonly long[] DefaultGridLevelsMicroliters = { 3000000, 2000000, 1000000, 750000, 375000 };
    private PurifierPlotViewModel currentViewModel;

    public event Action<PurifierPlotViewModel> ViewModelChanged;

    public void Present(PurifierCalculatorResult calculation, long maximumMicroliters)
    {
        int[] legendPurities = (int[])calculation.Purities.Clone();
        Array.Sort(legendPurities);
        long minimumMicroliters = FindMinimumMicroliters(calculation.YValues, maximumMicroliters);
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
            GridLevels = BuildGridLevels(minimumMicroliters, maximumMicroliters),
            MinimumMicroliters = minimumMicroliters,
            MaximumMicroliters = maximumMicroliters
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

    private PurifierGridLevelViewModel[] BuildGridLevels(long minimumMicroliters, long maximumMicroliters)
    {
        var gridLevels = new PurifierGridLevelViewModel[DefaultGridLevelsMicroliters.Length + 2];
        gridLevels[0] = new PurifierGridLevelViewModel
        {
            Microliters = maximumMicroliters,
            IsLabelVisible = true,
            IsGridLineVisible = false,
            Label = FormatLiters(maximumMicroliters)
        };

        for (int i = 0; i < DefaultGridLevelsMicroliters.Length; i++)
        {
            long microliters = DefaultGridLevelsMicroliters[i];
            bool visible = microliters >= minimumMicroliters * 4L / 3L
                && microliters <= maximumMicroliters * 3L / 4L;
            gridLevels[i + 1] = new PurifierGridLevelViewModel
            {
                Microliters = microliters,
                IsLabelVisible = visible,
                IsGridLineVisible = visible,
                Label = FormatLiters(microliters)
            };
        }

        gridLevels[gridLevels.Length - 1] = new PurifierGridLevelViewModel
        {
            Microliters = minimumMicroliters,
            IsLabelVisible = true,
            IsGridLineVisible = false,
            Label = FormatLiters(minimumMicroliters)
        };

        return gridLevels;
    }

    private long FindMinimumMicroliters(long[][] values, long maximumMicroliters)
    {
        long minimumMicroliters = long.MaxValue;
        for (int series = 0; series < values.Length; series++)
        {
            for (int point = 0; point < values[series].Length; point++)
            {
                minimumMicroliters = Math.Min(
                    minimumMicroliters,
                    Math.Max(MinimumFloorMicroliters, values[series][point]));
            }
        }

        if (minimumMicroliters == long.MaxValue || minimumMicroliters >= maximumMicroliters)
        {
            return MinimumFloorMicroliters;
        }

        return minimumMicroliters;
    }

    public static string FormatLiters(long microliters)
    {
        // The target here is the we can never display more than the given amount
        // and also display in a compact way.
        
        // Truncate all but the 4 most significant digits.
        long remainingDigits = microliters;
        int multiplier = 1;

        while (remainingDigits >= 10000 && multiplier < _1_L)
        {
            remainingDigits /= 10;
            multiplier *= 10;
        }
        
        double amount = microliters;
        long reconstructed = remainingDigits * multiplier;
        string unit = " uL";

        // Diplay with larger prefixes than uL, if either
        // - the displayed amount is greater or equal 10
        // - the diplayed amount is single digit.
        if (reconstructed >= _10_L || reconstructed > _1_L && reconstructed % _1_L == 0)
        {
            amount = remainingDigits / (double) (_1_L / multiplier);
            unit = " L";
        }
        else if (reconstructed >= _10_mL || reconstructed > _1_mL && reconstructed % _1_mL == 0)
        {
            amount = remainingDigits / (double) (_1_mL / multiplier);
            unit = " mL";
        }

        return amount.ToString("0.##") + unit;
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
            detailLines.Add((calculation.Purities[series] / 100.0).ToString("0.##") + "% " + PurifierPlotPresenter.FormatLiters(calculation.Capacities[series]));
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

}
