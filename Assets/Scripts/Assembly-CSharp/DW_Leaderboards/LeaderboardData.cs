using System; // Convert sınıfını kullanabilmek için şart!
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace DW_Leaderboards
{
    public class LeaderboardData
    {
        public int ranking;
        public string PlayerID;
        public string PlayerName;
        public int MultiplayerLevel;
        public int Points;
        
        // --- NGUI VE APP.PY BAĞLANTISI İÇİN YENİ EKLENEN DEĞİŞKENLER ---
        public string RankName;
        public string SpriteName;

        // Constructor güncellendi
        public LeaderboardData(int position, string id, string name, int point, int level, string rankName, string spriteName)
        {
            ranking = position;
            PlayerID = id;
            PlayerName = name;
            Points = point;
            MultiplayerLevel = level;
            RankName = rankName;
            SpriteName = spriteName;
        }

        public static List<LeaderboardData> ProcessObjectToList(object rawData)
        {
            List<LeaderboardData> list = new List<LeaderboardData>();

            IList list2 = rawData as IList;

            if (list2 == null)
                return list;

            foreach (IDictionary item in list2)
            {
                if (item == null) continue;

                // Geçersiz tip dönüşüm hatasını engellemek için Convert.ToInt32 kullanıyoruz
                int position = Convert.ToInt32(item["ranking"]);
                string id = item.Contains("playerid") ? item["playerid"].ToString() : "";
                string name = item.Contains("playername") && item["playername"] != null ? (string)item["playername"] : "Unknown";
                int point = Convert.ToInt32(
                    item.Contains("score") ? item["score"] : item["points"]
                );

                // --- PYTHON BACKEND'DEN GELEN YENİ ALANLARI GÜVENLİCE OKUYORUZ ---
                int level = item.Contains("multiplayer_level")
                ? Convert.ToInt32(item["multiplayer_level"])
                : 0;
                string rankName = item.Contains("rank_name") && item["rank_name"] != null ? (string)item["rank_name"] : "";
                string spriteName = item.Contains("sprite_name") && item["sprite_name"] != null ? (string)item["sprite_name"] : "";

                // Listeye yeni constructor yapısıyla ekliyoruz
                list.Add(new LeaderboardData(position, id, name, point, level, rankName, spriteName));
            }
            return list;
        }
    }
}