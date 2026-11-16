using System;
using System.Collections.Generic;
using System.Linq;

internal interface IPurifierMath
{
    int GetMaxPurityCap(int quality);
    long GetRemovalBudget(long totalVolume, int inputPurity, int performance);
    List<int> GetSpecialPurities();
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
    public long[][] YValues { get; set; }
    public int[] Purities { get; set; }
}

internal sealed class PurifierDetailCalculationResult
{
    public int InputPurity { get; set; }
    public int[] Purities { get; set; }
    public long[] Capacities { get; set; }
}

internal static class PurifierCalculator
{
    private const int MinPurity = 8000;
    public const int PointCount = 50;
    private const long MaxContainerVolume = int.MaxValue;
    private const double MachineMaximumDisplayTolerance = 0.02;

    internal static readonly int[] TargetPurities = [9990, 9900, 9800, 9600, 9000];

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
        List<int> specialPurities = purifierMath.GetSpecialPurities();

        var xValues = new int[targetPurities.Length][];
        var yValues = new long[targetPurities.Length][];
        for (int series = 0; series < targetPurities.Length; series++)
        {
            (xValues[series], yValues[series]) = SampleSeries(input, purifierMath, targetPurities[series], specialPurities);
        }

        return new PurifierCalculatorResult
        {
            XValues = xValues,
            YValues = yValues,
            Purities = targetPurities
        };
    }

    private static (int[], long[]) SampleSeries(PurifierCalculatorInput input, IPurifierMath purifierMath, int targetPurity, List<int> specialPurities)
    {
        int[] xValues = SamplingGrid(input, purifierMath, targetPurity, specialPurities);
        long[] yValues = new long[xValues.Length];
        for (int i = 0; i < xValues.Length; i++)
        {
            yValues[i] = MaxVolumeForTarget(xValues[i], targetPurity, input.Performance, purifierMath);
        }
        return (xValues, yValues);
    }

    private static int[] SamplingGrid(PurifierCalculatorInput input, IPurifierMath purifierMath, int targetPurity, List<int> specialPurities)
    {
        int maximumInputPurity = Math.Max(
                                MinPurity,
                                PurityAtMaxVolume(input, purifierMath, targetPurity));
        int xRange = maximumInputPurity - MinPurity;
        List<int> importantPoints = ImportantSamplingPoints(specialPurities, maximumInputPurity);
        int pointCount = Math.Max(Convert.ToInt32((double)PointCount * xRange / 2000.0), importantPoints.Count + 2);
        int[] uniformGrid = UniformGrid(xRange, pointCount - importantPoints.Count);
        return uniformGrid.Union(importantPoints).OrderBy(x => x).ToArray();
    }

    private static int[] UniformGrid(int xRange, int pointCount)
    {
        int[] grid = new int[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            int inputPurity = MinPurity + xRange * i / (pointCount - 1);
            grid[i] = inputPurity;
        }

        return grid;
    }

    private static List<int> ImportantSamplingPoints(List<int> specialPurities, int maximumInputPurity)
    {
        List<int> filteredSpecialPurities = specialPurities.FindAll(purity => purity > MinPurity && purity < maximumInputPurity);
        filteredSpecialPurities.Sort((a, b) => a.CompareTo(b));
        return filteredSpecialPurities;
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
        var validPurities = new List<int>(purities.Length);
        var capacities = new List<long>(purities.Length);
        for (int series = 0; series < purities.Length; series++)
        {
            if (inputPurity < purities[series])
            {
                validPurities.Add(purities[series]);
                capacities.Add(MaxVolumeForTarget(inputPurity, purities[series], input.Performance, purifierMath));
            }
        }

        return new PurifierDetailCalculationResult
        {
            InputPurity = inputPurity,
            Purities = validPurities.ToArray(),
            Capacities = capacities.ToArray()
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