using UnityEngine;

public class SeasonProgressBar : MonoBehaviour
{
	public LeagueBadge Badge;

	public UILabel PointsTotal;

	public UILabel PointsAccumulated;

	public UISprite FillBar;

	public void AnimateTo(int pointsFrom, int pointsTo, int pointsTotal)
	{
		PointsAccumulated.text = string.Empty;
		PointsTotal.text = pointsTo + " / " + pointsTotal;
		FillBar.fillAmount = Mathf.Clamp01((float)pointsTo / (float)pointsTotal);
	}
}
