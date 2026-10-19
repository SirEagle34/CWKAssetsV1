using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GraveyardShots : MonoBehaviour
{
	private const string CardId = "Leader_MarshallLee_01";

	private string mDir;

	private readonly StringBuilder mLog = new StringBuilder();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-graveshots") >= 0)
		{
			Application.runInBackground = true;
			GameObject obj = new GameObject("GraveyardShots");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<GraveyardShots>();
		}
	}

	private IEnumerator Start()
	{
		mDir = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(mDir);
		string[] files = Directory.GetFiles(mDir, "gs_*");
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
				Log("waiting for town: scene " + SceneManager.GetActiveScene().name);
				ScreenCapture.CaptureScreenshot(Path.Combine(mDir, "gs_boot_" + i2 + ".png"));
			}
			yield return new WaitForSeconds(1f);
			i2++;
		}
		yield return new WaitForSeconds(10f);
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		LeaderData data = LeaderDataManager.Instance.GetData("Leader_MarshallLee");
		CardData card = CardDataManager.Instance.GetData("Leader_MarshallLee_02");
		Log("data: leader " + (data != null) + " prefab " + ((data != null) ? data.Prefab : "-") + ", card " + (card != null) + " graveyard " + (card != null && card.Graveyard) + " script " + ((card != null) ? card.PlayScript : "-"));
		if (data == null || card == null)
		{
			Finish("FAILED: Marshall pack data missing (launch with -content staging)");
			yield break;
		}
		for (int j = 1; j <= 5; j++)
		{
			CardData data2 = CardDataManager.Instance.GetData("Leader_MarshallLee_0" + j);
			Log("  card " + j + ": " + ((data2 == null) ? "missing" : (KFFLocalization.Get(data2.Name) + " | " + KFFLocalization.Get(data2.Description) + " | frame " + data2.CardFrame + " art " + data2.UITexture)));
		}
		QuestData questData = null;
		foreach (QuestData item in QuestDataManager.Instance.GetDatabase())
		{
			if (questData == null && item.League != null && item.League.QuestLine == QuestLineEnum.Main)
			{
				questData = item;
			}
		}
		Loadout loadout = instance.GetCurrentLoadout().Copy();
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-extreme") >= 0)
		{
			PvpExtremeTeam.Reset();
			if (PvpExtremeTeam.Current == null)
			{
				Finish("FAILED: no Extreme team data");
				yield break;
			}
			loadout = PvpExtremeTeam.Current.Copy();
			Log("extreme team " + PvpExtremeTeam.ActiveIndex() + ": " + string.Join(", ", loadout.CreatureSet.ConvertAll((InventorySlotItem it) => it.Creature.Form.ID).ToArray()));
		}
		loadout.Leader = new LeaderItem(data);
		string addId = null;
		int num = Array.IndexOf(Environment.GetCommandLineArgs(), "-addcreature");
		if (num >= 0 && num + 1 < Environment.GetCommandLineArgs().Length)
		{
			addId = Environment.GetCommandLineArgs()[num + 1];
			CreatureData data3 = CreatureDataManager.Instance.GetData(addId);
			if (data3 == null)
			{
				Finish("FAILED: no creature " + addId + " (pack data deployed? -content staging?)");
				yield break;
			}
			CreatureItem creatureItem = new CreatureItem(data3);
			creatureItem.StarRating = Mathf.Max(1, data3.Rarity);
			creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(creatureItem.MaxLevel);
			loadout.CreatureSet.Insert(0, new InventorySlotItem(creatureItem));
			while (loadout.CreatureSet.Count > MiscParams.CreaturesOnTeam)
			{
				loadout.CreatureSet.RemoveAt(loadout.CreatureSet.Count - 1);
			}
			Log("added " + addId + ": HP " + creatureItem.HP + " STR " + creatureItem.STR + " INT " + creatureItem.INT + " cost " + data3.DeployCost.ToString() + " cards " + string.Join(",", data3.ActionCards.Select((CardData cd) => (cd != null) ? cd.ID : "-").ToArray()) + " passive " + ((data3.PassiveData != null) ? data3.PassiveData.ID : "-"));
		}
		instance.StateData.CurrentActiveQuest = questData;
		instance.StateData.CurrentLoadout = loadout;
		instance.StateData.SelectedHelper = null;
		Log("quest " + ((questData == null) ? "default" : questData.ID) + ", team " + loadout.CreatureSet.Count);
		DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
		float tb = Time.realtimeSinceStartup;
		for (i2 = 0; i2 < 14; i2++)
		{
			yield return StartCoroutine(ShotAt("gs_0" + i2.ToString("D2") + "_intro", tb + 6f + (float)i2 * 1.2f));
		}
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
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		int graves = 2;
		int num2 = Array.IndexOf(commandLineArgs, "-graves");
		if (num2 >= 0 && num2 + 1 < commandLineArgs.Length)
		{
			int.TryParse(commandLineArgs[num2 + 1], out graves);
		}
		graves = Mathf.Clamp(graves, 1, loadout.CreatureSet.Count - 1);
		PlayerState user = game.CurrentBoardState.GetPlayerState(PlayerType.User);
		PlayerState opp = game.CurrentBoardState.GetPlayerState(PlayerType.Opponent);
		for (i2 = 0; i2 < ((graves > 2) ? MiscParams.CreaturesOnBoard : 3); i2++)
		{
			if (user.DeploymentList.Count <= 0)
			{
				break;
			}
			CreatureItem c = user.DeploymentList[0].Data;
			CardPrefabScript cardPrefabScript = Singleton<HandCardController>.Instance.GetCreatureCards().Find((CardPrefabScript h) => h.Creature == c);
			game.DeployCreatureFromCard(PlayerType.User, c, i2);
			if (cardPrefabScript != null)
			{
				cardPrefabScript.StartCoroutine(cardPrefabScript.ShowPlayAnim());
			}
			yield return new WaitForSeconds(3f);
		}
		Log("creature hand cards after deploy: " + Singleton<HandCardController>.Instance.GetCreatureCards().Count);
		yield return StartCoroutine(Shot("gs_10_deployed"));
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
		Log("turn 2: on board " + user.GetCreatureCount() + ", in hand " + user.DeploymentList.Count + ", fallen " + user.RaisableDead().Count);
		if (addId != null)
		{
			CreatureState creature = user.Lanes[0].Creature;
			CreatureState target = null;
			foreach (LaneState lane in opp.Lanes)
			{
				if (target == null && lane.Creature != null && lane.Creature.HP > 0)
				{
					target = lane.Creature;
				}
			}
			if (creature == null || creature.Data.Form.ID != addId || target == null)
			{
				Finish("FAILED: lane 0 " + ((creature == null) ? "empty" : creature.Data.Form.ID) + ", enemy " + ((target == null) ? "none" : target.Data.Form.ID));
				yield break;
			}
			Func<CreatureState, string> sts = (CreatureState creatureState2) => string.Join(",", creatureState2.StatusEffects.ConvertAll((StatusState st) => st.Data.ID + ":" + st.Count).ToArray());
			Log("before attack: " + target.Data.Form.ID + " HP " + target.HP + " [" + sts(target) + "], body " + (Singleton<DWBattleLane>.Instance.GetLaneObject(creature) != null));
			user.ActionPoints = 20;
			game.DragAttack(PlayerType.User, 0, target.Lane.Index);
			yield return StartCoroutine(Film("i", 8f));
			Log("after attack: " + target.Data.Form.ID + " HP " + target.HP + " [" + sts(target) + "]");
			Finish("OK");
			yield break;
		}
		if (graves <= 2 && opp.GetCreatureCount() < 2 && opp.DeploymentList.Count > 0)
		{
			game.DeployCreatureFromCard(PlayerType.Opponent, opp.DeploymentList[0].Data, opp.GetCreatureCount());
			yield return new WaitForSeconds(4f);
		}
		CreatureState enemyPayer = null;
		CreatureState creatureState = null;
		foreach (LaneState lane2 in opp.Lanes)
		{
			if (lane2.Creature != null && lane2.Creature.HP > 0)
			{
				if (enemyPayer == null)
				{
					enemyPayer = lane2.Creature;
				}
				else if (creatureState == null)
				{
					creatureState = lane2.Creature;
				}
			}
		}
		if (graves <= 2 && enemyPayer != null && creatureState != null)
		{
			Singleton<DWBattleLane>.Instance.LootCollectAuto = true;
			creatureState.DealDamage(creatureState.HP + 9999, AttackBase.STR, IsDirect: true, null, null);
			yield return new WaitForSeconds(4f);
			string pick = PlayerState.GraveyardPickKey(opp.RaisableDead(), 0);
			opp.RaiseFromGraveyard(opp.FindGraveyardPick(pick), enemyPayer);
			yield return StartCoroutine(Film("c", 19f));
			Log("remote: " + GraveyardCinematic.Instance.LastLog);
			yield return new WaitForSeconds(2f);
		}
		else
		{
			Log("remote: skipped, the enemy has fewer than two creatures");
		}
		user.PlaceCardInHand(card);
		Singleton<HandCardController>.Instance.ShowCardDraw(PlayerType.User, card);
		yield return new WaitForSeconds(3f);
		user.ActionPoints = 20;
		CardPrefabScript handCard = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript h) => h.Card == card);
		CreatureState payer = null;
		foreach (LaneState lane3 in user.Lanes)
		{
			if (payer == null && lane3.Creature != null && lane3.Creature.HP > 0)
			{
				payer = lane3.Creature;
			}
		}
		Log("no fallen: dimmed " + (handCard != null && handCard.ShapeShiftDimmed) + ", CanPlay " + game.CanPlay(PlayerType.User, card, PlayerType.User, payer?.Lane.Index ?? 0));
		yield return StartCoroutine(Shot("gs_11_hand_dim_nobody_fallen"));
		for (i2 = 0; i2 < 6; i2++)
		{
			if (user.RaisableDead().Count >= graves)
			{
				break;
			}
			foreach (LaneState lane4 in user.Lanes)
			{
				CreatureState creature2 = lane4.Creature;
				if (user.RaisableDead().Count < graves && creature2 != null && creature2 != payer && creature2.HP > 0)
				{
					creature2.DealDamage(creature2.HP + 9999, AttackBase.STR, IsDirect: true, null, null);
					yield return new WaitForSeconds(1.5f);
				}
			}
			yield return new WaitForSeconds(4f);
			if (user.RaisableDead().Count >= graves || user.DeploymentList.Count <= 0)
			{
				continue;
			}
			foreach (LaneState item2 in user.Lanes.FindAll((LaneState l) => l.Creature == null))
			{
				if (user.DeploymentList.Count == 0)
				{
					break;
				}
				game.DeployCreatureFromCard(PlayerType.User, user.DeploymentList[0].Data, item2.Index);
				yield return new WaitForSeconds(4f);
			}
		}
		yield return new WaitForSeconds(2f);
		Log("after deaths: on board " + user.GetCreatureCount() + ", fallen " + user.RaisableDead().Count + ", dimmed " + (handCard != null && handCard.ShapeShiftDimmed) + ", CanPlay " + game.CanPlay(PlayerType.User, card, PlayerType.User, payer.Lane.Index));
		yield return StartCoroutine(Shot("gs_12_hand_bright"));
		int handBefore = user.DeploymentList.Count;
		int fallenBefore = user.RaisableDead().Count;
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(payer);
		Singleton<DWBattleLane>.Instance.PlayUserActionCard(handCard, laneObject);
		handCard.StartCoroutine(handCard.ShowPlayAnim());
		yield return StartCoroutine(Film("a", 7.5f + 0.6f * (float)Mathf.Max(0, graves - 2)));
		yield return StartCoroutine(Shot("gs_30_picker"));
		Log("cinematic: " + GraveyardCinematic.Instance.LastLog + " offered " + string.Join(", ", new List<string>(GraveyardCinematic.Instance.OfferedIds).ToArray()));
		GraveyardCinematic.Instance.Pick(0, force: true);
		yield return StartCoroutine(Film("b", 14f));
		bool flag = payer.StatusEffects.Exists((StatusState st) => st is Poison);
		string text = "";
		foreach (LaneState lane5 in user.Lanes)
		{
			if (lane5.Creature != null)
			{
				text = text + lane5.Creature.Data.Form.ID + " " + lane5.Creature.HP + "/" + lane5.Creature.MaxHP + ((Singleton<DWBattleLane>.Instance.GetLaneObject(lane5.Creature) != null) ? " (body)" : " (NO BODY)") + ", ";
			}
		}
		Log("board after raise: " + text);
		Log("creature hand cards after raise: " + Singleton<HandCardController>.Instance.GetCreatureCards().Count + " [" + string.Join(", ", Singleton<HandCardController>.Instance.GetCreatureCards().ConvertAll((CardPrefabScript h) => h.Creature.Form.ID).ToArray()) + "]");
		Log("after raise: hand " + handBefore + " -> " + user.DeploymentList.Count + ", fallen " + fallenBefore + " -> " + user.RaisableDead().Count + ", payer " + payer.Data.Form.ID + (flag ? " CURSED" : " not cursed") + " | " + GraveyardCinematic.Instance.LastLog + " | fov " + Singleton<DWGameCamera>.Instance.MainCam.fieldOfView);
		yield return StartCoroutine(Shot("gs_50_after"));
		Finish("OK");
	}

	private IEnumerator Film(string part, float seconds)
	{
		float t0 = Time.realtimeSinceStartup;
		int i = 0;
		while (Time.realtimeSinceStartup - t0 < seconds)
		{
			yield return StartCoroutine(ShotAt("gs_v" + part + "_" + i.ToString("D3"), t0 + (float)i * 0.1f));
			i++;
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
		File.WriteAllText(Path.Combine(mDir, "gs_log.txt"), mLog.ToString());
	}

	private void Finish(string result)
	{
		Log("RESULT: " + result);
		File.WriteAllText(Path.Combine(mDir, "done.txt"), result);
	}
}
