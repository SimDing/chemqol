using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[HarmonyPatch(typeof(MachinePurifier), nameof(MachinePurifier.WaterPurifier))]
internal static class PurifierPlotPatch
{
    private static void Postfix(GameItem __result)
    {
        if (__result?.contentWindow?.childElement is MachineryPanelRow row)
        {
            var plotOutput = new PurifierPlotPresenter();
            var detailOutput = new PurifierPlotDetailPresenter();
            var plotInteractor = new PurifierCreatePlotInteractor(
                new MachinePurifierMathAdapter(),
                plotOutput);
            var detailInteractor = new PurifierCreatePlotDetailInteractor(
                new MachinePurifierMathAdapter(),
                detailOutput);
            var controller = new PurifierPlotController(
                new GamePurifierMachineStateProvider(__result),
                plotInteractor,
                detailInteractor);
            var plot = new PurifierPlot(controller, plotOutput, detailOutput);
            row.Add(new MachineryPanel("Purification capacity", plot, "purifier-capacity"));
        }
    }
}

internal sealed class PurifierPlot : PixelElement
{
    private const int Width = 224;
    private const int Height = 126;
    private const int PlotLeft = 31;
    private const int PlotTop = 10;
    private const int PlotWidth = 184;
    private const int PlotHeight = 82;
    private const int TooltipWidth = 56;
    private const int TooltipLineHeight = 7;
    private const int TooltipPadding = 2;
    private const int SeriesCount = 6;

    private readonly PurifierPlotController controller;
    private readonly TreeNodeRender plotViewport;
    private readonly List<TreeNodeRender> lineSegments = new List<TreeNodeRender>();
    private readonly TreeNodeRender yAxis;
    private readonly TreeNodeRender topAxis;
    private readonly TreeNodeRender rightAxis;
    private readonly TreeNodeRender bottomAxis;
    private readonly List<MachineryText> xGridLabels = new List<MachineryText>();
    private readonly List<MachineryText> yGridLabels = new List<MachineryText>();
    private readonly List<TreeNodeRender> yGridLines = new List<TreeNodeRender>();
    private readonly TreeNodeRender[] legendMarkers = new TreeNodeRender[SeriesCount];
    private readonly MachineryText[] legendPurityLabels = new MachineryText[SeriesCount];
    private MachineryText yAxisTitle;
    private MachineryText xAxisTitle;
    private readonly TreeNodeRender hoverSurface;
    private readonly TreeNodeRender hoverLine;
    private readonly TreeNodeRender tooltipBackground;
    private readonly MachineryText tooltipText;
    private readonly Vector3[] cornerBuffer = new Vector3[4];
    private PurifierPlotViewModel currentViewModel;

    public bool canResize => false;
    public int widthPixels => Width;
    public int heightPixels => Height;
    public TreeNodeRender handler { get; private set; }
    public RectTransform rectTransform => handler.rectTransform;
    public TreeNodeRender inner => handler;

    public PurifierPlot(
        PurifierPlotController controller,
        IPurifierPlotOutput plotOutput,
        IPurifierPlotDetailOutput detailOutput)
    {
        this.controller = controller;
        plotOutput.ViewModelChanged += HandleViewModelChanged;
        detailOutput.DetailChanged += HandleDetailChanged;
        handler = TreeNodeRender.Instantiate(this, "PurifierPlot");
        plotViewport = TreeNodeRender.Instantiate().SetParent(handler);
        plotViewport.image.enabled = false;
        plotViewport.rectTransform.anchoredPosition = new Vector2(PlotLeft, -PlotTop);
        plotViewport.rectTransform.sizeDelta = new Vector2(PlotWidth + 1, PlotHeight + 1);
        RectMask2D plotMask = plotViewport.mask ?? plotViewport.gameObject.AddComponent<RectMask2D>();
        plotMask.enabled = true;

        hoverSurface = CreateBlock(new Color(0f, 0f, 0f, 0f), plotViewport);
        hoverSurface.rectTransform.anchorMin = Vector2.zero;
        hoverSurface.rectTransform.anchorMax = Vector2.one;
        hoverSurface.rectTransform.offsetMin = Vector2.zero;
        hoverSurface.rectTransform.offsetMax = Vector2.zero;
        hoverSurface.image.raycastTarget = true;
        hoverSurface.gameObject.AddComponent<PurifierPlotHover>().Initialize(this);

        yAxis = CreateBlock(MachineryStyle.Hex("77776f"));
        bottomAxis = CreateBlock(MachineryStyle.Hex("77776f"));
        topAxis = CreateBlock(MachineryStyle.Hex("77776f", 0.35f));
        rightAxis = CreateBlock(MachineryStyle.Hex("77776f", 0.35f));

        hoverLine = CreateBlock(MachineryStyle.Hex("77776f", 0.35f));
        hoverLine.image.enabled = false;

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

        handler.gameObject.AddComponent<PurifierPlotWatcher>().Initialize(this);
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

    public void HandleViewModelChanged(PurifierPlotViewModel viewModel)
    {
        currentViewModel = viewModel;
        Draw(viewModel);
    }

    private void Draw(PurifierPlotViewModel viewModel)
    {
        UpdateAxisTitle(ref yAxisTitle, viewModel.YAxisTitle, 0, 0, PlotLeft - 2, 9);
        UpdateAxisTitle(ref xAxisTitle, viewModel.XAxisTitle, PlotLeft + 29, PlotTop + PlotHeight + 8, 118, 9);
        int[][] xValues = viewModel.XValues;
        long[][] yValues = viewModel.YValues;
        int minimumPurity = viewModel.MinimumPurity;
        int maximumPurity = viewModel.MaximumPurity;
        UpdateXAxisLabels(viewModel.XAxisTicks, minimumPurity, maximumPurity);
        for (int i = 0; i < legendMarkers.Length; i++)
        {
            PurifierLegendItemViewModel legendItem = viewModel.LegendItems[i];
            legendMarkers[i].gameObject.SetActive(legendItem.IsVisible);
            legendPurityLabels[i].handler.gameObject.SetActive(legendItem.IsVisible);
            if (legendItem.IsVisible)
            {
                legendMarkers[i].image.color = MachineryStyle.Hex(viewModel.SeriesColors[legendItem.SeriesIndex]);
                legendPurityLabels[i].SetText(legendItem.Label);
            }
        }

        long minimumMicroliters = viewModel.MinimumMicroliters;
        long maximumMicroliters = viewModel.MaximumMicroliters;
        double logYRange = Math.Log((double)maximumMicroliters / minimumMicroliters);

        MachineryStyle.Place(yAxis, PlotLeft, PlotTop, 1, PlotHeight + 1);
        MachineryStyle.Place(rightAxis, PlotLeft + PlotWidth, PlotTop, 1, PlotHeight + 1);
        MachineryStyle.Place(topAxis, PlotLeft, PlotTop, PlotWidth + 1, 1);
        MachineryStyle.Place(bottomAxis, PlotLeft, PlotTop + PlotHeight, PlotWidth + 1, 1);
        EnsureLabelCount(yGridLabels, viewModel.GridLevels.Length, 0, 0, PlotLeft - 3, 8);
        EnsureGridLineCount(yGridLines, viewModel.GridLevels.Length);
        for (int i = 0; i < viewModel.GridLevels.Length; i++)
        {
            PurifierGridLevelViewModel gridLevel = viewModel.GridLevels[i];
            MachineryText label = yGridLabels[i];
            label.handler.gameObject.SetActive(gridLevel.IsLabelVisible);
            label.SetText(gridLevel.Label);
            int gridY = PlotHeight - Mathf.RoundToInt((float)(Math.Log((double)gridLevel.Microliters / minimumMicroliters) / logYRange * PlotHeight));
            MachineryStyle.Place(label.handler, 0, PlotTop + gridY - 4, label.widthPixels, label.heightPixels);
            TreeNodeRender gridLine = yGridLines[i];
            gridLine.image.enabled = gridLevel.IsGridLineVisible;
            MachineryStyle.Place(gridLine, PlotLeft, PlotTop + gridY, PlotWidth + 1, 1);
        }

        EnsureLineSegmentCount(viewModel);
        int segmentIndex = 0;
        for (int series = 0; series < xValues.Length; series++)
        {
            for (int point = 0; point < xValues[series].Length - 1; point++)
            {
                TreeNodeRender segment = lineSegments[segmentIndex++];
                float xRange = Math.Max(1, maximumPurity - minimumPurity);
                float x1 = (float)PlotWidth * (xValues[series][point] - minimumPurity) / xRange;
                float x2 = (float)PlotWidth * (xValues[series][point + 1] - minimumPurity) / xRange;
                float y1 = PlotHeight - (float)(Math.Log((double)Math.Max(minimumMicroliters, yValues[series][point]) / minimumMicroliters) / logYRange * PlotHeight);
                float y2 = PlotHeight - (float)(Math.Log((double)Math.Max(minimumMicroliters, yValues[series][point + 1]) / minimumMicroliters) / logYRange * PlotHeight);
                segment.image.color = MachineryStyle.Hex(viewModel.SeriesColors[series]);
                segment.image.enabled = true;
                PlaceSegment(segment, x1, y1, x2, y2);
            }
        }
    }

    private void EnsureLineSegmentCount(PurifierPlotViewModel viewModel)
    {
        int lineSegmentCount = CountNecessaryLineSegments(viewModel);
        EnsureBlockCount(lineSegments, lineSegmentCount, () => {
            var block = CreateBlock(Color.white, plotViewport);
            block.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return block;
        });
    }

    private int CountNecessaryLineSegments(PurifierPlotViewModel viewModel)
    {
        int count = 0;
        foreach (int[] series in viewModel.XValues)
        {
            count += series.Length - 1;
        }
        return count;
    }

    private void UpdateXAxisLabels(PurifierXAxisTickViewModel[] ticks, int minimumPurity, int maximumPurity)
    {
        EnsureLabelCount(xGridLabels, ticks.Length, 0, PlotTop + PlotHeight + 1, 20, 8);
        for (int i = 0; i < ticks.Length; i++)
        {
            PurifierXAxisTickViewModel tick = ticks[i];
            xGridLabels[i].handler.gameObject.SetActive(tick.IsVisible);
            if (!tick.IsVisible)
            {
                continue;
            }

            int purity = tick.Purity;
            float fraction = (float)(purity - minimumPurity) / (maximumPurity - minimumPurity);
            int x = PlotLeft + Mathf.RoundToInt(fraction * PlotWidth) - xGridLabels[i].widthPixels / 2;
            MachineryStyle.Place(xGridLabels[i].handler, x, PlotTop + PlotHeight + 1, xGridLabels[i].widthPixels, xGridLabels[i].heightPixels);
            xGridLabels[i].SetText(tick.Label);
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
        int minimumPurity = currentViewModel.MinimumPurity;
        int maximumPurity = currentViewModel.MaximumPurity;
        int mouseoverPurity = minimumPurity + Mathf.RoundToInt(fraction * (maximumPurity - minimumPurity));
        controller.UpdateHoverDetail(mouseoverPurity);
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

        int minPurity = currentViewModel.MinimumPurity;
        int maxPurity = currentViewModel.MaximumPurity;
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
        node.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, length), 1f);
        node.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg);
    }

    private TreeNodeRender CreateBlock(Color color, TreeNodeRender parent = null)
    {
        TreeNodeRender node = TreeNodeRender.Instantiate().SetParent(parent ?? handler);
        MachineryStyle.SetFlat(node, color);
        node.image.raycastTarget = false;
        return node;
    }

    private MachineryText CreateLabel(string text, float x, float y, int width, int height)
    {
        var label = new MachineryText(text, 5);
        label.ResizePixels(width, height);
        label.handler.SetParent(handler);
        MachineryStyle.Place(label.handler, Mathf.RoundToInt(x), Mathf.RoundToInt(y), label.widthPixels, label.heightPixels);
        return label;
    }

    private void UpdateAxisTitle(ref MachineryText label, string text, float x, float y, int width, int height)
    {
        if (label == null)
        {
            label = CreateLabel(text, x, y, width, height);
            return;
        }

        label.SetText(text);
    }

    private void EnsureLabelCount(List<MachineryText> labels, int count, float x, float y, int width, int height)
    {
        while (labels.Count < count)
        {
            labels.Add(CreateLabel("", x, y, width, height));
        }

        for (int i = count; i < labels.Count; i++)
        {
            labels[i].handler.gameObject.SetActive(false);
        }
    }

    private void EnsureBlockCount(List<TreeNodeRender> blocks, int count, Func<TreeNodeRender> ctor)
    {
        while (blocks.Count < count)
        {
            blocks.Add(ctor());
        }

        for (int i = count; i < blocks.Count; i++)
        {
            blocks[i].image.enabled = false;
        }
    }

    private void EnsureGridLineCount(List<TreeNodeRender> gridLines, int count)
    {
        EnsureBlockCount(gridLines, count, () => CreateBlock(MachineryStyle.Hex("77776f", 0.35f)));
    }

    private void CreateLegend(int index)
    {
        int x = 5 + index * 43;
        int y = 110;
        legendMarkers[index] = CreateBlock(Color.white);
        MachineryStyle.Place(legendMarkers[index], x, y + 3, 7, 2);
        legendPurityLabels[index] = new MachineryText("", 5);
        legendPurityLabels[index].ResizePixels(34, 8);
        legendPurityLabels[index].handler.SetParent(handler);
        legendPurityLabels[index].handler.image.raycastTarget = false;
        MachineryStyle.Place(legendPurityLabels[index].handler, x + 9, y, legendPurityLabels[index].widthPixels, legendPurityLabels[index].heightPixels);
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

internal sealed class PurifierPlotWatcher : MonoBehaviour
{
    private PurifierPlot plot;

    internal void Initialize(PurifierPlot plot)
    {
        this.plot = plot;
    }

    private void Update()
    {
        plot?.RefreshIfNeeded();
    }
}

internal sealed class PurifierPlotHover : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    private PurifierPlot plot;

    internal void Initialize(PurifierPlot plot)
    {
        this.plot = plot;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        plot?.UpdateHover(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        plot?.UpdateHover(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        plot?.OnPointerPlotExit();
    }
}