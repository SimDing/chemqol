public class ModEntry : IMod
{
    internal static ModLog Logger { get; private set; }

    public void Init(ModManifest manifest)
    {
        Logger = new ModLog(manifest);
        ChemQOLConfig.Initialize(manifest.ID);
        Logger.Log("Initialized.");
    }

    public void OnEnable()
    {
        Logger.Log("Enabled.");
    }

    public void OnDisable()
    {
        Logger.Log("Disabled.");
    }
}