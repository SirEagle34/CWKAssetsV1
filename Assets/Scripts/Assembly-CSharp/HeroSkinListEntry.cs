using UnityEngine;

public class HeroSkinListEntry : UIStreamingGridListItem
{
	public GameObject Highlight;

	public UITexture LeaderImage;

	public GameObject OwnedObject;

	public UILabel CostLabel;

	public LeaderData Leader { get; set; }

	public override void Populate(object dataObj)
	{
		Leader = dataObj as LeaderData;
		Singleton<SLOTResourceManager>.Instance.QueueUITextureLoad(Leader.PortraitTexture, "FTUEBundle", "UI/UI/LoadingPlaceholder", LeaderImage);
		LeaderItem leader = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout().Leader;
		bool active = leader.SelectedSkin == Leader;
		bool flag = true;

		LeaderItem owner = null;

		// Level 1
		if (Leader.SkinParentLeader != null)
		{
			owner = Singleton<PlayerInfoScript>.Instance.GetLeaderItem(Leader.SkinParentLeader);
		}

		// Level 2 fallback
		if (owner == null && Leader.SkinParentLeaderLevel2 != null)
		{
			owner = Singleton<PlayerInfoScript>.Instance.GetLeaderItem(Leader.SkinParentLeaderLevel2);
		}

		// Level 3 fallback
		if (owner == null && Leader.SkinParentLeaderLevel3 != null)
		{
			owner = Singleton<PlayerInfoScript>.Instance.GetLeaderItem(Leader.SkinParentLeaderLevel3);
		}

		if (owner != null && !owner.OwnedSkins.Contains(Leader))
		{
			flag = false;
		}
		OwnedObject.SetActive(flag);
		Highlight.SetActive(active);
		if (!flag)
		{
			CostLabel.transform.SetParentActive(true);
			CostLabel.text = Leader.SkinBuyCost.ToString();
		}
		else
		{
			CostLabel.transform.SetParentActive(false);
		}
	}

	public override void Unload()
	{
		LeaderImage.UnloadTexture();
	}

	private void OnClick()
	{
		Singleton<PlayerCustomizationController>.Instance.OnClickHeroSkinEntry(Leader);
	}
}
