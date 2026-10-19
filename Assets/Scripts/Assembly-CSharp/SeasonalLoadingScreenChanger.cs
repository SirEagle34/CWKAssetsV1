using UnityEngine;
using System;

public class SeasonalLoadingScreenChanger : MonoBehaviour
{
    public UITexture loadingScreenTexture; // przypisz w inspektorze referencję do UITexture w loading screen

    // Zakres zimowych miesięcy
    private readonly int[] winterMonths = { 12, 1, 2 };

    private DateTime halloweenStart;
    private DateTime halloweenEnd;

    private void Awake()
    {
        if (loadingScreenTexture == null)
        {
            Debug.LogError("Brak referencji do UITexture loading screen!");
            return;
        }

        DateTime today = DateTime.Now;
        halloweenStart = new DateTime(today.Year, 10, 20);
        halloweenEnd = new DateTime(today.Year, 11, 5);

        if (IsDateInRange(today, halloweenStart, halloweenEnd))
        {
            SetSeasonTexture("HALLOWEEN");
        }
        else if (Array.Exists(winterMonths, month => month == today.Month))
        {
            SetSeasonTexture("WINTER");
        }
        else
        {
            Debug.Log("Brak specjalnej tekstury sezonowej dla dzisiejszej daty.");
        }
    }

    private bool IsDateInRange(DateTime date, DateTime start, DateTime end)
    {
        return date >= start && date <= end;
    }

    private void SetSeasonTexture(string resourceName)
    {
        // Ładujemy teksturę z Resources (plik powinien być typu Texture, np .png w folderze Resources)
        Texture tex = Resources.Load<Texture>(resourceName);
        if (tex != null)
        {
            loadingScreenTexture.mainTexture = tex;
            Debug.Log($"Załadowano teksturę sezonową: {resourceName}");
        }
        else
        {
            Debug.LogWarning($"Nie znaleziono tekstury '{resourceName}' w Resources!");
        }
    }
}
