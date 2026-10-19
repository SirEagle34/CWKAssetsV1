public class PvPAllySelectController : Singleton<PvPAllySelectController>
{
	public UITweenController ShowTween;

	private AllyListController mAllyListController;

	private void Awake()
	{
		mAllyListController = GetComponent<AllyListController>();
	}

	public void Populate()
	{
		mAllyListController.ShowInbox(showOnlineStatus: true, OnAlliesPopulated);
	}

	private void OnAlliesPopulated()
	{
		ShowTween.Play();
		for (int i = 0; i < mAllyListController.AllyListGrid.transform.childCount; i++)
		{
			mAllyListController.AllyListGrid.transform.GetChild(i).GetComponent<HelperPrefabScript>().Mode = HelperMode.PvpMatch;
		}
	}

	public void Unload()
	{
		mAllyListController.Unload();
	}

	public void OnAllyClicked(HelperPrefabScript prefab)
	{
		if (!prefab.Helper.OnlineStatus)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!PLAYER_OFFLINE"));
		}
		else
		{
			Singleton<PVPSendMatchRequestController>.Instance.SendMatchRequest(prefab.Helper);
		}
	}
}
