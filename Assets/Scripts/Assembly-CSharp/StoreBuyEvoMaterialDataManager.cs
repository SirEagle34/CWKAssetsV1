using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StoreBuyEvoMaterialDataManager
    : DataManager<StoreBuyEvoMaterialData>
{
    private static StoreBuyEvoMaterialDataManager _instance;

    public static StoreBuyEvoMaterialDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                string path =
                    Path.Combine(
                        SQSettings.CDN_URL,
                        "Blueprints",
                        "db_StoreBuyEvoMaterials.json"
                    );

                _instance =
                    new StoreBuyEvoMaterialDataManager(path);
            }

            return _instance;
        }
    }

    public StoreBuyEvoMaterialDataManager(string path)
    {
        base.FilePath = path;

        AddDependency(
            EvoMaterialDataManager.Instance
        );
    }

    public List<StoreBuyEvoMaterialData> GetActiveMonthlyOffers(
        List<string> activeServerIds
    )
    {
        List<StoreBuyEvoMaterialData> activeOffers =
            new List<StoreBuyEvoMaterialData>();

        if (activeServerIds == null)
            return activeOffers;

        foreach (string offerId in activeServerIds)
        {
            if (string.IsNullOrEmpty(offerId))
                continue;

            StoreBuyEvoMaterialData data =
                GetData(offerId);

            if (data != null)
            {
                activeOffers.Add(data);
            }
            else
            {
                Debug.LogError(
                    "[Store] Evo Material blueprint " +
                    "verisi bulunamadı! ID: " +
                    offerId
                );
            }
        }

        return activeOffers;
    }
    
    public List<StoreBuyEvoMaterialData> GetActiveMonthlyOffers(
        List<string> activeServerIds,
        List<string> specialIds
    )
    {
        List<StoreBuyEvoMaterialData> activeOffers =
            new List<StoreBuyEvoMaterialData>();

        HashSet<string> addedIds =
            new HashSet<string>();

        // Monthly - app.py'nin seçtiği 4
        if (activeServerIds != null)
        {
            foreach (string offerId in activeServerIds)
            {
                if (string.IsNullOrEmpty(offerId))
                    continue;

                StoreBuyEvoMaterialData data =
                    GetData(offerId);

                if (data != null)
                {
                    if (addedIds.Add(data.ID))
                    {
                        activeOffers.Add(data);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError(
                        "[Store] Evo Material blueprint data " +
                        "not found! ID: " +
                        offerId
                    );
                }
            }
        }

        // Special - blueprint'te sabit
        if (specialIds != null)
        {
            foreach (string specialId in specialIds)
            {
                if (string.IsNullOrEmpty(specialId))
                    continue;

                StoreBuyEvoMaterialData data =
                    GetData(specialId);

                if (data != null)
                {
                    if (addedIds.Add(data.ID))
                    {
                        activeOffers.Add(data);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError(
                        "[Store] Special Evo Material blueprint " +
                        "data not found! ID: " +
                        specialId
                    );
                }
            }
        }

        return activeOffers;
    }
}