public class ModEntry : IMod
{
    private ModLog log;

    public void Init(ModManifest manifest)
    {
        log = new ModLog(manifest);
        ChemQOLConfig.Initialize(manifest.ID);
        log.Log("ChemQOL initialized.");
    }

    public void OnEnable()
    {
        log.Log("ChemQOL enabled.");
    }

    public void OnDisable()
    {
        log.Log("ChemQOL disabled.");
    }
}