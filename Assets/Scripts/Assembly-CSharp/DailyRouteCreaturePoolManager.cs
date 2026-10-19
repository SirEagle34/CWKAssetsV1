using System.Collections.Generic;
using UnityEngine;

public static class DailyRouteCreaturePoolManager
{
    private static Dictionary<string, List<string>> pools =
        new Dictionary<string, List<string>>()
    {
        {
            "Rare",
            new List<string>()
            {
                "MaiseWalker_Awaken",
                "CornWall_Awaken",
                "DrDeath_Awaken",
                "CottonCutie_Awaken",
                "BeachMum_Awaken",
                "ArcherDan_Awaken",
                "HuskerGiant_Awaken",
                "ethanallfire_Awaken",
                "HuskerKnight_Awaken",
                "SandEyeBat_Awaken",
                "SandJackal_Awaken",
                "Shepard_Awaken",
                "KnightOfObesity_Awaken",
                "PoppaCorn_Awaken_01",
                "GrapeSlimey_Awaken",
                "SoftRock_Awaken",
                "Ra_Awaken",
                "PyramidTheKid_Awaken",
                "HuskerTanker_Awaken",
                "DonkeyKhan_Awaken",
                "EnlightenedBaldMan_Awaken",
                "SandJackal_Awaken",
                "Phyllis_Awaken",
                "SandEyeBat_Awaken",
                "Sandwitch_Awaken",
                "TheDrooler_Awaken",
                "PeteMoss_Awaken",
                "Cow_Awaken",
                "Shepard_Awaken",
                "AngelEye_Awaken",
                "IcyWall_Awaken",
                "DullCoolDog_Awaken",
                "SnowSludger_Awaken",
                "IcePaladin_Awaken",
                "SnowyMcSnow_Awaken",
                "RobotScarab_Awaken",
                "MouthBall_Awaken",
                "BlackPaladin_Awaken",
                "YellowMermaid_Awaken",
                "HotEyeBat_Awaken",
                "DevilEye_Awaken",
                "MagmaDog_Awaken",
                "SnowDog_Awaken",
                "WinterCow_Awaken",
                "FuriousChick_Awaken",
                "PajamaNinja_Awaken",
                "GhostDeputyHotDog_Special",
                "MadameSeota_Awaken",
                "GhostPig_Special"
            }
        },

        {
            "Algebraic",
            new List<string>()
            {
                "Stalker_Awaken",
                "LostGolem_Awaken",
                "FieldReaper_Awaken",
                "AngelOfVanilla_Awaken",
                "DiamondDan_Awaken",
                "SgtSandwich_Base",
                "SteakChop_Awaken",
                "Pieclops_Awaken",
                "Cornataur_Awaken",
                "Feedman_Awaken",
                "ManWitch_Awaken",
                "Pickler_Awaken",
                "ElfMarauder_Awaken",
                "FarmerTom_Awaken",
                "PunkCat_Awaken",
                "PirateBear_Awaken",
                "DragonFoot_Awaken",
                "FuriousRooster_Awaken",
                "TeethLeaf_Awaken",
                "FuriousHen_Awaken",
                "BogBanSheAngel_Awaken",
                "BespectacledOgre_Awaken",
                "SunKing_Base",
                "SandShark_Awaken",
                "Shark_Awaken",
                "StoneGolem_Awaken",
                "ChadBear_Awaken",
                "BlackFisherFish_Awaken",
                "FlameingoWarrior_Awaken",
                "MagmaWall_Awaken",
                "HotHead_Awaken",
                "AngelOfSnow_Awaken",
                "CornasaurusRex_Awaken",
                "GhostHag_Awaken",
                "GhostStalker_Awaken",
                "ZombiePieclops_Awaken",
                "WallOfSlime_Awaken",
                "PlainsBarfer_Awaken",
                "BotanicGolem_Awaken",
                "Sackgeist_Special",
                "ChestBurster_Base"
            }
        },

        {
            "Obsidian",
            new List<string>()
            {
                "CedarCrystalDan_Obsidian",
                "SakuraRonin_Obsidian",
                "CottonCutie_Awaken",
                "PoppaCorn_Awaken",
                "BurnedFfantry_Awaken",
                "CameraDude_Awaken",
                "GreenPartyOgre_Awaken",
                "CornGolem_Awaken",
                "BaldmansThrone_Base",
                "WallOfHoney_Obsidian"
            }
        }
    };

    public static string PickCreature(string poolName)
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