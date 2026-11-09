internal sealed class PurifierCapacityGraphController
{
    private readonly IPurifierMachineStateProvider machineStateProvider;
    private readonly PurifierCreatePlotInteractor createPlotInteractor;
    private readonly PurifierCreatePlotDetailInteractor createPlotDetailInteractor;

    private PurifierMachineState lastState;

    public PurifierCapacityGraphController(
        IPurifierMachineStateProvider machineStateProvider,
        PurifierCreatePlotInteractor createPlotInteractor,
        PurifierCreatePlotDetailInteractor createPlotDetailInteractor)
    {
        this.machineStateProvider = machineStateProvider;
        this.createPlotInteractor = createPlotInteractor;
        this.createPlotDetailInteractor = createPlotDetailInteractor;
    }

    public void Refresh()
    {
        PurifierMachineState state = machineStateProvider.ReadCurrent();
        if (!state.Equals(lastState))
        {
            lastState = state;
            createPlotInteractor.TryRefresh(state);
        }
    }

    public void UpdateHoverDetail(float normalizedX)
    {
        createPlotDetailInteractor.TryCreate(normalizedX);
    }

    public void OnPointerPlotExit()
    {
        createPlotDetailInteractor.Clear();
    }
}
