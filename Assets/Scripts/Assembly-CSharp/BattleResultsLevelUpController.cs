using System.Collections;
using UnityEngine;

public class BattleResultsLevelUpController : Singleton<BattleResultsLevelUpController>
{
	public GameObject MainPanel;

	public UITweenController ShowTween;

	public UITweenController HideTween;

	public UILabel Level;

	public UIGrid NumericalsParent;

	public GameObject StaminaGroup;

	public UILabel StaminaLabel;

	public GameObject InventorySpaceGroup;

	public UILabel InventorySpaceLabel;

	public ParticleSystem mRays;

	public bool ShowLevelUp()
	{
		if (Singleton<PlayerInfoScript>.Instance.SaveData.PlayersLastSavedLevel != Singleton<PlayerInfoScript>.Instance.RankXpLevelData.mCurrentLevel)
		{
			StartCoroutine(PauseBeforeAnimating());
			return true;
		}
		return false;
	}

	public void DebugShowLevelUp()
	{
		StartCoroutine(PauseBeforeAnimating());
	}

	public IEnumerator PauseBeforeAnimating()
	{
		UICamera.LockInput();
		while (LoadingScreenController.ShowingLoadingScreen())
		{
			yield return null;
		}
		if (Singleton<ExpeditionStartController>.Instance.Showing)
		{
			Singleton<ExpeditionStartController>.Instance.HideTween.Play();
		}

		int lastLevel = Singleton<PlayerInfoScript>.Instance.SaveData.PlayersLastSavedLevel;
		int newLevel = Singleton<PlayerInfoScript>.Instance.RankXpLevelData.mCurrentLevel;

		// Guard: rank data must be loaded before proceeding
		PlayerRankData newLevelData = PlayerRankDataManager.Instance.GetData(newLevel - 1);
		PlayerRankData lastLevelData = PlayerRankDataManager.Instance.GetData(lastLevel - 1);
		if (newLevelData == null || lastLevelData == null)
		{
			Debug.LogError($"[LevelUp] Missing rank data — newLevel:{newLevel} lastLevel:{lastLevel}. Aborting level-up display.");
			UICamera.UnlockInput();
			yield break;
		}

		Singleton<PlayerInfoScript>.Instance.SaveData.PlayersLastSavedLevel = newLevel;
		ShowTween.Play();
		mRays.loop = true;
		Level.text = newLevel.ToString();

		// Guard: Find can return null if the unlock entry is missing
		int socialUnlockLevel = -1;
		if (PlayerRankDataManager.Instance.UnlockRanks != null)
		{
			PlayerRankData socialEntry = PlayerRankDataManager.Instance.UnlockRanks
				.Find((PlayerRankData m) => m.UnlockId == "TBuilding_Social");
			if (socialEntry != null)
				socialUnlockLevel = socialEntry.Level;
			else
				Debug.LogWarning("[LevelUp] 'TBuilding_Social' unlock rank not found in PlayerRankDataManager.");
		}
		else
		{
			Debug.LogError("[LevelUp] UnlockRanks is null — PlayerRankDataManager may not have finished loading.");
		}

		int inventorySlotsGained = 0;
		for (int gainedLevel = lastLevel + 1; gainedLevel <= newLevel; gainedLevel++)
		{
			switch (gainedLevel)
			{
			case 2:
				Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_LVL_2);
				break;
			case 10:
				Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_LVL_10);
				break;
			case 30:
				Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_LVL_30);
				break;
			case 50:
				Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_LVL_50);
				break;
			}
			if (gainedLevel == socialUnlockLevel)
			{
				Singleton<PlayerInfoScript>.Instance.SetDefaultHelperCreature();
			}

			// Guard: GetData can return null if gainedLevel is out of range
			PlayerRankData gainedLevelData = PlayerRankDataManager.Instance.GetData(gainedLevel - 1);
			if (gainedLevelData == null)
			{
				Debug.LogWarning($"[LevelUp] No rank data found for level {gainedLevel}, skipping inventory slot calculation.");
				continue;
			}
			inventorySlotsGained += gainedLevelData.InventorySpace;
		}

		if (inventorySlotsGained > 0)
		{
			Singleton<PlayerInfoScript>.Instance.SaveData.AddEmptyInventorySlots(inventorySlotsGained);
		}
		Singleton<PlayerInfoScript>.Instance.Save();

		int staminaGained = newLevelData.Stamina - lastLevelData.Stamina;
		if (staminaGained > 0)
		{
			StaminaGroup.SetActive(true);
			StaminaLabel.text = "+" + staminaGained;
		}
		else
		{
			StaminaGroup.SetActive(false);
		}

		if (inventorySlotsGained > 0)
		{
			InventorySpaceGroup.SetActive(true);
			InventorySpaceLabel.text = "+" + inventorySlotsGained;
		}
		else
		{
			InventorySpaceGroup.SetActive(false);
		}

		NumericalsParent.Reposition();
		StartCoroutine(PauseThenAnimateOut());
	}

	public IEnumerator PauseThenAnimateOut()
	{
		yield return new WaitForSeconds(2f);
		mRays.loop = false;
		yield return new WaitForSeconds(2f);
		HideLevelUp();
	}

	public void HideLevelUp()
	{
		UICamera.UnlockInput();
		HideTween.Play();
		Singleton<TownController>.Instance.AdvanceIntroState();
	}
}
