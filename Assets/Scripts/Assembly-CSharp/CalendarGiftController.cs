using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalendarGiftController : Singleton<CalendarGiftController>
{
	public GameObject MainPanel;

	public GameObject DayTemplatePrefab;

	public UITweenController RewardFXShow;

	public UITweenController RewardFXHide;

	public UITweenController CalendarShow;

	public UITweenController CalendarHide;

	public UITweenController MonthRewardClaim;

	public UISprite MonthRewardIcon;

	public UITexture MonthRewardTexture;

	public UILabel MonthRewardAmount;

	public UILabel FinaleLabel;

	public UILabel TitleLabel;

	public GameObject CalendarButtonClose;

	public GameObject EffectsObject;

	public Transform DayNodesParent;

	public UITexture Background;

	private bool mShowing;

	public void PopupGift()
	{
		StartCoroutine(GiftAndShowReward());
	}

	private IEnumerator GiftAndShowReward()
	{
		mShowing = true;
		bool bgLoaded = false;
		Singleton<SLOTResourceManager>.Instance.QueueResourceLoad("UI/GeneralBundle/Calendar_Frame", "GeneralBundle", delegate(Object loadedResouce)
		{
			Background.UnloadTexture();
			Background.mainTexture = loadedResouce as Texture;
			bgLoaded = true;
		});
		while (!bgLoaded)
		{
			yield return null;
		}
		if (Singleton<PlayerInfoScript>.Instance.SaveData.ActiveCalendar.Entries[0].Repeating)
		{
			TitleLabel.text = KFFLocalization.Get("!!REPEATING_CALENDAR");
		}
		else
		{
			TitleLabel.text = KFFLocalization.Get("!!DAILY_CALENDAR");
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		List<CalendarGift> gifts = Singleton<PlayerInfoScript>.Instance.GetCalendarGifts();
		List<DailyCalendarGiftItem> dayPrefabs = new List<DailyCalendarGiftItem>();
		for (int num = 0; num < gifts.Count - 1; num++)
		{
			string n = "Node_Day_" + (num + 1).ToString("D2");
			Transform transform = DayNodesParent.Find(n);
			if (!(transform == null))
			{
				DailyCalendarGiftItem component = transform.InstantiateAsChild(DayTemplatePrefab).GetComponent<DailyCalendarGiftItem>();
				dayPrefabs.Add(component);
				component.DayLabel.text = KFFLocalization.Get("!!DAY_X").Replace("<val1>", (num + 1).ToString());
				if (gifts[num].Texture != null)
				{
					component.RewardIcon.gameObject.SetActive(value: false);
					component.RewardTexture.gameObject.SetActive(value: true);
					component.RewardTexture.ReplaceTexture(gifts[num].Texture);
				}
				else
				{
					component.RewardTexture.gameObject.SetActive(value: false);
					component.RewardIcon.gameObject.SetActive(value: true);
					PackSprite.Apply(component.RewardIcon, gifts[num].Icon);
				}
				component.SetClaimed(num < (int)saveData.OneTimeCalendarDaysClaimed);
			}
		}
		CalendarGift calendarGift = gifts[gifts.Count - 1];
		if (calendarGift.Texture != null)
		{
			MonthRewardIcon.gameObject.SetActive(value: false);
			MonthRewardTexture.gameObject.SetActive(value: true);
			MonthRewardTexture.ReplaceTexture(calendarGift.Texture);
		}
		else
		{
			MonthRewardTexture.gameObject.SetActive(value: false);
			MonthRewardIcon.gameObject.SetActive(value: true);
			PackSprite.Apply(MonthRewardIcon, calendarGift.Icon);
		}
		MonthRewardAmount.text = gifts[gifts.Count - 1].Quantity.ToString();
		FinaleLabel.text = KFFLocalization.Get("!!DAY_X").Replace("<val1>", gifts.Count.ToString());
		int grantingGiftIndex = saveData.OneTimeCalendarDaysClaimed;
		CalendarGift grantingGift = gifts[grantingGiftIndex];
		++saveData.OneTimeCalendarDaysClaimed;
		saveData.LastOneTimeCalendarDateClaimed = TFUtils.ServerTime;
		InventorySlotItem slotItem;
		CardBackData cardBack;
		string displayName = grantingGift.Grant(out slotItem, out cardBack);
		Singleton<PlayerInfoScript>.Instance.Save();
		RewardFXShow.Play();
		CalendarShow.Play();
		CalendarButtonClose.SetActive(value: false);
		while (LoadingScreenController.ShowingLoadingScreen())
		{
			yield return null;
		}
		yield return new WaitForSeconds(1f);
		Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_Calendar_Show");
		if (grantingGiftIndex == gifts.Count - 1)
		{
			MonthRewardClaim.Play();
		}
		else
		{
			dayPrefabs[grantingGiftIndex].Claim();
		}
		yield return new WaitForSeconds(1.5f);
		RewardFXHide.Play();
		EffectsObject.SetActive(value: false);
		if (cardBack != null)
		{
			Singleton<DailyGiftBannerController>.Instance.Init(displayName, cardBack);
		}
		else if (slotItem != null)
		{
			Singleton<DailyGiftBannerController>.Instance.Init(displayName, slotItem);
		}
		else
		{
			Singleton<DailyGiftBannerController>.Instance.Init(displayName, grantingGift.Icon);
		}
		yield return StartCoroutine(Singleton<DailyGiftBannerController>.Instance.ShowBannerNowCo());
		EffectsObject.SetActive(value: true);
		RewardFXShow.Play();
		CalendarButtonClose.SetActive(value: true);
		while (mShowing)
		{
			yield return null;
		}
		foreach (DailyCalendarGiftItem item in dayPrefabs)
		{
			item.RewardTexture.UnloadTexture();
			NGUITools.Destroy(item.gameObject);
		}
		MonthRewardTexture.UnloadTexture();
	}

	public void HideCalendar()
	{
		Singleton<TownController>.Instance.ToggleTownColliders(enable: true);
		Singleton<TownController>.Instance.AdvanceIntroState();
	}

	public void Unload()
	{
		Background.UnloadTexture();
		mShowing = false;
	}
}
