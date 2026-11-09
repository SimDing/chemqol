using System;
using System.Collections.Generic;

internal interface IPurifierMath
{
    int GetMaxPurityCap(int quality);
    double GetDirtBonus(int inputPurity);
}

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

internal sealed class PurifierDetailCalculationResult
{
    public int InputPurity { get; set; }
    public int[] Purities { get; set; }
    public double[] Capacities { get; set; }
}

internal static class PurifierCalculator
{
    private const int MinPurity = 8000;
    private const int PointCount = 185;
    private const long MaxContainerVolume = int.MaxValue;
    private const double MachineMaximumDisplayTolerance = 0.02;

    internal static readonly int[] TargetPurities = { 9990, 9900, 9800, 9600, 9000 };

    public static PurifierCalculatorResult Calculate(PurifierCalculatorInput input, IPurifierMath purifierMath)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (purifierMath == null)
        {
            throw new ArgumentNullException(nameof(purifierMath));
        }

        int machineMaximum = purifierMath.GetMaxPurityCap(input.Quality);
        int[] targetPurities = GetTargetPurities(machineMaximum);

        var xValues = new int[PointCount];
        for (int point = 0; point < PointCount; point++)
        {
            xValues[point] = MinPurity + (machineMaximum - MinPurity) * point / (PointCount - 1);
        }

        var yValues = new double[targetPurities.Length][];
        for (int series = 0; series < targetPurities.Length; series++)
        {
            yValues[series] = new double[PointCount];
            for (int point = 0; point < PointCount; point++)
            {
                int inputPurity = xValues[point];
                if (inputPurity < targetPurities[series])
                {
                    yValues[series][point] = MaxVolumeForTarget(inputPurity, targetPurities[series], input.Performance, purifierMath) / 1000000.0;
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

    public static PurifierDetailCalculationResult CalculateDetail(
        PurifierCalculatorInput input,
        int inputPurity,
        IPurifierMath purifierMath)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (purifierMath == null)
        {
            throw new ArgumentNullException(nameof(purifierMath));
        }

        int[] purities = GetTargetPurities(purifierMath.GetMaxPurityCap(input.Quality));
        var capacities = new double[purities.Length];
        for (int series = 0; series < purities.Length; series++)
        {
            capacities[series] = inputPurity < purities[series]
                ? MaxVolumeForTarget(inputPurity, purities[series], input.Performance, purifierMath) / 1000000.0
                : double.NaN;
        }

        return new PurifierDetailCalculationResult
        {
            InputPurity = inputPurity,
            Purities = purities,
            Capacities = capacities
        };
    }

    private static int[] GetTargetPurities(int machineMaximum)
    {
        var purities = new List<int>();
        foreach (int target in TargetPurities)
        {
            if (target <= machineMaximum)
            {
                purities.Add(target);
            }
        }

        bool hasNearbyTarget = purities.Exists(target =>
            Math.Abs(machineMaximum / 100.0 - target / 100.0) <= MachineMaximumDisplayTolerance);
        if (!hasNearbyTarget)
        {
            purities.Add(machineMaximum);
        }
        return purities.ToArray();
    }

    private static long MaxVolumeForTarget(int inputPurity, int targetPurity, int performance, IPurifierMath purifierMath)
    {
        double multiplier = (purifierMath.GetDirtBonus(inputPurity) + performance / 100.0) * 1.25;
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