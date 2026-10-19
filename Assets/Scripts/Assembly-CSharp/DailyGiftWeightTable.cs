using System.Collections.Generic;
using UnityEngine;

internal class DailyGiftWeightTable
{
    public int totalWeight;
    public List<DailyGiftWeightPair> weights = new List<DailyGiftWeightPair>();

    public void Add(int id, int weight)
    {
        totalWeight += weight;
        weights.Add(new DailyGiftWeightPair(id, totalWeight));
    }

    public int Pick()
    {
        int rnum = Random.Range(1, totalWeight + 1);

        foreach (var p in weights)
        {
            if (rnum <= p.weight)
                return p.id;
        }

        return weights[weights.Count - 1].id;
    }
}