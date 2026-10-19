using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class CreatureData : ILoadableData
{
	public const int ACTION_CARDS_PER_CREATURE = 6;

	public const int MAX_EVO_RECIPE_SIZE = 5;

	public const int MAX_RARITY = 5;

	public const int MAX_GEM_SLOTS = 3;

	public const int MAX_EXCARDS_SLOTS = 4;

	private string _ID;

	private string _Name;

	private string _Details;

	private string _SoftLaunch;

	private string _Prefab;

	private string _PrefabTexture;

	private string _PrefabTexture2;

	private string _RezInVFX;

	private string _PersistentVFX;

	private string _PersistentVFXAttachBone;

	private string _ScriptName;

	private CardData[] _ActionCards = new CardData[6];

	private int[] _ActionCardWeights = new int[6];

	private string _PortraitTexture;

	private AttackStyle _Style;

	private string _AttackChargeVFX;

	private string _ShootVFX;

	private string _ShootVFXAttachBone;

	private string _ShootVFXType;

	private string _WeaponTrailVFX;

	private string _HitVFX;

	private string _CritHitVFX;

	private CreatureFaction _Faction;

	private CreatureType _Type;

	private int _Val1;

	private int _Val2;

	private string _Class;

	private string _EvolvesTo;

	private int _EvolveCost;

	private int _CardToCreate;

	private float _ChargeTime;

	private float _ShootStartTime;

	private float _ShootTravelTime;

	private iTween.EaseType _ShootTravelStyle;

	private float _AttackStartTime;

	private float _WeaponTrailStartTime;

	private float _AttackMovementTime;

	private float _MultiAttackInterval;

	private float _MultiShootInterval;

	private iTween.EaseType _AttackMovementStyle;

	private string _ShadowTexture;

	private int _ShadowSize;

	private int _InitNumOfLoad;

	private MetaSpecial _MetaSpecial;

	private int _MetaSpecialParam;

	private int _MinFeedXp;

	private int _MaxFeedXp;

	private float _Width;

	private float _Height;

	private ObscuredInt _TeamCost;

	private float _GrowthFactor;

	private EvoMaterialData[] _CardSlotUnlocks = new EvoMaterialData[4];

	private int[] _CardSlotUnlockAmounts = new int[4];

	private int[] _CardSlotUnlockCosts = new int[4];

	private bool _TutorialOnly;

	public CardData ActionCardData;

	public string ID => _ID;

	public string Name => _Name;

	public string Details => _Details;

	public string SoftLaunch => _SoftLaunch;

	public ObscuredInt MinHP { get; private set; }

	public ObscuredInt MaxHP { get; private set; }

	public ObscuredInt MinSTR { get; private set; }

	public ObscuredInt MaxSTR { get; private set; }

	public ObscuredInt MinDEF { get; private set; }

	public ObscuredInt MaxDEF { get; private set; }

	public ObscuredInt MinINT { get; private set; }

	public ObscuredInt MaxINT { get; private set; }

	public ObscuredInt MinRES { get; private set; }

	public ObscuredInt MaxRES { get; private set; }

	public ObscuredInt MinDEX { get; private set; }

	public ObscuredInt MaxDEX { get; private set; }

	public ObscuredInt AttackCost { get; private set; }

	public ObscuredInt DeployCost { get; private set; }

	public string Prefab => _Prefab;

	public string PrefabTexture => _PrefabTexture;

	public string RezInVFX => _RezInVFX;

	public string PersistentVFX => _PersistentVFX;

	public string PersistentVFXAttachBone => _PersistentVFXAttachBone;

	public IList<CardData> ActionCards => Array.AsReadOnly(_ActionCards);

	public IList<int> ActionCardWeights => Array.AsReadOnly(_ActionCardWeights);

	public string PortraitTexture => _PortraitTexture;

	public AttackStyle Style => _Style;

	public string AttackChargeVFX => _AttackChargeVFX;

	public string ShootVFX => _ShootVFX;

	public string ShootVFXAttachBone => _ShootVFXAttachBone;

	public string ShootVFXType => _ShootVFXType;

	public string WeaponTrailVFX => _WeaponTrailVFX;

	public string HitVFX => _HitVFX;

	public string CritHitVFX => _CritHitVFX;

	public CreatureFaction Faction => _Faction;

	public CreatureType Type => _Type;

	public bool AlwaysMagicVFX { get; private set; }

	public int Val1 => _Val1;

	public int Val2 => _Val2;

	public string Class => _Class;

	public string EvolvesTo => _EvolvesTo;

	public int EvolveCost => _EvolveCost;

	public string ScriptName => _ScriptName;

	public int CardToCreate => _CardToCreate;

	public float ChargeTime => _ChargeTime;

	public float ShootStartTime => _ShootStartTime;

	public float ShootTravelTime => _ShootTravelTime;

	public iTween.EaseType ShootTravelStyle => _ShootTravelStyle;

	public float AttackStartTime => _AttackStartTime;

	public float WeaponTrailStartTime => _WeaponTrailStartTime;

	public float AttackMovementTime => _AttackMovementTime;

	public float MultiAttackInterval => _MultiAttackInterval;

	public float MultiShootInterval => _MultiShootInterval;

	public iTween.EaseType AttackMovementStyle => _AttackMovementStyle;

	public string ShadowTexture => _ShadowTexture;

	public int ShadowSize => _ShadowSize;

	public int InitNumOfLoad => _InitNumOfLoad;

	public MetaSpecial MetaSpecial => _MetaSpecial;

	public int MetaSpecialParam => _MetaSpecialParam;

	public int MinFeedXp => _MinFeedXp;

	public int MaxFeedXp => _MaxFeedXp;

	public float Width => _Width;

	public float Height => _Height;

	public int TeamCost => _TeamCost;

	public float GrowthFactor => _GrowthFactor;

	public bool TutorialOnly => _TutorialOnly;

	public int CreatureNumber { get; private set; }

	public bool HideInMuseum { get; private set; }

	public bool BlockFromRandomDungeons { get; private set; }

	public List<List<EvoMaterialData>> EnhanceRecipes { get; private set; }

	public int Rarity { get; private set; }

	public CreaturePassiveData PassiveData { get; private set; }

	public int SellPrice { get; private set; }

	public string ChargeSound { get; private set; }

	public string ShootSound { get; private set; }

	public string LungeSound { get; private set; }

	public string SmashSound { get; private set; }

	public string IntroSound { get; private set; }

	public string ZoomSound { get; private set; }

	public string BattleZoomSound { get; private set; }

	public int TotalMaxLevel { get; private set; }

	public EvoMaterialData AwakenMaterial { get; private set; }

	public EvoMaterialData PassiveUpMaterial { get; private set; }

	public EvoMaterialData TurnsIntoMaterial { get; private set; }

	public int TurnsIntoMaterialCount { get; private set; }

	public int CardSlots { get; private set; }

	public int CostPerPassiveFeed { get; private set; }

	public bool AlreadySeen { get; set; }

	public bool AlreadyCollected { get; set; }

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "ID", string.Empty);
		PassiveData = CreaturePassiveDataManager.Instance.GetData(ID);
		_Name = TFUtils.LoadLocalizedString(dict, "Name", string.Empty);
		_SoftLaunch = TFUtils.LoadLocalizedString(dict, "SoftLaunch", string.Empty);
		MinHP = TFUtils.LoadInt(dict, "MinHP", 0);
		MaxHP = TFUtils.LoadInt(dict, "MaxHP", 0);
		MinSTR = TFUtils.LoadInt(dict, "MinSTR", 0);
		MaxSTR = TFUtils.LoadInt(dict, "MaxSTR", 0);
		MinDEF = TFUtils.LoadInt(dict, "MinDEF", 0);
		MaxDEF = TFUtils.LoadInt(dict, "MaxDEF", 0);
		MinINT = TFUtils.LoadInt(dict, "MinINT", 0);
		MaxINT = TFUtils.LoadInt(dict, "MaxINT", 0);
		MinRES = TFUtils.LoadInt(dict, "MinRES", 0);
		MaxRES = TFUtils.LoadInt(dict, "MaxRES", 0);
		MinDEX = TFUtils.LoadInt(dict, "MinDEX", 0);
		MaxDEX = TFUtils.LoadInt(dict, "MaxDEX", 0);
		AttackCost = TFUtils.LoadInt(dict, "AttackCost", 1);
		DeployCost = TFUtils.LoadInt(dict, "DeployCost", 1);
		_Prefab = TFUtils.LoadString(dict, "Prefab", string.Empty);
		_PrefabTexture = TFUtils.LoadString(dict, "PrefabTexture", string.Empty);
		_PrefabTexture2 = TFUtils.LoadString(dict, "PrefabTexture2", string.Empty);
		_RezInVFX = TFUtils.LoadString(dict, "RezInVFX", string.Empty);
		_PersistentVFX = TFUtils.LoadString(dict, "PersistentVFX", string.Empty);
		_PersistentVFXAttachBone = TFUtils.LoadString(dict, "PersistentVFXAttachBone", string.Empty);
		_PortraitTexture = "UI/Icons_Creatures/" + TFUtils.LoadString(dict, "PortraitTexture", string.Empty);
		ChargeSound = TFUtils.LoadString(dict, "ChargeSound", string.Empty);
		ShootSound = TFUtils.LoadString(dict, "ShootSound", string.Empty);
		LungeSound = TFUtils.LoadString(dict, "LungeSound", string.Empty);
		SmashSound = TFUtils.LoadString(dict, "SmashSound", string.Empty);
		IntroSound = TFUtils.LoadString(dict, "IntroSound", null);
		ZoomSound = TFUtils.LoadString(dict, "ZoomSound", null);
		BattleZoomSound = TFUtils.LoadString(dict, "BattleZoomSound", null);
		_AttackChargeVFX = TFUtils.LoadString(dict, "AttackChargeVFX", string.Empty);
		_ShootVFX = TFUtils.LoadString(dict, "ShootVFX", string.Empty);
		_ShootVFXAttachBone = TFUtils.LoadString(dict, "ShootVFXAttachBone", string.Empty);
		_ShootVFXType = TFUtils.LoadString(dict, "ShootVFXType", string.Empty);
		_WeaponTrailVFX = TFUtils.LoadString(dict, "WeaponTrailVFX", string.Empty);
		_HitVFX = TFUtils.LoadString(dict, "HitVFX", string.Empty);
		_CritHitVFX = TFUtils.LoadString(dict, "CritHitVFX", string.Empty);
		_Class = TFUtils.LoadString(dict, "Class", string.Empty);
		_EvolvesTo = TFUtils.LoadString(dict, "EvoTo", string.Empty);
		_EvolveCost = TFUtils.LoadInt(dict, "EvoCost", 0);
		_ScriptName = TFUtils.LoadString(dict, "ScriptName", string.Empty);
		_Faction = (CreatureFaction)(int)Enum.Parse(typeof(CreatureFaction), TFUtils.LoadString(dict, "Faction", string.Empty), ignoreCase: true);
		_Type = (CreatureType)(int)Enum.Parse(typeof(CreatureType), TFUtils.LoadString(dict, "Type", "None"), ignoreCase: true);
		CardSlots = TFUtils.LoadInt(dict, "CardSlots", 0);
		string text = TFUtils.LoadString(dict, "CardToCreate", string.Empty);
		text = text.Replace("ActionCard", string.Empty);
		_CardToCreate = ((!(text == string.Empty)) ? (int.Parse(text) - 1) : (-1));
		_ChargeTime = (float)TFUtils.LoadInt(dict, "ChargeFrame", 0) / 30f;
		_ShootStartTime = (float)TFUtils.LoadInt(dict, "ShootStartFrame", 0) / 30f;
		_ShootTravelTime = (float)TFUtils.LoadInt(dict, "ShootTravelFrames", 0) / 30f;
		_AttackStartTime = (float)TFUtils.LoadInt(dict, "AttackStartFrame", 0) / 30f;
		_WeaponTrailStartTime = (float)TFUtils.LoadInt(dict, "WeaponTrailStartFrame", 0) / 30f;
		_AttackMovementTime = (float)TFUtils.LoadInt(dict, "AttackTravelFrames", 30) / 30f;
		_MultiAttackInterval = TFUtils.LoadFloat(dict, "MultiAttackInterval", 0f);
		_MultiShootInterval = TFUtils.LoadFloat(dict, "MultiShootInterval", 0f);
		_ShadowTexture = TFUtils.LoadString(dict, "ShadowTexture", string.Empty);
		_ShadowSize = TFUtils.LoadInt(dict, "ShadowSize", 0);
		_InitNumOfLoad = TFUtils.LoadInt(dict, "InitNumOfLoad", 0);
		_MinFeedXp = TFUtils.LoadInt(dict, "minFeedXP", 0);
		_MaxFeedXp = TFUtils.LoadInt(dict, "maxFeedXP", 0);
		_Width = TFUtils.LoadFloat(dict, "Width", 3f);
		_Height = TFUtils.LoadFloat(dict, "Height", 3f);
		_TeamCost = TFUtils.LoadInt(dict, "TeamCost", 1);
		_GrowthFactor = TFUtils.LoadFloat(dict, "GrowthFactor", 0f);
		SellPrice = TFUtils.LoadInt(dict, "SellPrice", 0);
		_TutorialOnly = TFUtils.LoadBool(dict, "TutorialOnly", defaultValue: false);
		CreatureNumber = TFUtils.LoadInt(dict, "Number", -1);
		HideInMuseum = TFUtils.LoadBool(dict, "HideInMuseum", defaultValue: false);
		BlockFromRandomDungeons = TFUtils.LoadBool(dict, "BlockFromRandomDungeons", defaultValue: false);
		Rarity = TFUtils.LoadInt(dict, "Rarity", 1);
		TotalMaxLevel = TFUtils.LoadInt(dict, "MaxLevel", 1);
		CostPerPassiveFeed = TFUtils.LoadInt(dict, "CostPerPassiveFeed", 0);
		AlwaysMagicVFX = TFUtils.LoadBool(dict, "AlwaysMagicVFX", defaultValue: false);
		string text2 = TFUtils.LoadString(dict, "ShootTravelStyle", string.Empty);
		if (text2 != string.Empty)
		{
			_ShootTravelStyle = (iTween.EaseType)(int)Enum.Parse(typeof(iTween.EaseType), text2, ignoreCase: true);
		}
		else
		{
			_ShootTravelStyle = iTween.EaseType.linear;
		}
		string text3 = TFUtils.LoadString(dict, "AttackTravelStyle", string.Empty);
		if (text3 != string.Empty)
		{
			_AttackMovementStyle = (iTween.EaseType)(int)Enum.Parse(typeof(iTween.EaseType), text3, ignoreCase: true);
		}
		else
		{
			_AttackMovementStyle = iTween.EaseType.linear;
		}
		string text4 = TFUtils.LoadString(dict, "MetaSpecial", string.Empty);
		if (text4 != string.Empty)
		{
			_MetaSpecial = (MetaSpecial)(int)Enum.Parse(typeof(MetaSpecial), text4, ignoreCase: true);
		}
		else
		{
			_MetaSpecial = MetaSpecial.None;
		}
		for (int i = 1; i <= 6; i++)
		{
			string iD = TFUtils.LoadString(dict, "ActionCard" + i, string.Empty);
			_ActionCards[i - 1] = CardDataManager.Instance.GetData(iD);
			_ActionCardWeights[i - 1] = TFUtils.LoadInt(dict, "CardWeight" + i, 20);
			_ = _ActionCards[i - 1];
		}
		string text5 = TFUtils.LoadString(dict, "AttackStyle", string.Empty);
		_Style = AttackStyle.None;
		if (!(text5 == "Melee"))
		{
			if (text5 == "Range")
			{
				_Style = AttackStyle.Range;
			}
		}
		else
		{
			_Style = AttackStyle.Melee;
		}
		_Val1 = TFUtils.LoadInt(dict, "Val1", 0);
		_Val2 = TFUtils.LoadInt(dict, "Val2", 0);
		for (int j = 0; j < 4; j++)
		{
			_CardSlotUnlocks[j] = EvoMaterialDataManager.Instance.GetData(TFUtils.LoadString(dict, "ExRuneID" + (j + 1), "TestRune" + (j + 1)));
			_CardSlotUnlockAmounts[j] = TFUtils.LoadInt(dict, "ExRuneAmount" + (j + 1), 1);
			_CardSlotUnlockCosts[j] = TFUtils.LoadInt(dict, "ExCost" + (j + 1), 100);
		}
		EnhanceRecipes = new List<List<EvoMaterialData>>();
		int num = 1;
		while (true)
		{
			string text6 = TFUtils.LoadString(dict, "EnhanceRecipe" + num, null);
			if (text6 == null)
			{
				break;
			}
			List<EvoMaterialData> list = new List<EvoMaterialData>();
			EnhanceRecipes.Add(list);
			string[] array = text6.Split(',');
			for (int k = 0; k < array.Length; k++)
			{
				string text7 = array[k].Trim();
				if (!(text7 == string.Empty))
				{
					EvoMaterialData data = EvoMaterialDataManager.Instance.GetData(text7);
					if (data != null)
					{
						list.Add(data);
					}
				}
			}
			num++;
		}
		string text8 = TFUtils.LoadString(dict, "AwakenMat", null);
		if (text8 != null)
		{
			AwakenMaterial = EvoMaterialDataManager.Instance.GetData(text8);
		}
		string text9 = TFUtils.LoadString(dict, "PassiveUpMat", null);
		if (text9 != null)
		{
			PassiveUpMaterial = EvoMaterialDataManager.Instance.GetData(text9);
		}
		string text10 = TFUtils.LoadString(dict, "TurnsIntoMat", null);
		if (text10 != null)
		{
			TurnsIntoMaterial = EvoMaterialDataManager.Instance.GetData(text10);
		}
		TurnsIntoMaterialCount = TFUtils.LoadInt(dict, "TurnsIntoMatCount", 5);
	}

	public string GetClassString()
	{
		return Faction.ClassDisplayName();
	}

	public bool GrantsPassiveSkillup(CreatureData toCreature)
	{
		return false;
	}

	public void SwapCreatureTexture(GameObject obj, Texture2D loadedTexture, bool addSpin = false)
	{
		if (loadedTexture != null)
		{
			SkinnedMeshRenderer[] componentsInChildren = obj.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.mainTexture = loadedTexture;
			}
		}
		GameObject value = null;
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(PersistentVFX, out value);
		}
		if (value == null)
		{
			value = (GameObject)Singleton<SLOTResourceManager>.Instance.LoadResource("VFX/Creatures/" + PersistentVFX);
		}
		if (value != null)
		{
			foreach (Transform item in FindAllInChildren(obj.transform, PersistentVFXAttachBone, includeInactive: true))
			{
				item.InstantiateAsChild(value).AddComponent<VFXRenderQueueSorter>().ShouldScaleVFX = true;
			}
		}
		obj.ChangeLayer(obj.transform.parent.gameObject.layer);
		if (addSpin)
		{
			AddSpinToTheCreature(obj);
		}
	}

	public void AddSpinToTheCreature(GameObject obj)
	{
		obj.AddComponent<SpinWithMouse>();
		BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
		boxCollider.size = Vector3.one * 5f;
		boxCollider.center = new Vector3(0f, 2.5f, 0f);
		obj.AddComponent<UIWidget>().depth = 20;
		obj.AddComponent<BounceOnTap>().Init(this);
	}

	public void ParseKeywords()
	{
		if (ActionCards == null)
		{
			Debug.LogError("ActionCards NULL. Creature ID: " + ID + " Name: " + Name);
			return;
		}
		foreach (CardData actionCard in ActionCards)
		{
			actionCard?.ParseKeywords();
		}
	}

	private List<Transform> FindAllInChildren(Transform tr, string childName, bool includeInactive = false)
	{
		List<Transform> list = new List<Transform>();
		if (childName == string.Empty)
		{
			list.Add(tr);
			return list;
		}
		return tr.gameObject.GetComponentsInChildren<Transform>(includeInactive).FindAll((Transform m) => m.name.Contains(childName));
	}

	public bool HasPassiveAbility()
	{
		if (PassiveData != null)
		{
			return PassiveData.ScriptName != string.Empty;
		}
		return false;
	}

	public List<EvoMaterialData> GetEnhanceRecipe(int starRating)
	{
		int num = starRating - 1;
		if (num >= EnhanceRecipes.Count)
		{
			return new List<EvoMaterialData>();
		}
		return EnhanceRecipes[num];
	}

	public override string ToString()
	{
		return ID;
	}
}
