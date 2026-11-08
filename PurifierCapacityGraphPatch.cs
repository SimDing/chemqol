using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

[HarmonyPatch(typeof(MachinePurifier), nameof(MachinePurifier.WaterPurifier))]
internal static class PurifierCapacityGraphPatch
{
    private static void Postfix(GameItem __result)
    {
        if (__result?.contentWindow?.childElement is MachineryPanelRow row)
        {
            var graph = new PurifierCapacityGraph(__result);
            row.Add(new MachineryPanel("Purification capacity", graph, "purifier-capacity"));
        }
    }
}

internal sealed class PurifierCapacityGraph : PixelElement
{
    private const int Width = 224;
    private const int Height = 126;
    private const int PlotLeft = 31;
    private const int PlotTop = 10;
    private const int PlotWidth = 184;
    private const int PlotHeight = 66;
    private const int PointCount = PlotWidth + 1;
    private const double MinimumFloorLiters = 0.1;
    private const double MaxLiters = 6.0;
    private static readonly Color[] SeriesColors =
    {
        MachineryStyle.Hex("9cdb73"),
        MachineryStyle.Hex("78c9df"),
        MachineryStyle.Hex("e5d16e"),
        MachineryStyle.Hex("e59b68")
    };

    private readonly GameItem machine;
    private readonly TreeNodeRender plotViewport;
    private readonly TreeNodeRender[] lineSegments = new TreeNodeRender[4 * (PointCount - 1)];
    private readonly TreeNodeRender[] axes = new TreeNodeRender[5];
    private readonly List<MachineryText> labels = new List<MachineryText>();
    private readonly TreeNodeRender[] legendMarkers = new TreeNodeRender[4];
    private readonly MachineryText[] legendLabels = new MachineryText[4];
    private int lastPerformance = int.MinValue;
    private int lastQuality = int.MinValue;

    public bool canResize => false;
    public int widthPixels => Width;
    public int heightPixels => Height;
    public TreeNodeRender handler { get; private set; }
    public RectTransform rectTransform => handler.rectTransform;
    public TreeNodeRender inner => handler;

    public PurifierCapacityGraph(GameItem machine)
    {
        this.machine = machine;
        handler = TreeNodeRender.Instantiate(this, "PurifierCapacityGraph");
        plotViewport = TreeNodeRender.Instantiate().SetParent(handler);
        plotViewport.image.enabled = false;
        plotViewport.rectTransform.anchoredPosition = new Vector2(PlotLeft, -PlotTop);
        plotViewport.rectTransform.sizeDelta = new Vector2(PlotWidth + 1, PlotHeight + 1);
        RectMask2D plotMask = plotViewport.mask ?? plotViewport.gameObject.AddComponent<RectMask2D>();
        plotMask.enabled = true;

        for (int i = 0; i < lineSegments.Length; i++)
        {
            lineSegments[i] = CreateBlock(Color.white, plotViewport);
            lineSegments[i].image.enabled = false;
        }

        for (int i = 0; i < axes.Length; i++)
        {
            axes[i] = CreateBlock(MachineryStyle.Hex("77776f"));
        }

        AddLabel("L/cycle", 0, 0, PlotLeft - 2, 9);
        AddLabel("80", PlotLeft - 8, PlotTop + PlotHeight + 1, 16, 8);
        AddLabel("85", PlotLeft + PlotWidth * 0.25f - 8, PlotTop + PlotHeight + 1, 16, 8);
        AddLabel("90", PlotLeft + PlotWidth * 0.5f - 8, PlotTop + PlotHeight + 1, 16, 8);
        AddLabel("95", PlotLeft + PlotWidth * 0.75f - 8, PlotTop + PlotHeight + 1, 16, 8);
        AddLabel("100", PlotLeft + PlotWidth - 8, PlotTop + PlotHeight + 1, 16, 8);
        AddLabel("Input purity (%)", PlotLeft + 29, PlotTop + PlotHeight + 8, 118, 9);
        AddLabel("0", 0, PlotTop + PlotHeight - 4, PlotLeft - 3, 8);
        AddLabel("", 0, PlotTop + PlotHeight / 2 - 4, PlotLeft - 3, 8);
        AddLabel("", 0, PlotTop - 4, PlotLeft - 3, 8);

        for (int i = 0; i < 4; i++)
        {
            CreateLegend(i);
        }

        handler.gameObject.AddComponent<PurifierCapacityGraphWatcher>().Initialize(this);
        Validate();
    }

    public void Validate()
    {
        RefreshIfNeeded();
    }

    internal void RefreshIfNeeded()
    {
        int performance = MachineryHelper.GetCurrentPerformanceBonus(machine);
        int quality = MachineryHelper.GetCurrentQualityBonus(machine);
        if (performance == lastPerformance && quality == lastQuality)
        {
            return;
        }

        lastPerformance = performance;
        lastQuality = quality;
        Draw(performance, quality);
    }

    private void Draw(int performance, int quality)
    {
        int machineMaximum = MachinePurifier.GetMaxPurityCap(quality);
        var calculation = PurifierCalculator.Calculate(new PurifierCalculatorInput
        {
            Quality = quality,
            Performance = performance
        });
        int[] xValues = calculation.XValues;
        double[][] values = calculation.YValues;
        int[] purities = calculation.Purities;
        for (int i = 0; i < legendLabels.Length; i++)
        {
            legendLabels[i].handler.gameObject.SetActive(i < purities.Length);
            legendMarkers[i].gameObject.SetActive(i < purities.Length);
        }

        double minimumLiters = double.PositiveInfinity;
        for (int series = 0; series < purities.Length; series++)
        {
            for (int point = 0; point < values[series].Length; point++)
            {
                if (!double.IsNaN(values[series][point]))
                {
                    minimumLiters = Math.Min(minimumLiters, Math.Max(MinimumFloorLiters, values[series][point]));
                }
            }
            bool isDistinctMachineMaximum = purities[series] == machineMaximum && Array.IndexOf(PurifierCalculator.TargetPurities, machineMaximum) < 0;
            UpdateLegend(series, purities[series], purities[series] > machineMaximum, isDistinctMachineMaximum);
        }

        if (double.IsInfinity(minimumLiters) || minimumLiters >= MaxLiters)
        {
            minimumLiters = MinimumFloorLiters;
        }

        double logYRange = Math.Log(MaxLiters / minimumLiters);
        double middleLiters = Math.Sqrt(minimumLiters * MaxLiters);
        int middleY = PlotHeight - Mathf.RoundToInt((float)(Math.Log(middleLiters / minimumLiters) / logYRange * PlotHeight));
        SetLabelText(7, FormatLiters(minimumLiters));
        SetLabelText(8, FormatLiters(middleLiters));
        SetLabelText(9, "6");
        MachineryStyle.Place(labels[8].handler, 0, PlotTop + middleY - 4, labels[8].widthPixels, labels[8].heightPixels);

        MachineryStyle.Place(axes[0], PlotLeft, PlotTop, 1, PlotHeight + 1);
        MachineryStyle.Place(axes[1], PlotLeft, PlotTop + PlotHeight, PlotWidth + 1, 1);
        MachineryStyle.Place(axes[2], PlotLeft, PlotTop + middleY, PlotWidth + 1, 1);
        MachineryStyle.Place(axes[3], PlotLeft, PlotTop, PlotWidth + 1, 1);
        axes[2].image.color = MachineryStyle.Hex("77776f", 0.35f);
        axes[3].image.color = MachineryStyle.Hex("77776f", 0.35f);
        axes[4].image.enabled = false;

        int segmentIndex = 0;
        for (int series = 0; series < 4; series++)
        {
            for (int point = 0; point < xValues.Length - 1; point++)
            {
                TreeNodeRender segment = lineSegments[segmentIndex++];
                if (series >= values.Length)
                {
                    segment.image.enabled = false;
                    continue;
                }

                double[] capacities = values[series];
                if (double.IsNaN(capacities[point]) || double.IsNaN(capacities[point + 1]))
                {
                    segment.image.enabled = false;
                    continue;
                }

                float xRange = xValues[xValues.Length - 1] - xValues[0];
                float x1 = (float)PlotWidth * (xValues[point] - xValues[0]) / xRange;
                float x2 = (float)PlotWidth * (xValues[point + 1] - xValues[0]) / xRange;
                float y1 = PlotHeight - (float)(Math.Log(Math.Max(minimumLiters, capacities[point]) / minimumLiters) / logYRange * PlotHeight);
                float y2 = PlotHeight - (float)(Math.Log(Math.Max(minimumLiters, capacities[point + 1]) / minimumLiters) / logYRange * PlotHeight);
                segment.image.color = SeriesColors[series];
                segment.image.enabled = true;
                PlaceSegment(segment, x1, y1, x2, y2);
            }
        }
    }

    private void PlaceSegment(TreeNodeRender node, float x1, float y1, float x2, float y2)
    {
        float deltaX = x2 - x1;
        float deltaY = y1 - y2;
        float length = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        node.rectTransform.anchoredPosition = new Vector2((x1 + x2) / 2f, -(y1 + y2) / 2f);
        node.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, length + 1f), 1f);
        node.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg);
    }

    private TreeNodeRender CreateBlock(Color color, TreeNodeRender parent = null)
    {
        TreeNodeRender node = TreeNodeRender.Instantiate().SetParent(parent ?? handler);
        MachineryStyle.SetFlat(node, color);
        node.image.raycastTarget = false;
        return node;
    }

    private void AddLabel(string text, float x, float y, int width, int height)
    {
        var label = new MachineryText(text, 5);
        label.ResizePixels(width, height);
        label.handler.SetParent(handler);
        MachineryStyle.Place(label.handler, Mathf.RoundToInt(x), Mathf.RoundToInt(y), label.widthPixels, label.heightPixels);
        labels.Add(label);
    }

    private void CreateLegend(int index)
    {
        int x = index % 2 == 0 ? 6 : 116;
        int y = 99 + index / 2 * 11;
        legendMarkers[index] = CreateBlock(SeriesColors[index]);
        MachineryStyle.Place(legendMarkers[index], x, y + 3, 8, 2);
        legendLabels[index] = new MachineryText("", 5);
        legendLabels[index].ResizePixels(98, 9);
        legendLabels[index].handler.SetParent(handler);
        MachineryStyle.Place(legendLabels[index].handler, x + 11, y, legendLabels[index].widthPixels, legendLabels[index].heightPixels);
    }

    private void UpdateLegend(int index, int purity, bool unavailable, bool isMachineMaximum)
    {
        if (index >= legendLabels.Length)
        {
            return;
        }
        string title = isMachineMaximum ? "Machine max" : WaterFeatureHelper.GetWaterDisplayName(purity);
        string value = (purity / 100.0).ToString("0.##") + "%";
        legendLabels[index].SetText(title + " " + value + (unavailable ? " (unavailable)" : ""));
    }

    private void SetLabelText(int index, string text)
    {
        labels[index].SetText(text);
    }

    private static string FormatLiters(double liters)
    {
        return liters.ToString(liters < 1.0 ? "0.###" : "0.##");
    }

    public void ResizePixels(int width, int height)
    {
    }

    public bool Attach(PixelElement element) => false;
    public bool Detach(PixelElement element) => false;
    public int WidthPixelsClamp(int width) => Width;
    public int HeightPixelsClamp(int height) => Height;
    public int XClamp(int x) => 0;
    public int YClamp(int y) => 0;
}

internal sealed class PurifierCapacityGraphWatcher : MonoBehaviour
{
    private PurifierCapacityGraph graph;

    internal void Initialize(PurifierCapacityGraph graph)
    {
        this.graph = graph;
    }

    private void Update()
    {
        graph?.RefreshIfNeeded();
    }
}