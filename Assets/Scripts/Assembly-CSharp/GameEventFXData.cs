using System;
using System.Collections.Generic;
using UnityEngine;

public class GameEventFXData : ILoadableData
{
	public enum ParticleAttractorPoint
	{
		None = 0,
		Creature = 1,
		MyPortrait = 2
	}

	private string _ID;

	private string _ActionFXPrefab;

	private GameEventFXSpawnPoint _ActionFXSpawnPoint;

	private string _CreatureStatusFXPrefab;

	private bool _ValueDisplay;

	private Color _DisplayTextColor;

	private GameEvent _DisableEvent;

	private string _Anim;

	private string _RezInOut;

	private bool _IgnoreDup;

	private string _SwapModel;

	private bool _FreezeModel;

	private float _ActionFXWaitTime;

	private bool _CanGroup;

	private float _ActionFXGroupedWaitTime;

	public string ID => _ID;

	public string ActionFXPrefab => _ActionFXPrefab;

	public GameEventFXSpawnPoint ActionFXSpawnPoint => _ActionFXSpawnPoint;

	public string CreatureStatusFXPrefab => _CreatureStatusFXPrefab;

	public bool ValueDisplay => _ValueDisplay;

	public Color DisplayTextColor => _DisplayTextColor;

	public GameEvent DisableEvent => _DisableEvent;

	public string Anim => _Anim;

	public string RezInOut => _RezInOut;

	public bool IgnoreDup => _IgnoreDup;

	public string SwapModelPrefab => _SwapModel;

	public bool FreezeModel => _FreezeModel;

	public float ActionFXWaitTime => _ActionFXWaitTime;

	public bool CanGroup => _CanGroup;

	public float ActionFXGroupedWaitTime => _ActionFXGroupedWaitTime;

	public bool DetachStatusFXFromCreature { get; private set; }

	public ParticleAttractorPoint AttractPoint { get; private set; }

	public KeyWordData Keyword { get; set; }

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "ID", string.Empty);
		_ActionFXPrefab = TFUtils.LoadString(dict, "ActionFXPrefab", string.Empty);
		_CreatureStatusFXPrefab = TFUtils.LoadString(dict, "CreatureStatusFXPrefab", string.Empty);
		_ValueDisplay = TFUtils.LoadBool(dict, "ValueDisplay", defaultValue: false);
		_ActionFXWaitTime = TFUtils.LoadFloat(dict, "ActionFXWaitTime", 0f);
		_CanGroup = TFUtils.LoadBool(dict, "CanGroup", defaultValue: false);
		_ActionFXGroupedWaitTime = TFUtils.LoadFloat(dict, "GroupedWaitTime", 0f);
		DetachStatusFXFromCreature = TFUtils.LoadBool(dict, "DetachStatusFXFromCreature", defaultValue: false);
		string text = TFUtils.LoadString(dict, "StatusDisableEvent", string.Empty);
		GameEvent disableEvent = GameEvent.NONE;
		if (text != string.Empty)
		{
			try
			{
				disableEvent = (GameEvent)(int)Enum.Parse(typeof(GameEvent), text);
			}
			catch
			{
				disableEvent = GameEvent.NONE;
			}
		}
		_DisableEvent = disableEvent;
		text = TFUtils.LoadString(dict, "DisplayTextColor", string.Empty);
		Color displayTextColor = Color.white;
		if (text != string.Empty)
		{
			switch (text)
			{
			case "Green":
				displayTextColor = Color.green;
				break;
			case "Red":
				displayTextColor = Color.red;
				break;
			case "White":
				displayTextColor = Color.white;
				break;
			case "Blue":
				displayTextColor = Color.blue;
				break;
			case "Cyan":
				displayTextColor = Color.cyan;
				break;
			case "Grey":
				displayTextColor = Color.gray;
				break;
			case "Magenta":
				displayTextColor = Color.magenta;
				break;
			case "Yellow":
				displayTextColor = Color.yellow;
				break;
			}
		}
		_DisplayTextColor = displayTextColor;
		_Anim = TFUtils.LoadString(dict, "Anim", string.Empty);
		_RezInOut = TFUtils.LoadString(dict, "RezInOut", string.Empty);
		text = TFUtils.LoadString(dict, "ActionFXSpawnPoint", string.Empty);
		if (text != string.Empty)
		{
			_ActionFXSpawnPoint = (GameEventFXSpawnPoint)(int)Enum.Parse(typeof(GameEventFXSpawnPoint), text);
		}
		else
		{
			_ActionFXSpawnPoint = GameEventFXSpawnPoint.None;
		}
		string text2 = TFUtils.LoadString(dict, "ParticleAttractorPoint", null);
		if (text2 != null)
		{
			AttractPoint = (ParticleAttractorPoint)(int)Enum.Parse(typeof(ParticleAttractorPoint), text2);
		}
		_IgnoreDup = TFUtils.LoadBool(dict, "IgnoreDuplicates", defaultValue: false);
		_SwapModel = TFUtils.LoadString(dict, "SwapModel", string.Empty);
		_FreezeModel = TFUtils.LoadBool(dict, "FreezeModel", defaultValue: false);
	}
}
