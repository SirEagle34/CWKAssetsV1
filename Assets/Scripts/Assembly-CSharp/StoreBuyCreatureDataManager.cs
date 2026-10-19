using System.IO;

public class StoreBuyCreatureDataManager
    : DataManager<StoreBuyCreatureData>
{
    private static StoreBuyCreatureDataManager _instance;

    public static StoreBuyCreatureDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                string path =
                    Path.Combine(
                        SQSettings.CDN_URL,
                        "Blueprints",
                        "db_StoreBuyCreatures.json"
                    );

                _instance =
                    new StoreBuyCreatureDataManager(path);
            }

            return _instance;
        }
    }

    public StoreBuyCreatureDataManager(string path)
    {
        base.FilePath = path;

        AddDependency(
            CreatureDataManager.Instance
        );
    }
}