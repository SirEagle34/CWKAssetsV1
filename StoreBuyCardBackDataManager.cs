using System.IO;

public class StoreBuyCardBackDataManager
    : DataManager<StoreBuyCardBackData>
{
    private static StoreBuyCardBackDataManager _instance;

    public static StoreBuyCardBackDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                string path =
                    Path.Combine(
                        SQSettings.CDN_URL,
                        "Blueprints",
                        "db_StoreBuyCardBacks.json"
                    );

                _instance =
                    new StoreBuyCardBackDataManager(path);
            }

            return _instance;
        }
    }

    public StoreBuyCardBackDataManager(string path)
    {
        base.FilePath = path;

        AddDependency(
            CardBackDataManager.Instance
        );
    }
}