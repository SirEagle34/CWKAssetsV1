using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureBuffBar : MonoBehaviour
{
	public float EnemyScale = 0.7f;

	public GameObject StatusIconPrefab;

	public GameObject StatHintParent;

	public UILabel[] StatHints = new UILabel[6];

	public Color[] StatColors = new Color[6];

	public UIGrid StatusIconParentGrid;

	public CreatureState mCreature;

	private GameObject mCreatureObject;

	private List<StatusIconItem> mPersistentFXIcons = new List<StatusIconItem>();

	public void Init(CreatureState creature)
	{
		mCreature = creature;
		mCreatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(creature);
		if (mCreature.Owner.Type == PlayerType.Opponent)
		{
			base.transform.localScale = new Vector3(EnemyScale, EnemyScale, 1f);
		}
	}

	private void Update()
	{
		if (mCreatureObject == null)
		{
			Singleton<BattleHudController>.Instance.BuffBars.Remove(this);
			Object.Destroy(base.gameObject);
		}
	}

	private void LateUpdate()
	{
		if (Singleton<DWGameCamera>.Instance.MainCam != null && mCreatureObject != null)
		{
			Vector3 position = mCreatureObject.transform.position;
			position.y += mCreature.Data.Form.Height;
			Vector2 vector = Singleton<DWGameCamera>.Instance.MainCam.WorldToScreenPoint(position);
			base.transform.position = Singleton<DWGameCamera>.Instance.BattleUICam.ScreenToWorldPoint(vector);
		}
	}

	public List<StatusIconItem> GetActivePersistentFXList()
	{
		return mPersistentFXIcons;
	}

	public IEnumerator SetPersistentVFX(GameMessage ms)
	{
		if (ms.Status == null)
		{
			yield break;
		}
		if (ms.Action == GameEvent.GAIN_BUFF || ms.Action == GameEvent.GAIN_DEBUFF || ms.Action == GameEvent.TICK_STATUS)
		{
			StatusIconItem statusIconItem = mPersistentFXIcons.Find((StatusIconItem m) => m.Status == ms.Status);
			if (statusIconItem == null)
			{
				_ = ms.Action;
				StatusIconItem component = StatusIconParentGrid.transform.InstantiateAsChild(StatusIconPrefab).GetComponent<StatusIconItem>();
				component.Populate(ms.Status, ms);
				mPersistentFXIcons.Add(component);
				StatusIconParentGrid.Reposition();
			}
			else
			{
				yield return StartCoroutine(statusIconItem.UpdateValue(ms));
			}
		}
		else if (ms.Action == GameEvent.LOSE_BUFF || ms.Action == GameEvent.LOSE_DEBUFF)
		{
			int num = mPersistentFXIcons.FindIndex((StatusIconItem m) => m.Status == ms.Status);
			if (num != -1)
			{
				mPersistentFXIcons[num].StartHideTween();
				mPersistentFXIcons[num].transform.parent = mPersistentFXIcons[num].transform.parent.parent;
				mPersistentFXIcons.RemoveAt(num);
				StatusIconParentGrid.Reposition();
			}
		}
	}

	public GameObject GetStatusEffectObject(string effectId)
	{
		StatusIconItem statusIconItem = mPersistentFXIcons.Find((StatusIconItem m) => m.Status.ID == effectId);
		if (statusIconItem != null)
		{
			return statusIconItem.gameObject;
		}
		return null;
	}

	private GameEvent GetDisableEventFX(GameMessage ms)
	{
		GameEvent result = GameEvent.NONE;
		foreach (StatusData item in StatusDataManager.Instance.GetDatabase())
		{
			if (item.DisableMessage == ms.Action)
			{
				return result = item.EnableMessage;
			}
		}
		return result;
	}

	public void StartBlinkPredictedBuff(CardData card)
	{
		StatHintParent.SetActive(value: true);
		List<CreatureStat> statHints = card.GetStatHints(mCreature.Owner.Type);
		for (int i = 0; i < StatHints.Length; i++)
		{
			if (i < statHints.Count)
			{
				StatHints[i].gameObject.SetActive(value: true);
				StatHints[i].text = statHints[i].DisplayName() + " " + mCreature.GetStat(statHints[i]);
				StatHints[i].color = StatColors[(int)statHints[i]];
			}
			else
			{
				StatHints[i].gameObject.SetActive(value: false);
			}
		}
	}

	public void StopBlinkPredictedBuff()
	{
		StatHintParent.SetActive(value: false);
	}

	public StatusIconItem FindStatusIcon(string statusKeyword)
	{
		return mPersistentFXIcons.Find((StatusIconItem m) => m.Status.FXData.Keyword.ID == statusKeyword);
	}
}
