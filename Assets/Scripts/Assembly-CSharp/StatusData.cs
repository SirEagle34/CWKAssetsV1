using System;
using System.Collections.Generic;

public class StatusData : ILoadableData
{
	public const int MAX_CANCELS = 5;

	private string _ID;

	private StackType _StackRule;

	private DisplayStackType _DisplayType;

	private GameEvent _EnableMessage;

	private GameEvent _DisableMessage;

	private string _ParamName;

	private string _ParamUnit;

	private StatusType _StatusType;

	private string _InflictedStatus;

	private CreatureFaction _Vulnerability;

	private string _StatusIconSprite;

	private GameEventFXData _FXData;

	public List<string> StatusCancels = new List<string>();

	public string ID => _ID;

	public float FixedValue1 { get; private set; }

	public float FixedValue2 { get; private set; }

	public StackType StackRule => _StackRule;

	public DisplayStackType DisplayType => _DisplayType;

	public GameEvent EnableMessage => _EnableMessage;

	public GameEvent DisableMessage => _DisableMessage;

	public string ParamName => _ParamName;

	public string ParamUnit => _ParamUnit;

	public StatusType StatusType => _StatusType;

	public string InflictedStatus => _InflictedStatus;

	public CreatureFaction Vulnerability => _Vulnerability;

	public string StatusIconSprite => _StatusIconSprite;

	public GameEventFXData FXData => _FXData;

	public bool Instant { get; private set; }

	public Dictionary<string, object> Raw { get; private set; }

	public bool IsData { get; private set; }

	public object DataParams { get; set; }

	public bool IsLandscape { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "StatusName", string.Empty);
		FixedValue1 = TFUtils.LoadFloat(dict, "FixedValue1", 0f);
		FixedValue2 = TFUtils.LoadFloat(dict, "FixedValue2", 0f);
		_StackRule = (StackType)(int)Enum.Parse(typeof(StackType), TFUtils.LoadString(dict, "StackType", "None"), ignoreCase: true);
		_DisplayType = (DisplayStackType)(int)Enum.Parse(typeof(DisplayStackType), TFUtils.LoadString(dict, "DisplayType", "None"), ignoreCase: true);
		_EnableMessage = (GameEvent)(int)Enum.Parse(typeof(GameEvent), TFUtils.LoadString(dict, "EnableMessage", "None"), ignoreCase: true);
		_DisableMessage = (GameEvent)(int)Enum.Parse(typeof(GameEvent), TFUtils.LoadString(dict, "DisableMessage", "None"), ignoreCase: true);
		_StatusType = (StatusType)(int)Enum.Parse(typeof(StatusType), TFUtils.LoadString(dict, "StatusType", "None"), ignoreCase: true);
		_InflictedStatus = TFUtils.LoadString(dict, "InflictStatus", string.Empty);
		_ParamName = TFUtils.LoadString(dict, "ParamName", string.Empty);
		_ParamUnit = TFUtils.LoadString(dict, "ParamUnit", string.Empty);
		_Vulnerability = (CreatureFaction)(int)Enum.Parse(typeof(CreatureFaction), TFUtils.LoadString(dict, "Vulnerability", "Colorless"), ignoreCase: true);
		_StatusIconSprite = TFUtils.LoadString(dict, "StatusIconSprite", string.Empty);
		Instant = TFUtils.LoadBool(dict, "Instant", defaultValue: false);
		StatusCancels.Clear();
		int num = 1;
		while (true)
		{
			string key = "Cancel" + num;
			string text = TFUtils.LoadString(dict, key, string.Empty);
			if (string.IsNullOrEmpty(text))
			{
				break;
			}
			StatusCancels.Add(text);
			num++;
		}
		Raw = dict;
		IsData = dict.ContainsKey("Behaviors");
		DataParams = null;
		if (IsData)
		{
			_EnableMessage = GameEvent.ENABLE_DATASTATUS;
			_DisableMessage = GameEvent.DISABLE_DATASTATUS;
		}
		string text2 = TFUtils.LoadString(dict, "FXRow", string.Empty);
		string enableString = ((text2 != string.Empty) ? text2.ToLower() : EnableMessage.ToString().ToLower());
		_FXData = GameEventFXDataManager.Instance.Find((GameEventFXData m) => m.ID.ToLower() == enableString);
		IsLandscape = ID == "LandCorn" || ID == "LandPlains" || ID == "LandSand" || ID == "LandNice" || ID == "LandSwamp" || ID == "LandIcy" || ID == "LandMagma";
	}

	public string GetValueString(CreatureState creature, bool verbose)
	{
		StatusState statusState = creature.StatusEffects.Find((StatusState m) => m.Data == this);
		if (statusState == null)
		{
			return string.Empty;
		}
		float value;
		switch (DisplayType)
		{
		case DisplayStackType.Count:
			value = statusState.Count;
			break;
		case DisplayStackType.Turns:
			value = ((StackRule != StackType.Count) ? ((float)statusState.Duration) : ((float)statusState.Count));
			break;
		case DisplayStackType.Percent:
		case DisplayStackType.Amount:
		case DisplayStackType.IntAmount:
			value = statusState.Intensity;
			break;
		default:
			value = 0f;
			break;
		}
		if (verbose)
		{
			string text = DisplayType.Format(value, verbose: true);
			if (text != string.Empty)
			{
				text = "(" + text + ")";
			}
			return text;
		}
		return DisplayType.Format(value);
	}

	public float GetValueAmount(GameMessage message)
	{
		switch (DisplayType)
		{
		case DisplayStackType.Turns:
		case DisplayStackType.Count:
			return message.Amount;
		case DisplayStackType.Percent:
		case DisplayStackType.Amount:
		case DisplayStackType.IntAmount:
			return message.RawAmount;
		default:
			return 0f;
		}
	}
}
