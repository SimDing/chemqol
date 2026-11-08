using System;
using System.Collections.Generic;

internal sealed class PurifierCalculatorInput
{
    public int Quality { get; set; }
    public int Performance { get; set; }
}

internal sealed class PurifierCalculatorResult
{
    public int[] XValues { get; set; }
    public double[][] YValues { get; set; }
    public int[] Purities { get; set; }
}

internal static class PurifierCalculator
{
    private const int MinPurity = 8000;
    private const int MaxPurity = 10000;
    private const int PointCount = 185;
    private const long MaxContainerVolume = int.MaxValue;

    internal static readonly int[] TargetPurities = { 9800, 9900, 9990 };

    public static PurifierCalculatorResult Calculate(PurifierCalculatorInput input)
    {
        int machineMaximum = MachinePurifier.GetMaxPurityCap(input.Quality);
        var purities = new List<int>(TargetPurities);
        if (!purities.Contains(machineMaximum))
        {
            purities.Add(machineMaximum);
        }

        var xValues = new int[PointCount];
        for (int point = 0; point < PointCount; point++)
        {
            xValues[point] = MinPurity + (MaxPurity - MinPurity) * point / (PointCount - 1);
        }

        int[] targetPurities = purities.ToArray();
        var yValues = new double[targetPurities.Length][];
        for (int series = 0; series < targetPurities.Length; series++)
        {
            yValues[series] = new double[PointCount];
            for (int point = 0; point < PointCount; point++)
            {
                int inputPurity = xValues[point];
                if (targetPurities[series] <= machineMaximum && inputPurity < targetPurities[series])
                {
                    yValues[series][point] = MaxVolumeForTarget(inputPurity, targetPurities[series], input.Performance) / 1000000.0;
                }
                else
                {
                    yValues[series][point] = double.NaN;
                }
            }
        }

        return new PurifierCalculatorResult
        {
            XValues = xValues,
            YValues = yValues,
            Purities = targetPurities
        };
    }

    private static long MaxVolumeForTarget(int inputPurity, int targetPurity, int performance)
    {
        double multiplier = (MachinePurifier.GetDirtBonus(inputPurity) + performance / 100.0) * 1.25;
        long low = 0;
        long high = MaxContainerVolume;
        while (low < high)
        {
            long middle = low + (high - low + 1) / 2;
            long requiredRemoval = (middle * (targetPurity - inputPurity) + 9999) / 10000;
            long budgetVolume = Math.Min(middle, 10000000L);
            long budget = (long)((budgetVolume * 3 / 1000 + 25000) * multiplier);
            if (requiredRemoval <= budget)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }
        return low;
    }
}