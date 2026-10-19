using System.Collections.Generic;

[System.Serializable]
public class MonthlyStoreResponse
{
    public bool success;
    public string month;
    public string server_time;

    public List<string> evo_materials;
    public List<string> evo_special;

    public List<string> action_cards;
    public List<string> action_card_special;
}