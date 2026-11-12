internal sealed class PurifierPlotController
{
    private readonly IPurifierMachineStateProvider machineStateProvider;
    private readonly PurifierCreatePlotInteractor createPlotInteractor;
    private readonly PurifierCreatePlotDetailInteractor createPlotDetailInteractor;

    private PurifierMachineState lastState;
    private bool hasObservedState;

    public PurifierPlotController(
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
        if (!hasObservedState || !state.Equals(lastState))
        {
            lastState = state;
            hasObservedState = true;
            createPlotInteractor.TryRefresh(state);
            UpdateHoverDetail(null);
        }
    }

    public void UpdateHoverDetail(int? mouseoverPurity)
    {
        createPlotDetailInteractor.TryCreate(lastState, mouseoverPurity);
    }

    public void OnPointerPlotExit()
    {
        UpdateHoverDetail(null);
    }
}
