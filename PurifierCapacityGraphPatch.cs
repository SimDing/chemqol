using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[HarmonyPatch(typeof(MachinePurifier), nameof(MachinePurifier.WaterPurifier))]
internal static class PurifierCapacityGraphPatch
{
    private static void Postfix(GameItem __result)
    {
        if (__result?.contentWindow?.childElement is MachineryPanelRow row)
        {
            var plotOutput = new PurifierCapacityGraphPresenter();
            var detailOutput = new PurifierDetailPresenter(plotOutput);
            var plotInteractor = new PurifierCreatePlotInteractor(
                new MachinePurifierMathAdapter(),
                plotOutput);
            var detailInteractor = new PurifierCreatePlotDetailInteractor(detailOutput);
            var controller = new PurifierCapacityGraphController(
                new GamePurifierMachineStateProvider(__result),
                plotInteractor,
                detailInteractor);
            var graph = new PurifierCapacityGraph(controller, plotOutput, detailOutput);
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
    private const int PlotHeight = 82;
    private const int PointCount = PlotWidth + 1;
    private const int TooltipWidth = 56;
    private const int TooltipLineHeight = 7;
    private const int TooltipPadding = 2;
    private const int SeriesCount = 6;
    private static readonly Color[] SeriesColors =
    {
        MachineryStyle.Hex("3d823f"),
        MachineryStyle.Hex("7ab053"),
        MachineryStyle.Hex("ccb987"),
        MachineryStyle.Hex("ba8953"),
        MachineryStyle.Hex("b26b4e"),
        MachineryStyle.Hex("78c9df")
    };

    private readonly PurifierCapacityGraphController controller;
    private readonly TreeNodeRender plotViewport;
    private readonly TreeNodeRender[] lineSegments = new TreeNodeRender[SeriesCount * (PointCount - 1)];
    private readonly TreeNodeRender[] axes = new TreeNodeRender[8];
    private readonly List<MachineryText> labels = new List<MachineryText>();
    private readonly TreeNodeRender[] legendMarkers = new TreeNodeRender[SeriesCount];
    private readonly MachineryText[] legendPurityLabels = new MachineryText[SeriesCount];
    private readonly TreeNodeRender hoverSurface;
    private readonly TreeNodeRender hoverLine;
    private readonly TreeNodeRender tooltipBackground;
    private readonly MachineryText tooltipText;
    private readonly Vector3[] cornerBuffer = new Vector3[4];
    private PurifierGraphViewModel currentViewModel;

    public bool canResize => false;
    public int widthPixels => Width;
    public int heightPixels => Height;
    public TreeNodeRender handler { get; private set; }
    public RectTransform rectTransform => handler.rectTransform;
    public TreeNodeRender inner => handler;

    public PurifierCapacityGraph(
        PurifierCapacityGraphController controller,
        IPurifierPlotOutput plotOutput,
        IPurifierPlotDetailOutput detailOutput)
    {
        this.controller = controller;
        plotOutput.ViewModelChanged += HandleViewModelChanged;
        detailOutput.DetailChanged += HandleDetailChanged;
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

        hoverSurface = CreateBlock(new Color(0f, 0f, 0f, 0f), plotViewport);
        hoverSurface.rectTransform.anchorMin = Vector2.zero;
        hoverSurface.rectTransform.anchorMax = Vector2.one;
        hoverSurface.rectTransform.offsetMin = Vector2.zero;
        hoverSurface.rectTransform.offsetMax = Vector2.zero;
        hoverSurface.image.raycastTarget = true;
        hoverSurface.gameObject.AddComponent<PurifierCapacityGraphHover>().Initialize(this);

        for (int i = 0; i < axes.Length; i++)
        {
            axes[i] = CreateBlock(MachineryStyle.Hex("77776f"));
        }

        hoverLine = CreateBlock(MachineryStyle.Hex("77776f", 0.35f));
        hoverLine.image.enabled = false;

        AddLabel("L/cycle", 0, 0, PlotLeft - 2, 9);
        for (int i = 0; i < 5; i++)
        {
            AddLabel("", 0, PlotTop + PlotHeight + 1, 20, 8);
        }
        AddLabel("Input purity (%)", PlotLeft + 29, PlotTop + PlotHeight + 8, 118, 9);
        AddLabel("3L", 0, 0, PlotLeft - 3, 8);
        AddLabel("2L", 0, 0, PlotLeft - 3, 8);
        AddLabel("1L", 0, 0, PlotLeft - 3, 8);
        AddLabel("750ml", 0, 0, PlotLeft - 3, 8);
        AddLabel("375ml", 0, 0, PlotLeft - 3, 8);
        AddLabel("6L", 0, 0, PlotLeft - 3, 8);
        AddLabel("", 0, 0, PlotLeft - 3, 8);

        for (int i = 0; i < SeriesCount; i++)
        {
            CreateLegend(i);
        }

        tooltipBackground = CreateBlock(MachineryStyle.Hex("252a2d", 0.72f));
        tooltipBackground.image.enabled = false;
        tooltipText = new MachineryText("", 5);
        tooltipText.ResizePixels(TooltipWidth - TooltipPadding * 2, TooltipLineHeight);
        tooltipText.handler.SetParent(handler);
        tooltipText.handler.image.raycastTarget = false;
        tooltipText.handler.gameObject.SetActive(false);

        handler.gameObject.AddComponent<PurifierCapacityGraphWatcher>().Initialize(this);
        Validate();
    }

    public void Validate()
    {
        controller.Refresh();
    }

    internal void RefreshIfNeeded()
    {
        controller.Refresh();
    }

    public void HandleViewModelChanged(PurifierGraphViewModel viewModel)
    {
        currentViewModel = viewModel;
        Draw(viewModel);
    }

    private void Draw(PurifierGraphViewModel viewModel)
    {
        int[] xValues = viewModel.XValues;
        double[][] values = viewModel.YValues;
        int[] purities = viewModel.Purities;
        UpdateXAxisLabels(viewModel.XAxisTicks, xValues[0], xValues[xValues.Length - 1]);
        for (int i = 0; i < legendMarkers.Length; i++)
        {
            PurifierLegendItemViewModel legendItem = viewModel.LegendItems[i];
            legendMarkers[i].gameObject.SetActive(legendItem.IsVisible);
            legendPurityLabels[i].handler.gameObject.SetActive(legendItem.IsVisible);
            if (legendItem.IsVisible)
            {
                legendMarkers[i].image.color = GetSeriesColor(legendItem.Purity);
                legendPurityLabels[i].SetText(legendItem.Label);
            }
        }

        double minimumLiters = viewModel.MinimumLiters;
        double maximumLiters = viewModel.MaximumLiters;
        double logYRange = Math.Log(maximumLiters / minimumLiters);

        MachineryStyle.Place(axes[0], PlotLeft, PlotTop, 1, PlotHeight + 1);
        MachineryStyle.Place(axes[6], PlotLeft, PlotTop, PlotWidth + 1, 1);
        MachineryStyle.Place(axes[7], PlotLeft, PlotTop + PlotHeight, PlotWidth + 1, 1);
        axes[6].image.color = MachineryStyle.Hex("77776f", 0.35f);
        axes[7].image.color = MachineryStyle.Hex("77776f", 0.35f);
        SetLabelText(12, viewModel.MaximumLitersLabel);
        SetLabelText(13, viewModel.MinimumLitersLabel);
        MachineryStyle.Place(labels[12].handler, 0, PlotTop - 4, labels[12].widthPixels, labels[12].heightPixels);
        MachineryStyle.Place(labels[13].handler, 0, PlotTop + PlotHeight - 4, labels[13].widthPixels, labels[13].heightPixels);
        for (int i = 0; i < viewModel.GridLevels.Length; i++)
        {
            PurifierGridLevelViewModel gridLevel = viewModel.GridLevels[i];
            axes[i + 1].image.enabled = gridLevel.IsVisible;
            labels[i + 7].handler.gameObject.SetActive(gridLevel.IsVisible);
            int gridY = PlotHeight - Mathf.RoundToInt((float)(Math.Log(gridLevel.Liters / minimumLiters) / logYRange * PlotHeight));
            MachineryStyle.Place(axes[i + 1], PlotLeft, PlotTop + gridY, PlotWidth + 1, 1);
            axes[i + 1].image.color = MachineryStyle.Hex("77776f", 0.35f);
            MachineryStyle.Place(labels[i + 7].handler, 0, PlotTop + gridY - 4, labels[i + 7].widthPixels, labels[i + 7].heightPixels);
        }

        int segmentIndex = 0;
        for (int series = 0; series < SeriesCount; series++)
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
                segment.image.color = GetSeriesColor(purities[series]);
                segment.image.enabled = true;
                PlaceSegment(segment, x1, y1, x2, y2);
            }
        }
    }

    private void UpdateXAxisLabels(PurifierXAxisTickViewModel[] ticks, int minimumPurity, int maximumPurity)
    {
        for (int i = 0; i < 5; i++)
        {
            PurifierXAxisTickViewModel tick = ticks[i];
            labels[i + 1].handler.gameObject.SetActive(tick.IsVisible);
            if (!tick.IsVisible)
            {
                continue;
            }

            int purity = tick.Purity;
            float fraction = (float)(purity - minimumPurity) / (maximumPurity - minimumPurity);
            int x = PlotLeft + Mathf.RoundToInt(fraction * PlotWidth) - labels[i + 1].widthPixels / 2;
            MachineryStyle.Place(labels[i + 1].handler, x, PlotTop + PlotHeight + 1, labels[i + 1].widthPixels, labels[i + 1].heightPixels);
            labels[i + 1].SetText(tick.Label);
        }
    }

    internal void UpdateHover(PointerEventData eventData)
    {
        if (currentViewModel == null)
        {
            return;
        }

        if (!TryGetNormalizedPointerPosition(plotViewport.rectTransform, eventData, out Vector2 plotPosition))
        {
            return;
        }

        float fraction = Mathf.Clamp01(plotPosition.x);
        controller.UpdateHoverDetail(fraction);
    }

    public void HandleDetailChanged(PurifierPlotDetailViewModel viewModel)
    {
        if (!viewModel.IsVisible)
        {
            tooltipText.handler.gameObject.SetActive(false);
            hoverLine.image.enabled = false;
            tooltipBackground.image.enabled = false;
            return;
        }
        int xValue = viewModel.XValue;
        string[] lines = viewModel.Lines;
        if (currentViewModel == null)
        {
            return;
        }

        int minPurity = currentViewModel.XValues[0];
        int maxPurity = currentViewModel.XValues[currentViewModel.XValues.Length - 1];
        float xFraction = maxPurity == minPurity
            ? 0f
            : (float)(xValue - minPurity) / (maxPurity - minPurity);
        float lineX = PlotWidth * Mathf.Clamp01(xFraction);
        MachineryStyle.Place(hoverLine, PlotLeft + Mathf.RoundToInt(lineX), PlotTop, 1, PlotHeight);
        hoverLine.image.enabled = true;

        tooltipText.SetText(string.Join("\n", lines));
        int tooltipHeight = lines.Length * TooltipLineHeight + TooltipPadding * 2;
        tooltipText.ResizePixels(TooltipWidth - TooltipPadding * 2, tooltipHeight - TooltipPadding * 2);
        tooltipText.handler.gameObject.SetActive(true);
        int textX = Mathf.RoundToInt(PlotLeft + lineX - TooltipWidth / 2f);
        textX = Mathf.Clamp(textX, PlotLeft, PlotLeft + PlotWidth - TooltipWidth);
        MachineryStyle.Place(tooltipBackground, textX, PlotTop, TooltipWidth, tooltipHeight);
        MachineryStyle.Place(tooltipText.handler, textX + TooltipPadding, PlotTop + TooltipPadding, tooltipText.widthPixels, tooltipText.heightPixels);
        tooltipBackground.image.enabled = true;
    }

    private bool TryGetNormalizedPointerPosition(RectTransform target, PointerEventData eventData, out Vector2 normalizedPosition)
    {
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            normalizedPosition = Vector2.zero;
            return false;
        }

        Camera camera = eventData.pressEventCamera;
        if (camera == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera ?? Camera.main;
        }

        target.GetWorldCorners(cornerBuffer);
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < cornerBuffer.Length; i++)
        {
            Vector2 screenPoint;
            if (camera != null)
            {
                Vector3 projected = camera.WorldToScreenPoint(cornerBuffer[i]);
                screenPoint = new Vector2(projected.x, projected.y);
            }
            else
            {
                RectTransform canvasRect = canvas.transform as RectTransform;
                if (canvasRect == null)
                {
                    normalizedPosition = Vector2.zero;
                    return false;
                }

                Vector3 canvasPoint = canvasRect.InverseTransformPoint(cornerBuffer[i]);
                Rect canvasBounds = canvasRect.rect;
                screenPoint = new Vector2(
                    canvas.pixelRect.xMin + (canvasPoint.x - canvasBounds.xMin) * canvas.scaleFactor,
                    canvas.pixelRect.yMin + (canvasPoint.y - canvasBounds.yMin) * canvas.scaleFactor);
            }

            minX = Mathf.Min(minX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxX = Mathf.Max(maxX, screenPoint.x);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        float screenWidth = maxX - minX;
        float screenHeight = maxY - minY;
        if (screenWidth <= 0f || screenHeight <= 0f)
        {
            normalizedPosition = Vector2.zero;
            return false;
        }

        normalizedPosition = new Vector2(
            (eventData.position.x - minX) / screenWidth,
            (eventData.position.y - minY) / screenHeight);
        return true;
    }

    internal void OnPointerPlotExit()
    {
        controller.OnPointerPlotExit();
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
        int x = 5 + index * 43;
        int y = 110;
        legendMarkers[index] = CreateBlock(SeriesColors[index]);
        MachineryStyle.Place(legendMarkers[index], x, y + 3, 7, 2);
        legendPurityLabels[index] = new MachineryText("", 5);
        legendPurityLabels[index].ResizePixels(34, 8);
        legendPurityLabels[index].handler.SetParent(handler);
        legendPurityLabels[index].handler.image.raycastTarget = false;
        MachineryStyle.Place(legendPurityLabels[index].handler, x + 9, y, legendPurityLabels[index].widthPixels, legendPurityLabels[index].heightPixels);
    }

    private static Color GetSeriesColor(int purity)
    {
        switch (purity)
        {
            case 9990: return MachineryStyle.Hex("3d823f");
            case 9900: return MachineryStyle.Hex("7ab053");
            case 9800: return MachineryStyle.Hex("ccb987");
            case 9600: return MachineryStyle.Hex("ba8953");
            case 9000: return MachineryStyle.Hex("b26b4e");
            default: return MachineryStyle.Hex("78c9df");
        }
    }

    private void SetLabelText(int index, string text)
    {
        labels[index].SetText(text);
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

internal sealed class PurifierCapacityGraphHover : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    private PurifierCapacityGraph graph;

    internal void Initialize(PurifierCapacityGraph graph)
    {
        this.graph = graph;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        graph?.UpdateHover(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        graph?.UpdateHover(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        graph?.OnPointerPlotExit();
    }
}