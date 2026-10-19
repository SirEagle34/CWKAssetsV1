using System.Collections.Generic;
using UnityEngine;

public static class DailyRouteSpellPoolManager
{
    private static Dictionary<string, List<string>> pools =
        new Dictionary<string, List<string>>()
    {
        {
            "Cool_Spell",
            new List<string>()
            {
                "NICE_0001",
                "NICE_0018",
                "NICE_0019",
                "NICE_0027",
                "NICE_0028",
                "NICE_0029",
                "NICE_0030",
                "CORN_0002",
                "CORN_0004",
                "CORN_0006",
                "CORN_0008",
                "CORN_0011",
                "CORN_0013",
                "CORN_0017",
                "SAND_0001",
                "SAND_0002",
                "SAND_0003",
                "SAND_0015",
                "SAND_0016",
                "SAND_0017",
                "PLAINS_0003",
                "PLAINS_0004",
                "PLAINS_0008",
                "PLAINS_0013",
                "PLAINS_0026",
                "SWAMP_0001",
                "SWAMP_0002",
                "SWAMP_0005",
                "SWAMP_0009",
                "SWAMP_0031"
            }
        },
        {
            "Rare_Spell",
            new List<string>()
            {
                "CORN_0009",
                "CORN_0027",
                "CORN_0013",
                "CORN_0020",
                "PLAINS_0002",
                "PLAINS_0005",
                "PLAINS_0028",
                "PLAINS_0010",
                "SAND_0013",
                "SAND_0017",
                "SAND_0026",
                "SAND_0027",
                "NICE_0009",
                "NICE_0010",
                "NICE_0007",
                "NICE_0020",
                "NICE_0024",
                "SWAMP_0003",
                "SWAMP_0011",
                "SWAMP_0010",
                "SWAMP_0008",
                "SWAMP_0012",
                "SWAMP_0039",
                "CORN_0031",
                "NICE_0025",
                "CORN_0037",
                "SAND_0014",
                "SAND_0030",
                "SWAMP_0027",
                "NICE_0035",
            }
        },

        {
            "Algebraic_Spell",
            new List<string>()
            {
                "SAND_0007",
                "PLAINS_0007",
                "PLAINS_0009",
                "CORN_0011",
                "SAND_0028",
                "NICE_0039",
                "NICE_0016",
                "CORN_0018",
                "SWAMP_0014",
                "SWAMP_0016",
                "SAND_0025",
                "CORN_0044",
                "PLAINS_0001",
                "NICE_0001",
                "SAND_0006",
                "PLAINS_0008",
                "SWAMP_0033",
                "NICE_0013",
                "NICE_0021",
                "NICE_0012",
                "NICE_0032",
                "NICE_0017",
                "NICE_0036",
                "SAND_0020",
                "PLAINS_0035",
                "SWAMP_0036",
                "CORN_0033"
            }
        }
    };

    public static string PickSpell(string poolName)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError(
                "Pool not found: " + poolName);

            return null;
        }

        List<string> list = pools[poolName];

        if (list.Count == 0)
            return null;

        int rand =
            Random.Range(0, list.Count);

        return list[rand];
    }
}