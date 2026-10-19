using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StoreBuyActionCardDataManager
    : DataManager<StoreBuyActionCardData>
{
    private static StoreBuyActionCardDataManager _instance;

    public static StoreBuyActionCardDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                string path =
                    Path.Combine(
                        SQSettings.CDN_URL,
                        "Blueprints",
                        "db_StoreBuyActionCards.json"
                    );

                _instance =
                    new StoreBuyActionCardDataManager(path);
            }

            return _instance;
        }
    }

    public StoreBuyActionCardDataManager(string path)
    {
        base.FilePath = path;

        AddDependency(
            CardDataManager.Instance
        );
    }

    /// <summary>
    /// Server tarafından aktif olarak seçilen monthly
    /// ve blueprint'te IsSpecial=true olan special
    /// ürünleri blueprint datasından bulur.
    /// </summary>
    public List<StoreBuyActionCardData> GetActiveMonthlyOffers(
        List<string> activeServerIds,
        List<string> specialIds
    )
    {
        List<StoreBuyActionCardData> activeOffers =
            new List<StoreBuyActionCardData>();

        HashSet<string> addedIds =
            new HashSet<string>();

        // -------------------------------------------------
        // MONTHLY - app.py tarafından seçilen 4 ürün
        // -------------------------------------------------

        if (activeServerIds != null)
        {
            foreach (string offerId in activeServerIds)
            {
                if (string.IsNullOrEmpty(offerId))
                    continue;

                StoreBuyActionCardData data =
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
                    Debug.LogError(
                        "[Store] Action Card blueprint data " +
                        "not found! ID: " +
                        offerId
                    );
                }
            }
        }

        // -------------------------------------------------
        // SPECIAL - blueprint'te sabit olan ürün
        // -------------------------------------------------

        if (specialIds != null)
        {
            foreach (string specialId in specialIds)
            {
                if (string.IsNullOrEmpty(specialId))
                    continue;

                StoreBuyActionCardData data =
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
                    Debug.LogError(
                        "[Store] Special Action Card blueprint " +
                        "data not found! ID: " +
                        specialId
                    );
                }
            }
        }

        return activeOffers;
    }
}
