using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShapeShiftShots : MonoBehaviour
{
	// Keeps clone-shot selection separate from the existing ShapeShift diagnostic flow.
	public static bool UsesCloneShotReference(CardData card)
	{
		return card != null && card.Clone;
	}
	private const string CardId = "SPOOKY_0050";

	private string mDir;

	private readonly StringBuilder mLog = new StringBuilder();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-shapeshots") >= 0)
		{
			Application.runInBackground = true;
			GameObject obj = new GameObject("ShapeShiftShots");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<ShapeShiftShots>();
		}
	}

	private IEnumerator Start()
	{
		mDir = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(mDir);
		string[] files = Directory.GetFiles(mDir, "ss_*");
		for (int i = 0; i < files.Length; i++)
		{
			File.Delete(files[i]);
		}
		File.Delete(Path.Combine(mDir, "done.txt"));
		int i2 = 0;
		while (UnityEngine.Object.FindObjectOfType<TownController>() == null)
		{
			if (i2 % 30 == 0)
			{
				Log("waiting for town: scene " + SceneManager.GetActiveScene().name + " flow " + DetachedSingleton<SceneFlowManager>.Instance.GetCurrentScene());
				ScreenCapture.CaptureScreenshot(Path.Combine(mDir, "ss_boot_" + i2 + ".png"));
			}
			yield return new WaitForSeconds(1f);
			i2++;
		}
		yield return new WaitForSeconds(12f);
		PlayerInfoScript info = Singleton<PlayerInfoScript>.Instance;
		long realSpin = info.SaveData.LastDailySpinTime;
		info.SaveData.LastDailySpinTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(Shot("ss_spin_town_cooldown"));
		TownHudController hud = UnityEngine.Object.FindObjectOfType<TownHudController>();
		if (hud != null)
		{
			hud.DailyRouletteButtonPress();
			yield return new WaitForSeconds(2.5f);
			yield return StartCoroutine(Shot("ss_spin_wheel_cooldown"));
			Log("spin town label: " + ((hud.DailyRouletteLevelRequirementLabel != null) ? (hud.DailyRouletteLevelRequirementLabel.gameObject.activeInHierarchy + " '" + hud.DailyRouletteLevelRequirementLabel.text + "'") : "none"));
			info.SaveData.LastDailySpinTime = 0L;
			yield return new WaitForSeconds(1.5f);
			yield return StartCoroutine(Shot("ss_spin_wheel_ready"));
			DailyGiftRouletteContoller dailyGiftRouletteContoller = UnityEngine.Object.FindObjectOfType<DailyGiftRouletteContoller>();
			if (dailyGiftRouletteContoller != null && dailyGiftRouletteContoller.DailyRouletteHide != null)
			{
				dailyGiftRouletteContoller.DailyRouletteHide.Play();
			}
			yield return new WaitForSeconds(1.5f);
			yield return StartCoroutine(Shot("ss_spin_town_ready"));
		}
		info.SaveData.LastDailySpinTime = realSpin;
		InventorySlotItem grimSlot = null;
		foreach (InventorySlotItem inventorySlot in info.SaveData.InventorySlots)
		{
			if (inventorySlot != null && inventorySlot.Creature != null && inventorySlot.Creature.Form.ID == "Grim_Obsidian")
			{
				grimSlot = inventorySlot;
			}
		}
		if (grimSlot == null)
		{
			Finish("FAILED: this account has no Grim_Special");
			yield break;
		}
		QuestData questData = null;
		foreach (QuestData item in QuestDataManager.Instance.GetDatabase())
		{
			if (questData == null && item.League != null && item.League.QuestLine == QuestLineEnum.Main)
			{
				questData = item;
			}
		}
		Loadout loadout = info.GetCurrentLoadout().Copy();
		loadout.CreatureSet.RemoveAll((InventorySlotItem s) => s != null && s.Creature == grimSlot.Creature);
		loadout.CreatureSet.Insert(0, grimSlot);
		info.StateData.CurrentActiveQuest = questData;
		info.StateData.CurrentLoadout = loadout;
		info.StateData.SelectedHelper = null;
		Log("quest " + ((questData == null) ? "default" : questData.ID) + ", team " + loadout.CreatureSet.Count);
		DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
		DWGame game = null;
		for (i2 = 0; i2 < 180; i2++)
		{
			if (!(game == null) && game.GetCurrentGameState() == GameState.P1Turn)
			{
				break;
			}
			yield return new WaitForSeconds(1f);
			game = Singleton<DWGame>.Instance;
		}
		if (game == null || game.GetCurrentGameState() != GameState.P1Turn)
		{
			Finish("FAILED: no player turn in 180s");
			yield break;
		}
		yield return new WaitForSeconds(3f);
		PlayerState user = game.CurrentBoardState.GetPlayerState(PlayerType.User);
		PlayerState opp = game.CurrentBoardState.GetPlayerState(PlayerType.Opponent);
		yield return StartCoroutine(Shot("ss_00_turn1"));
		if (FindGrim(user) == null)
		{
			game.DeployCreatureFromCard(PlayerType.User, grimSlot.Creature, 0);
			yield return new WaitForSeconds(4f);
		}
		yield return StartCoroutine(Shot("ss_01_grim_deployed"));
		game.EndPlayerTurn();
		for (i2 = 0; i2 < 120; i2++)
		{
			if (game.GetCurrentGameState() == GameState.P1Turn && !game.InDeploymentPhase())
			{
				break;
			}
			yield return new WaitForSeconds(1f);
		}
		yield return new WaitForSeconds(3f);
		CreatureState grim = FindGrim(user);
		Log("turn 2: state " + game.GetCurrentGameState().ToString() + " deployment " + game.InDeploymentPhase() + " grim " + ((grim != null) ? (grim.HP + "/" + grim.MaxHP + " lane " + grim.Lane.Index) : "missing"));
		if (grim == null)
		{
			Finish("FAILED: Grim not on the board");
			yield break;
		}
		DescribeBoard();
		Vector3 camBefore = Singleton<DWGameCamera>.Instance.MainCam.transform.position;
		yield return StartCoroutine(Shot("ss_02_turn2"));
		CardData blockedCard = CardDataManager.Instance.GetData("SPOOKY_0041");
		user.PlaceCardInHand(blockedCard);
		Singleton<HandCardController>.Instance.ShowCardDraw(PlayerType.User, blockedCard, grim);
		yield return new WaitForSeconds(2.5f);
		CardPrefabScript cardPrefabScript = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript h) => h.Card == blockedCard);
		Log("blocked card: list any " + CardBlocklist.Any + ", CanPlay " + game.CanPlay(PlayerType.User, blockedCard, PlayerType.User, grim.Lane.Index).ToString() + ", dimmed " + (cardPrefabScript != null && cardPrefabScript.ShapeShiftDimmed));
		yield return StartCoroutine(Shot("ss_02b_blocked_card"));
		CardData card = CardDataManager.Instance.GetData("SPOOKY_0050");
		user.PlaceCardInHand(card);
		Singleton<HandCardController>.Instance.ShowCardDraw(PlayerType.User, card, grim);
		yield return new WaitForSeconds(3f);
		user.ActionPoints = 20;
		CardPrefabScript handCard = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript h) => h.Card == card);
		Log("hand card found " + (handCard != null) + ", dimmed " + (handCard != null && handCard.ShapeShiftDimmed) + " at " + grim.HP + "/" + grim.MaxHP);
		yield return StartCoroutine(Shot("ss_03_hand_bright"));
		int fullHp = grim.HP;
		grim.SetHP((int)((float)grim.MaxHP * 0.5f));
		yield return new WaitForSeconds(1f);
		Log("at 50% HP dimmed " + (handCard != null && handCard.ShapeShiftDimmed) + ", can play on Grim " + game.CanPlay(PlayerType.User, card, PlayerType.User, grim.Lane.Index));
		yield return StartCoroutine(Shot("ss_04_hand_dim"));
		grim.SetHP(fullHp);
		yield return new WaitForSeconds(1f);
		Log("back to full: dimmed " + (handCard != null && handCard.ShapeShiftDimmed));
		Log("offer, fresh board: " + Offer(ShapeShiftAdvisor.Choose(grim, card.ShapeShiftPool, 5)));
		foreach (LaneState lane in opp.Lanes)
		{
			if (lane.Creature != null && lane.Creature.HP > 0)
			{
				lane.Creature.SetHP((int)((float)lane.Creature.MaxHP * 0.5f));
			}
		}
		Log("offer, enemies at 50%: " + Offer(ShapeShiftAdvisor.Choose(grim, card.ShapeShiftPool, 5)));
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(grim);
		Singleton<DWBattleLane>.Instance.PlayUserActionCard(handCard, laneObject);
		handCard.StartCoroutine(handCard.ShowPlayAnim());
		float t0 = Time.realtimeSinceStartup;
		float[] array = new float[5] { 0.3f, 0.7f, 1.2f, 1.9f, 2.6f };
		foreach (float num in array)
		{
			yield return StartCoroutine(ShotAt("ss_1" + (int)(num * 10f) + "_closeup", t0 + num));
		}
		Log("cinematic: " + ShapeShiftCinematic.Instance.LastLog);
		yield return new WaitForSeconds(1f);
		yield return StartCoroutine(Shot("ss_2_picker"));
		IList<string> offeredIds = ShapeShiftCinematic.Instance.OfferedIds;
		Log("offered: " + string.Join(", ", new List<string>(offeredIds).ToArray()));
		int index = Mathf.Max(0, new List<string>(offeredIds).IndexOf("ZombiePieclops_Awaken"));
		ShapeShiftCinematic.Instance.Pick(index, force: true);
		t0 = Time.realtimeSinceStartup;
		array = new float[13]
		{
			0.2f, 0.5f, 0.9f, 1.3f, 1.8f, 2.4f, 2.9f, 3.3f, 3.8f, 4.5f,
			5.5f, 6.5f, 8f
		};
		foreach (float num2 in array)
		{
			yield return StartCoroutine(ShotAt("ss_3" + ((int)(num2 * 10f)).ToString("D2") + "_shift", t0 + num2));
		}
		CreatureState creature = user.GetCreature(grim.Lane.Index);
		Log("after: lane " + grim.Lane.Index + " is " + ((creature != null) ? (creature.Data.Form.ID + " " + creature.HP + "/" + creature.MaxHP) : "empty") + " | " + ShapeShiftCinematic.Instance.LastLog + " | camera fov " + Singleton<DWGameCamera>.Instance.MainCam.fieldOfView);
		if (creature != null)
		{
			string text = "";
			foreach (AbilityState ability in creature.Abilities)
			{
				text = text + ability.GetType().Name + " ";
			}
			string text2 = "";
			foreach (LaneState lane2 in opp.Lanes)
			{
				if (lane2.Creature != null)
				{
					text2 = text2 + lane2.Creature.Data.Form.ID + (lane2.Creature.StatusEffects.Exists((StatusState st) => st is Poison) ? " CURSED " : " clean ");
				}
			}
			Log("passive after shift: [" + text + "] enemies: " + text2);
		}
		yield return new WaitForSeconds(2f);
		yield return StartCoroutine(Shot("ss_4_after"));
		Vector3 position = Singleton<DWGameCamera>.Instance.MainCam.transform.position;
		Log("camera before " + camBefore.ToString("F2") + " after " + position.ToString("F2") + " moved " + Vector3.Distance(camBefore, position).ToString("F2") + " fov " + Singleton<DWGameCamera>.Instance.MainCam.fieldOfView);
		CreatureState creatureState = null;
		foreach (LaneState lane3 in opp.Lanes)
		{
			if (creatureState == null && lane3.Creature != null && lane3.Creature.HP > 0)
			{
				creatureState = lane3.Creature;
			}
		}
		if (creatureState != null)
		{
			opp.ShapeShift(creatureState, "SnowSaurusRex_Base");
			t0 = Time.realtimeSinceStartup;
			array = new float[7] { 0.6f, 1.2f, 2f, 2.6f, 3.4f, 4.5f, 6.5f };
			foreach (float num3 in array)
			{
				yield return StartCoroutine(ShotAt("ss_5" + ((int)(num3 * 10f)).ToString("D2") + "_remote", t0 + num3));
			}
			Log("remote: " + ShapeShiftCinematic.Instance.LastLog);
		}
		Finish("OK");
	}

	private static string Offer(List<ShapeShiftAdvisor.Option> options)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ShapeShiftAdvisor.Option option in options)
		{
			stringBuilder.Append(option.Id).Append(" [").Append(option.Role)
				.Append(' ')
				.Append(option.Score.ToString("0.00"))
				.Append("] ")
				.Append(option.Why)
				.Append(" | ");
		}
		return stringBuilder.ToString();
	}

	private static CreatureState FindGrim(PlayerState p)
	{
		foreach (LaneState lane in p.Lanes)
		{
			if (lane.Creature != null && lane.Creature.Data.Form.ID == "Grim_Special")
			{
				return lane.Creature;
			}
		}
		return null;
	}

	private void DescribeBoard()
	{
		DWGameCamera instance = Singleton<DWGameCamera>.Instance;
		Log("camera " + instance.MainCam.transform.position.ToString("F1") + " look " + instance.MainCamLookAt.transform.position.ToString("F1") + " fov " + instance.MainCam.fieldOfView + " screen " + Screen.width + "x" + Screen.height);
		for (int i = 0; i < 2; i++)
		{
			foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[i])
			{
				GameObject creatureObject = item.CreatureObject;
				Log("  side " + i + " " + item.Creature.Data.Form.ID + " lane pos " + item.transform.position.ToString("F1") + " fwd " + item.transform.forward.ToString("F2") + ((creatureObject != null) ? (" body fwd " + creatureObject.transform.forward.ToString("F2")) : ""));
			}
		}
	}

	private IEnumerator ShotAt(string name, float realTime)
	{
		while (Time.realtimeSinceStartup < realTime)
		{
			yield return null;
		}
		ScreenCapture.CaptureScreenshot(Path.Combine(mDir, name + ".png"));
		yield return null;
	}

	private IEnumerator Shot(string name)
	{
		yield return new WaitForSeconds(1f);
		ScreenCapture.CaptureScreenshot(Path.Combine(mDir, name + ".png"));
		yield return new WaitForSeconds(0.5f);
	}

	private void Log(string line)
	{
		mLog.AppendLine($"{Time.realtimeSinceStartup,7:0.0}s {line}");
		File.WriteAllText(Path.Combine(mDir, "ss_log.txt"), mLog.ToString());
	}

	private void Finish(string result)
	{
		Log("RESULT: " + result);
		File.WriteAllText(Path.Combine(mDir, "done.txt"), result);
	}
}
