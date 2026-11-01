public class ModEntry : IMod
{
    internal static ModLog Logger { get; private set; }

    internal static void DebugLog(string message)
    {
        if (ChemQOLConfig.DebugLogging)
        {
            Logger?.Log(message);
        }
    }

    public void Init(ModManifest manifest)
    {
        Logger = new ModLog(manifest);
        ChemQOLConfig.Initialize(manifest.ID);
        DebugLog("Initialized.");
    }

    public void OnEnable()
    {
        DebugLog("Enabled.");
    }

    public void OnDisable()
    {
        DebugLog("Disabled.");
    }
}