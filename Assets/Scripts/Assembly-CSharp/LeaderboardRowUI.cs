using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
	public TextMeshProUGUI placementText;

	public TextMeshProUGUI nameText;

	public TextMeshProUGUI rankNameText;

	public TextMeshProUGUI pointsText;

	public void SetData(int placement, string name, string rankName, int points)
	{
		placementText.text = "#" + placement;
		nameText.text = name;
		rankNameText.text = rankName;
		pointsText.text = points + " Puan";
	}
}
