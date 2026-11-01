public class ModEntry : IMod
{
    private ModLog log;

    public void Init(ModManifest manifest)
    {
        log = new ModLog(manifest);
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