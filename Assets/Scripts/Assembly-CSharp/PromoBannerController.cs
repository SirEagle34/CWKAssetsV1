using System;
using System.Collections.Generic;
using UnityEngine;

public class PromoBannerController : MonoBehaviour
{
	private enum TweenDirection
	{
		Left = 0,
		Right = 1,
		Down = 2,
		Up = 3
	}

	private const string _TexturePath = "UI/Promo_Banners/";

	private int _BannerIndex = -1;

	private List<PromoBannerData> _PromoBannerData = new List<PromoBannerData>();

	private List<PromoBannerItem> _PromoBannerItems = new List<PromoBannerItem>();

	private Transform[] _BannerItemInSlide = new Transform[2];

	[SerializeField]
	private TweenDirection _TweenDirection = TweenDirection.Right;

	[SerializeField]
	private float _SlideTweenSpeed = 0.3f;

	[SerializeField]
	private float _DelayBetweenSlides = 2f;

	[SerializeField]
	private float _VerticalSlideOffset;

	[SerializeField]
	private UIPanel _Panel;

	[SerializeField]
	private UIWidget[] _SlideWidgets = new UIWidget[2];

	[SerializeField]
	private UITweenController[] _TweenInSlides = new UITweenController[2];

	[SerializeField]
	private UITweenController[] _TweenOutSlides = new UITweenController[2];

	[SerializeField]
	private PromoBannerItem[] _PromoBannerItemPrefab = new PromoBannerItem[0];

	[SerializeField]
	private bool _ShouldLocalizeText = true;

	private bool _initialized;

	private Vector3 fp;

	private Vector3 lp;

	private float dragDistance;

	private List<Vector3> touchPositions = new List<Vector3>();

	private DragType dragDirection;

	private float newDuration = 0.3f;

	public bool IsInitialized()
	{
		return _initialized;
	}

	private void Update()
	{
		if (_PromoBannerData.Count == 0)
		{
			return;
		}
		Touch[] touches = Input.touches;
		for (int i = 0; i < touches.Length; i++)
		{
			Touch touch = touches[i];
			if (touch.phase == TouchPhase.Began)
			{
				cancelAutoSliding();
			}
			if (touch.phase == TouchPhase.Moved)
			{
				touchPositions.Add(touch.position);
				if (touchPositions.Count > 1)
				{
					OnUpdatePosition();
				}
			}
			if (touch.phase == TouchPhase.Ended && touchPositions.Count > 0)
			{
				fp = touchPositions[0];
				lp = touchPositions[touchPositions.Count - 1];
				if ((Mathf.Abs(lp.x - fp.x) > dragDistance || Mathf.Abs(lp.y - fp.y) > dragDistance) && Mathf.Abs(lp.x - fp.x) > Mathf.Abs(lp.y - fp.y))
				{
					if (lp.x > fp.x)
					{
						OnSwipeRight();
					}
					else
					{
						OnSwipeLeft();
					}
				}
				touchPositions.Clear();
				dragDirection = DragType.Center;
				InvokeRepeating("TweenSlides", _DelayBetweenSlides, _DelayBetweenSlides);
				break;
			}
			if (touch.phase == TouchPhase.Ended)
			{
				dragDirection = DragType.Center;
			}
		}
	}

	public void LoadBannerData(EventTemplateData tdata, EventBannersData bannerData)
	{
		PromoBannerData promoBannerData = new PromoBannerData();
		if (!string.IsNullOrEmpty(tdata.TitleText))
		{
			promoBannerData.TitleText = tdata.TitleText;
		}
		if (!string.IsNullOrEmpty(tdata.TitleTextColor))
		{
			promoBannerData.TitleTextColor = convertStringToColor(tdata.TitleTextColor);
		}
		if (!string.IsNullOrEmpty(tdata.TitleTextOutlineColor))
		{
			promoBannerData.TitleTextOutlineColor = convertStringToColor(tdata.TitleTextOutlineColor);
		}
		if (!string.IsNullOrEmpty(tdata.TitleTextWidth))
		{
			promoBannerData.TitleTextWidth = tdata.GetIntTitleTextWidth();
		}
		if (tdata.TemplateCustomData.ContainsKey("ButtonText") && !string.IsNullOrEmpty(tdata.TemplateCustomData["ButtonText"]))
		{
			promoBannerData.ButtonText = tdata.TemplateCustomData["ButtonText"];
		}
		if (!string.IsNullOrEmpty(tdata.MainText))
		{
			promoBannerData.DescriptionText = tdata.MainText;
		}
		if (!string.IsNullOrEmpty(tdata.MainTextColor))
		{
			promoBannerData.DescriptionTextColor = convertStringToColor(tdata.MainTextColor);
		}
		if (!string.IsNullOrEmpty(tdata.MainTextOutlineColor))
		{
			promoBannerData.DescriptionTextOutlineColor = convertStringToColor(tdata.MainTextOutlineColor);
		}
		if (!string.IsNullOrEmpty(tdata.MainTextWidth))
		{
			promoBannerData.DescriptionTextWidth = tdata.GetIntMainTextWidth();
		}
		if (!string.IsNullOrEmpty(tdata.SubText))
		{
			promoBannerData.CurrencyText = tdata.SubText;
		}
		if (!string.IsNullOrEmpty(tdata.SubTextColor))
		{
			promoBannerData.CurrencyTextColor = convertStringToColor(tdata.SubTextColor);
		}
		if (!string.IsNullOrEmpty(tdata.SubTextOutlineColor))
		{
			promoBannerData.CurrencyTextOutlineColor = convertStringToColor(tdata.SubTextOutlineColor);
		}
		if (!string.IsNullOrEmpty(tdata.SubTextWidth))
		{
			promoBannerData.CurrencyTextWidth = tdata.GetIntSubTextWidth();
		}
		if (tdata.TemplateCustomData.ContainsKey("CurrencyType") && !string.IsNullOrEmpty(tdata.TemplateCustomData["CurrencyType"]))
		{
			promoBannerData.CurrencyType = convertStringToCurrencyType(tdata.TemplateCustomData["CurrencyType"]);
		}
		if (tdata.TemplateCustomData.ContainsKey("ButtonPlacement") && !string.IsNullOrEmpty(tdata.TemplateCustomData["ButtonPlacement"]))
		{
			promoBannerData.ButtonPlacement = convertStringToButtonPlacement(tdata.TemplateCustomData["ButtonPlacement"]);
		}
		if (tdata.TemplateCustomData.ContainsKey("ButtonColor") && !string.IsNullOrEmpty(tdata.TemplateCustomData["ButtonColor"]))
		{
			promoBannerData.ButtonColor = convertStringToColor(tdata.TemplateCustomData["ButtonColor"]);
		}
		if (!string.IsNullOrEmpty(tdata.BackgroundImage))
		{
			promoBannerData.BgTextureName = "UI/Promo_Banners/" + tdata.BackgroundImage;
		}
		if (tdata.TemplateCustomData.ContainsKey("delegate") && !string.IsNullOrEmpty(tdata.TemplateCustomData["delegate"]))
		{
			promoBannerData.OnClickedDelegate = convertStringToDelegate(tdata.TemplateCustomData["delegate"], promoBannerData);
		}
		promoBannerData.BannerType = convertStringToBannerItemType(tdata.Prefab);
		promoBannerData.BannerEndDate = getEndDateTime(bannerData);
		promoBannerData.BannerID = tdata.ID;
		promoBannerData.ShouldLocalizeText = _ShouldLocalizeText;
		_PromoBannerData.Add(promoBannerData);
	}

	public DateTime getEndDateTime(EventBannersData data)
	{
		bool flag = false;
		string[] array = data.EndDate.Split('/');
		if (array.Length < 3)
		{
			flag = true;
		}
		if (flag)
		{
			DayOfWeek dayOfWeek = DateTime.Today.DayOfWeek;
			string[] array2 = data.SpecificDayRepeats.Split(',');
			if (array2.Length == 0)
			{
				return DateTime.Now;
			}
			for (int i = 0; i < array2.Length; i++)
			{
				switch (int.Parse(array2[i]))
				{
				case 1:
					if (dayOfWeek == DayOfWeek.Sunday)
					{
						return DateTime.Today;
					}
					break;
				case 2:
					if (dayOfWeek == DayOfWeek.Monday)
					{
						return DateTime.Today;
					}
					break;
				case 3:
					if (dayOfWeek == DayOfWeek.Tuesday)
					{
						return DateTime.Today;
					}
					break;
				case 4:
					if (dayOfWeek == DayOfWeek.Wednesday)
					{
						return DateTime.Today;
					}
					break;
				case 5:
					if (dayOfWeek == DayOfWeek.Thursday)
					{
						return DateTime.Today;
					}
					break;
				case 6:
					if (dayOfWeek == DayOfWeek.Friday)
					{
						return DateTime.Today;
					}
					break;
				case 7:
					if (dayOfWeek == DayOfWeek.Saturday)
					{
						return DateTime.Today;
					}
					break;
				}
			}
			return DateTime.Now;
		}
		int month = int.Parse(array[0]);
		int day = int.Parse(array[1]);
		return new DateTime(int.Parse(array[2]), month, day);
	}

	private PromoBannerItemType convertStringToBannerItemType(string bannerTypeStr)
	{
		return bannerTypeStr switch
		{
			"PromoBannerItem_Generic_Left" => PromoBannerItemType.PromoBannerItem_Generic_Left, 
			"PromoBannerItem_Generic_Center" => PromoBannerItemType.PromoBannerItem_Generic_Center, 
			"PromoBannerItem_Generic_Right" => PromoBannerItemType.PromoBannerItem_Generic_Right, 
			"PromoBannerItem_DailyDungeon" => PromoBannerItemType.PromoBannerItem_DailyDungeon, 
			"PromoBannerItem_DailyDungeon_Left" => PromoBannerItemType.PromoBannerItem_DailyDungeon_Left, 
			"PromoBannerItem_DailyDungeon_Center" => PromoBannerItemType.PromoBannerItem_DailyDungeon_Center, 
			"PromoBannerItem_DailyDungeon_Right" => PromoBannerItemType.PromoBannerItem_DailyDungeon_Right, 
			"PromoBannerItem_Faction" => PromoBannerItemType.PromoBannerItem_Faction, 
			"PromoBannerItem_Faction_Left" => PromoBannerItemType.PromoBannerItem_Faction_Left, 
			"PromoBannerItem_Faction_Center" => PromoBannerItemType.PromoBannerItem_Faction_Center, 
			"PromoBannerItem_Faction_Right" => PromoBannerItemType.PromoBannerItem_Faction_Right, 
			"PromoBannerItem_Boss" => PromoBannerItemType.PromoBannerItem_Boss, 
			"PromoBannerItem_Boss_Left" => PromoBannerItemType.PromoBannerItem_Boss_Left, 
			"PromoBannerItem_Boss_Center" => PromoBannerItemType.PromoBannerItem_Boss_Center, 
			"PromoBannerItem_Boss_Right" => PromoBannerItemType.PromoBannerItem_Boss_Right, 
			"PromoBannerItem_Creature" => PromoBannerItemType.PromoBannerItem_Creature, 
			"PromoBannerItem_Creature_Left" => PromoBannerItemType.PromoBannerItem_Creature_Left, 
			"PromoBannerItem_Creature_Center" => PromoBannerItemType.PromoBannerItem_Creature_Center, 
			"PromoBannerItem_Creature_Right" => PromoBannerItemType.PromoBannerItem_Creature_Right, 
			"PromoBannerItem_Hero" => PromoBannerItemType.PromoBannerItem_Hero, 
			"PromoBannerItem_Hero_Left" => PromoBannerItemType.PromoBannerItem_Hero_Left, 
			"PromoBannerItem_Hero_Center" => PromoBannerItemType.PromoBannerItem_Hero_Center, 
			"PromoBannerItem_Hero_Right" => PromoBannerItemType.PromoBannerItem_Hero_Right, 
			_ => PromoBannerItemType.PromoBannerItem_Generic, 
		};
	}

	private PromoCurrencyType convertStringToCurrencyType(string str)
	{
		return str switch
		{
			"BattleEnergy" => PromoCurrencyType.BattleEnergy, 
			"Customize" => PromoCurrencyType.Customize, 
			"Hard" => PromoCurrencyType.Hard, 
			"PVPCurrency" => PromoCurrencyType.PVPCurrency, 
			"PVPEnergy" => PromoCurrencyType.PVPEnergy, 
			"Soft" => PromoCurrencyType.Soft, 
			_ => PromoCurrencyType.None, 
		};
	}

	private PromoButtonPlacement convertStringToButtonPlacement(string str)
	{
		return str switch
		{
			"Right" => PromoButtonPlacement.Right, 
			"Left" => PromoButtonPlacement.Left, 
			"Center" => PromoButtonPlacement.Center, 
			_ => PromoButtonPlacement.None, 
		};
	}

	private Color convertStringToColor(string str)
	{
		switch (str)
		{
		case "Red":
			return Color.red;
		case "Blue":
			return Color.blue;
		case "Green":
			return Color.green;
		case "White":
			return Color.white;
		case "Black":
			return Color.black;
		case "Yellow":
			return Color.yellow;
		default:
		{
			string[] array = str.Trim().Split(' ');
			if (array.Length == 3)
			{
				byte r = byte.Parse(array[0]);
				byte g = byte.Parse(array[1]);
				byte b = byte.Parse(array[2]);
				byte a = byte.MaxValue;
				return new Color32(r, g, b, a);
			}
			if (array.Length == 4)
			{
				byte r2 = byte.Parse(array[0]);
				byte g2 = byte.Parse(array[1]);
				byte b2 = byte.Parse(array[2]);
				byte a2 = byte.Parse(array[3]);
				return new Color32(r2, g2, b2, a2);
			}
			return Color.green;
		}
		}
	}

	private Action convertStringToDelegate(string str, PromoBannerData promoData)
	{
		return str switch
		{
			"OpenGacha" => delegate
			{
				OnSwipeRight();
				Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Gacha").GetComponent<TownBuildingScript>().SendMessage("OnClick");
			}, 
			"OpenDungeons" => delegate
			{
				OnSwipeRight();
				Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Dungeon").GetComponent<TownBuildingScript>().SendMessage("OnClick");
			}, 
			"OpenStoreHeroes" => delegate
			{
				OnSwipeRight();
				Singleton<StoreScreenController>.Instance.OpenToHeroes = true;
				Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Store").GetComponent<TownBuildingScript>().SendMessage("OnClick");
			}, 
			"OpenCustomization" => delegate
			{
				OnSwipeRight();
				Singleton<StoreScreenController>.Instance.OpenToCustomization = true;
				Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Store").GetComponent<TownBuildingScript>().SendMessage("OnClick");
			}, 
			"OpenExpeditions" => delegate
			{
				OnSwipeRight();
				Singleton<ExpeditionStartController>.Instance.Show();
			}, 
			"OpenLabAwakenings" => delegate
			{
				OnSwipeRight();
				Singleton<LabBuildingController>.Instance.TabToJumpTo = "Awaken";
				Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Lab").GetComponent<TownBuildingScript>().SendMessage("OnClick");
			}, 
			"OpenPVP" => delegate
			{
				OnSwipeRight();
				Singleton<TownController>.Instance.GetBuildingObject("TBuilding_PVP").GetComponent<TownBuildingScript>().SendMessage("OnClick");
			}, 
			_ => GenericAction(str), 
		};
	}

	private Action GenericAction(string str)
	{
		string arg = ((str != null && str.Contains(":")) ? str.Substring(str.IndexOf(':') + 1) : string.Empty);
		if (str != null && str.StartsWith("OpenBuilding:"))
		{
			return delegate
			{
				GameObject buildingObject = Singleton<TownController>.Instance.GetBuildingObject(arg);
				TownBuildingScript townBuildingScript = ((buildingObject != null) ? buildingObject.GetComponent<TownBuildingScript>() : null);
				if (townBuildingScript != null)
				{
					OnSwipeRight();
					townBuildingScript.SendMessage("OnClick");
				}
			};
		}
		if (str == "OpenTrade" || str == "OpenRoulette" || str == "OpenCraft")
		{
			return delegate
			{
				OnSwipeRight();
				TownHudController instance = Singleton<TownHudController>.Instance;
				if (str == "OpenTrade")
				{
					instance.TradeRoomButtonPress();
				}
				else if (str == "OpenRoulette")
				{
					instance.DailyRouletteButtonPress();
				}
				else
				{
					instance.CraftRoomButtonPress();
				}
			};
		}
		if (str != null && str.StartsWith("OpenURL:") && arg.StartsWith("https://"))
		{
			return delegate
			{
				Application.OpenURL(arg);
			};
		}
		return delegate
		{
		};
	}

	public void Init()
	{
		if (_PromoBannerData.Count != 0)
		{
			for (int i = 0; i < _PromoBannerData.Count; i++)
			{
				PromoBannerData promoBannerData = _PromoBannerData[i];
				GameObject obj = base.transform.InstantiateAsChild(_PromoBannerItemPrefab[(int)promoBannerData.BannerType].gameObject);
				obj.SetActive(value: false);
				obj.transform.localScale = Vector3.one;
				obj.name = promoBannerData.BannerType.ToString() + i + "(clone)";
				PromoBannerItem component = obj.GetComponent<PromoBannerItem>();
				i.ToString();
				component.Init(promoBannerData);
				_PromoBannerItems.Add(component);
			}
			Vector3 vector = Vector3.zero;
			Vector3 to = Vector3.zero;
			Vector3 vector2 = new Vector3(0f, _VerticalSlideOffset, 0f);
			switch (_TweenDirection)
			{
			case TweenDirection.Left:
				vector = new Vector3(_Panel.width + 10f, _VerticalSlideOffset, 0f);
				to = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
				break;
			case TweenDirection.Right:
				vector = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
				to = new Vector3(_Panel.width + 10f, _VerticalSlideOffset, 0f);
				break;
			case TweenDirection.Down:
				vector = new Vector3(0f, _Panel.height + 10f + _VerticalSlideOffset, 0f);
				to = new Vector3(0f, _VerticalSlideOffset - (_Panel.height + 10f), 0f);
				break;
			case TweenDirection.Up:
				vector = new Vector3(0f, _VerticalSlideOffset - (_Panel.height + 10f), 0f);
				to = new Vector3(0f, _Panel.width + 10f + _VerticalSlideOffset, 0f);
				break;
			}
			newDuration = _SlideTweenSpeed;
			_ = _Panel.width;
			for (int j = 0; j < 2; j++)
			{
				TweenPosition component2 = _TweenInSlides[j].GetComponent<TweenPosition>();
				TweenPosition component3 = _TweenOutSlides[j].GetComponent<TweenPosition>();
				component2.from = vector;
				component2.to = vector2;
				component3.from = vector2;
				component3.to = to;
				component2.duration = _SlideTweenSpeed;
				component3.duration = _SlideTweenSpeed;
			}
			_SlideWidgets[0].transform.localPosition = vector2;
			_SlideWidgets[1].transform.localPosition = vector;
			ParentNextBannerToNewSlideWidget(previousSlide: false);
			InvokeRepeating("TweenSlides", _DelayBetweenSlides, _DelayBetweenSlides);
			_initialized = true;
		}
	}

	private void OnSwipeRight()
	{
		ParentNextBannerToNewSlideWidget(previousSlide: false);
		_TweenOutSlides[0].Play();
		_TweenInSlides[1].Play();
	}

	private void OnSwipeLeft()
	{
		ParentNextBannerToNewSlideWidget(previousSlide: true);
		_TweenOutSlides[0].Play();
		_TweenInSlides[1].Play();
	}

	private void OnUpdatePosition()
	{
		Vector3 vector = touchPositions[0];
		Vector3 vector2 = touchPositions[touchPositions.Count - 1];
		float num = Mathf.Abs(vector2.x - vector.x);
		DragType dragType = dragDirection;
		if (vector2.x < vector.x)
		{
			dragDirection = DragType.Left;
		}
		else
		{
			dragDirection = DragType.Right;
		}
		Vector3 vector3 = new Vector3(0f, _VerticalSlideOffset, 0f);
		TweenPosition component = _TweenOutSlides[0].GetComponent<TweenPosition>();
		TweenPosition component2 = _TweenInSlides[1].GetComponent<TweenPosition>();
		if (dragType != dragDirection)
		{
			if (dragDirection == DragType.Left)
			{
				_SlideWidgets[0].transform.localPosition = vector3;
				_SlideWidgets[1].transform.localPosition = new Vector3(_Panel.width + 10f, _VerticalSlideOffset, 0f);
				component.from = vector3;
				component.to = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
				component2.to = vector3;
				component2.from = new Vector3(_Panel.width + 10f, _VerticalSlideOffset, 0f);
				int bannerIndex = _BannerIndex;
				bannerIndex--;
				if (bannerIndex < 0)
				{
					bannerIndex = _PromoBannerItems.Count - 1;
				}
				if (_BannerItemInSlide[0] != null)
				{
					_BannerItemInSlide[0].gameObject.SetActive(value: false);
					_BannerItemInSlide[0].parent = null;
				}
				if (_BannerItemInSlide[1] != null)
				{
					_BannerItemInSlide[1].gameObject.SetActive(value: false);
					_BannerItemInSlide[1].parent = null;
				}
				_BannerItemInSlide[1] = _PromoBannerItems[_BannerIndex].transform;
				_BannerItemInSlide[1].parent = _SlideWidgets[0].transform;
				_BannerItemInSlide[1].localPosition = Vector3.zero;
				_BannerItemInSlide[1].gameObject.SetActive(value: true);
				_BannerItemInSlide[0] = _PromoBannerItems[bannerIndex].transform;
				_BannerItemInSlide[0].parent = _SlideWidgets[1].transform;
				_BannerItemInSlide[0].localPosition = Vector3.zero;
				_BannerItemInSlide[0].gameObject.SetActive(value: true);
			}
			else
			{
				_SlideWidgets[0].transform.localPosition = vector3;
				_SlideWidgets[1].transform.localPosition = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
				component.from = vector3;
				component.to = new Vector3(_Panel.width + 10f, _VerticalSlideOffset, 0f);
				component2.to = vector3;
				component2.from = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
				int bannerIndex2 = _BannerIndex;
				bannerIndex2++;
				bannerIndex2 %= _PromoBannerItems.Count;
				if (_BannerItemInSlide[0] != null)
				{
					_BannerItemInSlide[0].gameObject.SetActive(value: false);
					_BannerItemInSlide[0].parent = null;
				}
				if (_BannerItemInSlide[1] != null)
				{
					_BannerItemInSlide[1].gameObject.SetActive(value: false);
					_BannerItemInSlide[1].parent = null;
				}
				_BannerItemInSlide[1] = _PromoBannerItems[_BannerIndex].transform;
				_BannerItemInSlide[1].parent = _SlideWidgets[0].transform;
				_BannerItemInSlide[1].localPosition = Vector3.zero;
				_BannerItemInSlide[1].gameObject.SetActive(value: true);
				_BannerItemInSlide[0] = _PromoBannerItems[bannerIndex2].transform;
				_BannerItemInSlide[0].parent = _SlideWidgets[1].transform;
				_BannerItemInSlide[0].localPosition = Vector3.zero;
				_BannerItemInSlide[0].gameObject.SetActive(value: true);
			}
		}
		component.value = Vector3.Lerp(component.from, component.to, 0.001f * num);
		component2.value = Vector3.Lerp(component2.from, component2.to, 0.001f * num);
		newDuration = _SlideTweenSpeed * (1f - 0.001f * num);
	}

	private void cancelAutoSliding()
	{
		CancelInvoke();
	}

	private void TweenSlides()
	{
		Vector3 vector = new Vector3(0f, _VerticalSlideOffset, 0f);
		TweenPosition component = _TweenOutSlides[0].GetComponent<TweenPosition>();
		TweenPosition component2 = _TweenInSlides[1].GetComponent<TweenPosition>();
		_SlideWidgets[0].transform.localPosition = vector;
		_SlideWidgets[1].transform.localPosition = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
		component.from = vector;
		component.to = new Vector3(_Panel.width + 10f, _VerticalSlideOffset, 0f);
		component2.to = vector;
		component2.from = new Vector3(0f - (_Panel.width + 10f), _VerticalSlideOffset, 0f);
		_ = (_BannerIndex + 1) % _PromoBannerItems.Count;
		ParentNextBannerToNewSlideWidget(previousSlide: false);
		_TweenOutSlides[0].Play();
		_TweenInSlides[1].Play();
	}

	public void CheckBannerStatus()
	{
		int i;
		for (i = 0; i < _PromoBannerData.Count; i++)
		{
			EventTemplateData eventTemplateData = EventTemplateDataManager.Instance.GetDatabase().Find((EventTemplateData m) => m.ID == _PromoBannerData[i].BannerID);
			if (eventTemplateData != null && !EventTemplateDataManager.Instance.CheckEventCondition(eventTemplateData))
			{
				_PromoBannerData.RemoveAt(i);
				_PromoBannerItems.RemoveAt(i);
			}
		}
	}

	private void ParentNextBannerToNewSlideWidget(bool previousSlide)
	{
		if (_PromoBannerItems.Count <= 0)
		{
			return;
		}
		TweenPosition component = _TweenOutSlides[0].GetComponent<TweenPosition>();
		TweenPosition component2 = _TweenInSlides[1].GetComponent<TweenPosition>();
		component.from = component.value;
		component2.from = component2.value;
		component2.duration = newDuration;
		component.duration = newDuration;
		int num = _BannerIndex;
		if (num == -1)
		{
			num = _PromoBannerItems.Count - 1;
		}
		if (previousSlide)
		{
			_BannerIndex--;
			if (_BannerIndex < 0)
			{
				_BannerIndex = _PromoBannerItems.Count - 1;
			}
		}
		else
		{
			_BannerIndex++;
			_BannerIndex %= _PromoBannerItems.Count;
		}
		if (_BannerItemInSlide[0] != null)
		{
			_BannerItemInSlide[0].gameObject.SetActive(value: false);
			_BannerItemInSlide[0].parent = null;
		}
		if (_BannerItemInSlide[1] != null)
		{
			_BannerItemInSlide[1].gameObject.SetActive(value: false);
			_BannerItemInSlide[1].parent = null;
		}
		_BannerItemInSlide[1] = _PromoBannerItems[num].transform;
		_BannerItemInSlide[1].parent = _SlideWidgets[0].transform;
		_BannerItemInSlide[1].localPosition = Vector3.zero;
		_BannerItemInSlide[1].gameObject.SetActive(value: true);
		_BannerItemInSlide[0] = _PromoBannerItems[_BannerIndex].transform;
		_BannerItemInSlide[0].parent = _SlideWidgets[1].transform;
		_BannerItemInSlide[0].localPosition = Vector3.zero;
		_BannerItemInSlide[0].gameObject.SetActive(value: true);
		newDuration = _SlideTweenSpeed;
	}
}
