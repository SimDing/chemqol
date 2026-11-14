using System;
using System.Collections.Generic;

internal interface IPurifierMath
{
    int GetMaxPurityCap(int quality);
    long GetRemovalBudget(long totalVolume, int inputPurity, int performance);
}

internal sealed class PurifierCalculatorInput
{
    public long MaxVolume { get; set; }
    public int Quality { get; set; }
    public int Performance { get; set; }
}

internal sealed class PurifierCalculatorResult
{
    public int[][] XValues { get; set; }
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
    public const int PointCount = 50;
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

        var xValues = new int[targetPurities.Length][];
        var yValues = new double[targetPurities.Length][];
        for (int series = 0; series < targetPurities.Length; series++)
        {
            int maximumInputPurity = Math.Max(
                MinPurity,
                PurityAtMaxVolume(input, purifierMath, targetPurities[series]));
            xValues[series] = new int[PointCount];
            yValues[series] = new double[PointCount];
            for (int point = 0; point < PointCount; point++)
            {
                int inputPurity = MinPurity + (maximumInputPurity - MinPurity) * point / (PointCount - 1);
                xValues[series][point] = inputPurity;
                if (inputPurity <= targetPurities[series])
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

    private static int PurityAtMaxVolume(PurifierCalculatorInput input, IPurifierMath purifierMath, int targetPurity)
    {
        int low = 0;
        int high = purifierMath.GetMaxPurityCap(input.Quality);
        while (low < high)
        {
            int middle = low + (high - low + 1) / 2;
            long maxVolume = MaxVolumeForTarget(middle, targetPurity, input.Performance, purifierMath);
            if (maxVolume <= input.MaxVolume)
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
        long low = 0;
        long high = MaxContainerVolume;
        while (low < high)
        {
            long middle = low + (high - low + 1) / 2;
            long requiredRemoval = (middle * (targetPurity - inputPurity) + 9999) / 10000;
            long budget = purifierMath.GetRemovalBudget(middle, inputPurity, performance);
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