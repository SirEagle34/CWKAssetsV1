using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class TradeShots : MonoBehaviour
{
	private string mDir;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		if (Array.IndexOf(commandLineArgs, "-tradeshots") >= 0 || Array.IndexOf(commandLineArgs, "-uishots") >= 0 || Array.IndexOf(commandLineArgs, "-labshots") >= 0 || Array.IndexOf(commandLineArgs, "-newsshots") >= 0 || Array.IndexOf(commandLineArgs, "-chestshots") >= 0 || Array.IndexOf(commandLineArgs, "-storeshots") >= 0 || Array.IndexOf(commandLineArgs, "-craftshots") >= 0 || Array.IndexOf(commandLineArgs, "-extremesearch") >= 0 || Array.IndexOf(commandLineArgs, "-crafttest") >= 0 || Array.IndexOf(commandLineArgs, "-pvpshots") >= 0 || Array.IndexOf(commandLineArgs, "-pvpprobe") >= 0 || Array.IndexOf(commandLineArgs, "-pvphost") >= 0 || Array.IndexOf(commandLineArgs, "-pvpjoin") >= 0)
		{
			Application.runInBackground = true;
			GameObject obj = new GameObject("TradeShots");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<TradeShots>();
		}
	}

	private IEnumerator Start()
	{
		mDir = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(mDir);
		while (UnityEngine.Object.FindObjectOfType<TownController>() == null)
		{
			yield return new WaitForSeconds(1f);
		}
		yield return new WaitForSeconds(12f);
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		int num = Array.IndexOf(commandLineArgs, "-pvphost");
		int num2 = Array.IndexOf(commandLineArgs, "-pvpjoin");
		if ((num >= 0 && num + 1 < commandLineArgs.Length) || (num2 >= 0 && num2 + 1 < commandLineArgs.Length))
		{
			bool host = num >= 0;
			string targetName = commandLineArgs[(host ? num : num2) + 1];
			string role = (host ? "host" : "join");
			StringBuilder log = new StringBuilder();
			float t0 = Time.realtimeSinceStartup;
			bool sent = false;
			bool got = false;
			OnlinePvPManager.OnlinePvPEvent callback = delegate(OnlinePvPManager.OnlinePvPEventCode e, object o)
			{
				log.AppendLine($"{Time.realtimeSinceStartup - t0,6:0.0}s {e} {o}");
				if (e == OnlinePvPManager.OnlinePvPEventCode.IncomingMessages)
				{
					got = true;
				}
				if (!sent && ((host && e == OnlinePvPManager.OnlinePvPEventCode.NewPlayerJoined) || (!host && e == OnlinePvPManager.OnlinePvPEventCode.JoinedToExist)))
				{
					sent = true;
					Singleton<OnlinePvPManager>.Instance.SendMessage("hello-from-" + role);
					log.AppendLine("       sent hello-from-" + role);
				}
			};
			Singleton<TBPvPManager>.Instance.FriendMatch = true;
			if (host)
			{
				Singleton<OnlinePvPManager>.Instance.CreateGame(targetName, callback);
			}
			else
			{
				Singleton<OnlinePvPManager>.Instance.JoinGame(targetName, "probe-joiner", callback);
			}
			for (int i = 0; i < (host ? 600 : 150); i++)
			{
				if (sent && got)
				{
					break;
				}
				yield return new WaitForSeconds(1f);
				File.WriteAllText(Path.Combine(mDir, "pvp_" + role + ".txt"), log.ToString());
			}
			yield return new WaitForSeconds(5f);
			log.AppendLine((sent && got) ? "RESULT: OK - connected, in the same room, messages both ways" : ("RESULT: FAILED (sent=" + sent + " received=" + got + ")"));
			File.WriteAllText(Path.Combine(mDir, "pvp_" + role + ".txt"), log.ToString());
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvpprobe") >= 0)
		{
			string result = "no answer in 40s (app id " + SQSettings.PHOTON_PUN_APP_ID + ", relay " + SQSettings.USE_RELAY + ")";
			bool done = false;
			Singleton<TBPvPManager>.Instance.ConnectToServer("probe", ranked: false, delegate(TBPvPManager.tbPvPEventCode e, object o)
			{
				if (!done)
				{
					result = e.ToString() + " " + ((o != null) ? o.ToString() : string.Empty) + " (app id " + SQSettings.PHOTON_PUN_APP_ID + ")";
					done = e != TBPvPManager.tbPvPEventCode.Disconnected || true;
				}
			});
			for (int i = 0; i < 40; i++)
			{
				if (done)
				{
					break;
				}
				yield return new WaitForSeconds(1f);
			}
			File.WriteAllText(Path.Combine(mDir, "pvpprobe.txt"), result);
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		int num3 = Array.IndexOf(Environment.GetCommandLineArgs(), "-crafttest");
		if (num3 >= 0)
		{
			string text = ((commandLineArgs.Length > num3 + 1) ? commandLineArgs[num3 + 1] : "CRAFT_CARD_001");
			StringBuilder log2 = new StringBuilder();
			CardCraftingData recipe = CardCraftingDataManager.Instance.GetData(text);
			PlayerSaveData save = Singleton<PlayerInfoScript>.Instance.SaveData;
			Func<string> counts = delegate
			{
				string text6 = "craft coins " + save.CraftCurrency + " | " + recipe.CraftItem + " x" + CardCraftingController.CountUsable((recipe.CraftItemData == "CardData") ? "Card" : "Creature", recipe.CraftItem);
				foreach (CardCraftingIngredient ingredient in recipe.Ingredients)
				{
					text6 = text6 + " | " + ingredient.ItemData + " x" + CardCraftingController.CountUsable(ingredient.Item, ingredient.ItemData) + " (need " + ingredient.Cost + ")";
				}
				return text6;
			};
			log2.AppendLine("recipe " + text + " costs " + recipe.CraftCurrencyCost + " craft coins");
			log2.AppendLine("before: " + counts());
			CardCraftingController craft = Singleton<CardCraftingController>.Instance;
			craft.Open();
			craft.SelectRecipe(recipe);
			yield return Shot("t1_recipe");
			log2.AppendLine("craft button enabled: " + (craft.CraftButton != null && craft.CraftButton.isEnabled));
			craft.OnClickCraft();
			yield return Shot("t2_prompt");
			Singleton<SimplePopupController>.Instance.SendMessage("OnClickYes", SendMessageOptions.DontRequireReceiver);
			yield return new WaitForSeconds(8f);
			yield return Shot("t3_result");
			log2.AppendLine("after:  " + counts());
			File.WriteAllText(Path.Combine(mDir, "crafttest.txt"), log2.ToString());
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvpshots") >= 0)
		{
			string[] commandLineArgs2 = Environment.GetCommandLineArgs();
			int num4 = Array.IndexOf(commandLineArgs2, "-extremeteam");
			if (num4 >= 0 && num4 + 1 < commandLineArgs2.Length)
			{
				PvpExtremeTeam.ForceIndex = int.Parse(commandLineArgs2[num4 + 1]);
			}
			int num5 = Array.IndexOf(commandLineArgs2, "-shotlang");
			string sfx = string.Empty;
			if (num5 >= 0 && num5 + 1 < commandLineArgs2.Length)
			{
				Language.SwitchLanguage(commandLineArgs2[num5 + 1]);
				sfx = "_" + commandLineArgs2[num5 + 1];
				yield return new WaitForSeconds(2f);
			}
			PvPModeSelectController modes = UnityEngine.Object.FindObjectOfType<PvPModeSelectController>(includeInactive: true);
			modes.gameObject.SetActive(value: true);
			modes.Populate();
			yield return new WaitForSeconds(4f);
			yield return Shot("p1_mode_menu" + sfx);
			UIScrollView board = null;
			UIScrollView[] componentsInChildren = modes.GetComponentsInChildren<UIScrollView>(includeInactive: true);
			foreach (UIScrollView uIScrollView in componentsInChildren)
			{
				if (uIScrollView.GetComponentInChildren<LeaderboardRowNGUI>(includeInactive: true) != null)
				{
					board = uIScrollView;
				}
			}
			if (board != null)
			{
				UIPanel component = board.GetComponent<UIPanel>();
				string text2 = "clip " + component.clipOffset.ToString() + " pos " + board.transform.localPosition.ToString() + " bounds " + board.bounds.ToString() + " canV " + board.shouldMoveVertically + " panel " + component.finalClipRegion.ToString() + " depth " + component.depth;
				board.MoveRelative(new Vector3(0f, 260f, 0f));
				string text3 = "clip " + component.clipOffset.ToString() + " pos " + board.transform.localPosition.ToString();
				board.RestrictWithinBounds(instant: true);
				File.AppendAllText(Path.Combine(mDir, "pvpshots.txt"), "scroll before: " + text2 + "\nafter move: " + text3 + "\nafter restrict: clip " + component.clipOffset.ToString() + " pos " + board.transform.localPosition.ToString() + "\n");
				yield return Shot("p1b_board_scrolled" + sfx);
				board.ResetPosition();
			}
			File.AppendAllText(Path.Combine(mDir, "pvpshots.txt"), "leaderboard rows: " + modes.GetComponentsInChildren<LeaderboardRowNGUI>(includeInactive: true).Length + " scroll view: " + (board != null) + "\n");
			int i = PvpExtremeTeamDataManager.Instance.GetDatabase().Count;
			for (int ti2 = 0; ti2 < i; ti2++)
			{
				PvpExtremeTeam.ForceIndex = ti2;
				PlayerPrefs.DeleteKey("ExtremeHero");
				Singleton<PVPPrepScreenController>.Instance.Show(PvpMode.Extreme, null);
				yield return Shot("p4_team" + ti2 + sfx);
				Singleton<PVPPrepScreenController>.Instance.OnClickNextLoadout();
				yield return Shot("p4_team" + ti2 + "_nexthero" + sfx);
				Singleton<PVPPrepScreenController>.Instance.OnBackClicked();
				yield return new WaitForSeconds(2f);
			}
			PvpExtremeTeam.ForceIndex = -1;
			PlayerPrefs.DeleteKey("ExtremeHero");
			string[] array = new string[4] { "Tab_Play", "Tab_PlayExtreme", "Tab_History", "Tab_Rewards" };
			foreach (string tabName in array)
			{
				Transform transform = null;
				Transform[] componentsInChildren2 = modes.GetComponentsInChildren<Transform>(includeInactive: true);
				foreach (Transform transform2 in componentsInChildren2)
				{
					if (transform2.name == tabName)
					{
						transform = transform2;
					}
				}
				if (transform == null)
				{
					continue;
				}
				transform.gameObject.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
				yield return Shot("p2_" + tabName + sfx);
				StringBuilder stringBuilder = new StringBuilder();
				BattleCharacterAnimState[] array2 = UnityEngine.Object.FindObjectsOfType<BattleCharacterAnimState>();
				foreach (BattleCharacterAnimState battleCharacterAnimState in array2)
				{
					Bounds bounds = new Bounds(battleCharacterAnimState.transform.position, Vector3.zero);
					Renderer[] componentsInChildren3 = battleCharacterAnimState.GetComponentsInChildren<Renderer>();
					foreach (Renderer renderer in componentsInChildren3)
					{
						bounds.Encapsulate(renderer.bounds);
					}
					stringBuilder.AppendLine("model " + battleCharacterAnimState.name + " pos " + battleCharacterAnimState.transform.position.ToString("F2") + " bounds center " + bounds.center.ToString("F2") + " size " + bounds.size.ToString("F2") + " layer " + battleCharacterAnimState.gameObject.layer + " scale " + battleCharacterAnimState.transform.lossyScale.ToString("F2") + " active " + battleCharacterAnimState.gameObject.activeInHierarchy);
					componentsInChildren3 = battleCharacterAnimState.GetComponentsInChildren<Renderer>(includeInactive: true);
					foreach (Renderer renderer2 in componentsInChildren3)
					{
						stringBuilder.AppendLine("   renderer " + renderer2.name + " " + renderer2.GetType().Name + " enabled " + renderer2.enabled + " active " + renderer2.gameObject.activeInHierarchy + " layer " + renderer2.gameObject.layer + " visible " + renderer2.isVisible + " bounds " + renderer2.bounds.center.ToString("F1") + "/" + renderer2.bounds.size.ToString("F1") + " shader " + ((renderer2.sharedMaterial != null && renderer2.sharedMaterial.shader != null) ? (renderer2.sharedMaterial.shader.name + (renderer2.sharedMaterial.shader.isSupported ? "" : " UNSUPPORTED")) : "none"));
					}
				}
				FrontEndPIP[] array3 = UnityEngine.Object.FindObjectsOfType<FrontEndPIP>();
				foreach (FrontEndPIP frontEndPIP in array3)
				{
					Camera component2 = frontEndPIP.GetComponent<Camera>();
					if (component2 != null && component2.enabled)
					{
						RenderTexture renderTexture = new RenderTexture(512, 512, 24);
						RenderTexture targetTexture = component2.targetTexture;
						Rect rect = component2.rect;
						component2.targetTexture = renderTexture;
						component2.rect = new Rect(0f, 0f, 1f, 1f);
						component2.Render();
						RenderTexture.active = renderTexture;
						Texture2D texture2D = new Texture2D(512, 512, TextureFormat.RGB24, mipChain: false);
						texture2D.ReadPixels(new Rect(0f, 0f, 512f, 512f), 0, 0);
						texture2D.Apply();
						File.WriteAllBytes(Path.Combine(mDir, "cam_" + tabName + ".png"), texture2D.EncodeToPNG());
						RenderTexture.active = null;
						component2.targetTexture = targetTexture;
						component2.rect = rect;
						string[] obj = new string[10] { "   rect ", null, null, null, null, null, null, null, null, null };
						Rect rect2 = rect;
						obj[1] = rect2.ToString();
						obj[2] = " clear ";
						obj[3] = component2.clearFlags.ToString();
						obj[4] = " depth ";
						obj[5] = component2.depth.ToString();
						obj[6] = " near ";
						obj[7] = component2.nearClipPlane.ToString();
						obj[8] = " far ";
						obj[9] = component2.farClipPlane.ToString();
						stringBuilder.AppendLine(string.Concat(obj));
					}
					stringBuilder.AppendLine("pipcam " + frontEndPIP.name + " pos " + frontEndPIP.transform.position.ToString("F2") + " fwd " + frontEndPIP.transform.forward.ToString("F2") + " enabled " + (component2 != null && component2.enabled) + " fov " + ((component2 != null) ? component2.fieldOfView : 0f) + " ortho " + (component2 != null && component2.orthographic) + " size " + ((component2 != null) ? component2.orthographicSize : 0f) + " mask " + ((component2 != null) ? component2.cullingMask : 0));
				}
				File.AppendAllText(Path.Combine(mDir, "pip.txt"), tabName + "\n" + stringBuilder);
				PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
				File.AppendAllText(Path.Combine(mDir, "pvpshots.txt"), tabName + ": pool " + Singleton<TBPvPManager>.Instance.GetCompatibilityVersion(pvPData.RankedMode, pvPData.ExtremeMode) + " extreme=" + pvPData.ExtremeMode + "\n");
				if (tabName == "Tab_History")
				{
					Singleton<BattleHistoryController>.Instance.HideHistoryUI();
				}
				else if (tabName == "Tab_Rewards")
				{
					UIButton[] array4 = UnityEngine.Object.FindObjectsOfType<UIButton>();
					foreach (UIButton uIButton in array4)
					{
						foreach (EventDelegate item in uIButton.onClick)
						{
							if (item != null && item.methodName == "OnClickClose" && uIButton.transform.root.name.Contains("SeasonRewards"))
							{
								uIButton.gameObject.SendMessage("OnClick");
							}
						}
					}
				}
				else
				{
					Singleton<PVPPrepScreenController>.Instance.OnBackClicked();
				}
				yield return new WaitForSeconds(3f);
				yield return Shot("p3_back_" + tabName + sfx);
			}
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-extremesearch") >= 0)
		{
			StringBuilder log2 = new StringBuilder();
			float t1 = Time.realtimeSinceStartup;
			Singleton<PVPPrepScreenController>.Instance.Show(PvpMode.Extreme, null);
			yield return new WaitForSeconds(3f);
			log2.AppendLine("pool " + Singleton<TBPvPManager>.Instance.GetCompatibilityVersion(ranked: false, extreme: true) + " extreme=" + Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode);
			Singleton<PVPPrepScreenController>.Instance.OnClickPlay();
			string sfx = string.Empty;
			string tabName = string.Empty;
			bool battle = false;
			for (int i = 0; i < 300; i++)
			{
				if (battle)
				{
					break;
				}
				yield return new WaitForSeconds(1f);
				string text4 = DetachedSingleton<SceneFlowManager>.Instance.GetCurrentScene().ToString();
				string text5 = Singleton<PlayerInfoScript>.Instance.PvPData.OpponentName ?? string.Empty;
				if (text4 != sfx || text5 != tabName)
				{
					log2.AppendLine($"{Time.realtimeSinceStartup - t1,6:0.0}s scene={text4} opponent={text5} extreme={Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode}");
					sfx = text4;
					tabName = text5;
				}
				battle = text4 == "Battle";
				File.WriteAllText(Path.Combine(mDir, "extreme.txt"), log2.ToString());
			}
			if (battle)
			{
				yield return new WaitForSeconds(20f);
				yield return Shot("x_battle");
			}
			log2.AppendLine(battle ? "RESULT: OK - matched in the Extreme pool, battle loaded" : "RESULT: FAILED - no battle in 300s");
			File.WriteAllText(Path.Combine(mDir, "extreme.txt"), log2.ToString());
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-storeshots") >= 0)
		{
			StringBuilder slog = new StringBuilder();
			CalendarGiftController instance = Singleton<CalendarGiftController>.Instance;
			if (instance != null && instance.CalendarButtonClose != null && instance.CalendarButtonClose.activeInHierarchy)
			{
				instance.CalendarButtonClose.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
			}
			yield return new WaitForSeconds(2f);
			Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Store").SendMessage("OnClick");
			yield return new WaitForSeconds(6f);
			StoreScreenController st = Singleton<StoreScreenController>.Instance;
			UIToggle[] tabs = new UIToggle[6] { st.HeroTabButton, st.CustomizationTabButton, st.ActionCardTabButton, st.CreatureTabButton, st.EvoMatTabButton, st.CardBackTabButton };
			Action<string> tabsState = delegate(string when)
			{
				slog.Append(when + ":");
				UIToggle[] array10 = tabs;
				foreach (UIToggle uIToggle in array10)
				{
					if (uIToggle != null)
					{
						slog.Append(" " + uIToggle.name + "=" + (uIToggle.gameObject.activeInHierarchy ? "on" : "OFF"));
					}
				}
				slog.AppendLine();
				Camera camera = null;
				Camera[] allCameras = Camera.allCameras;
				foreach (Camera camera2 in allCameras)
				{
					if (camera2.name == "Camera_Main")
					{
						camera = camera2;
					}
				}
				if (camera != null)
				{
					Renderer[] array11 = UnityEngine.Object.FindObjectsOfType<Renderer>();
					foreach (Renderer renderer3 in array11)
					{
						if (renderer3.isVisible && renderer3.enabled && (camera.cullingMask & (1 << renderer3.gameObject.layer)) != 0 && renderer3.GetComponent<UIDrawCall>() == null && !(renderer3 is ParticleSystemRenderer))
						{
							slog.AppendLine("   3d " + renderer3.name + " layer=" + renderer3.gameObject.layer + " rq=" + ((renderer3.sharedMaterial == null) ? (-1) : renderer3.sharedMaterial.renderQueue) + " shader=" + ((renderer3.sharedMaterial == null) ? "-" : renderer3.sharedMaterial.shader.name) + " z=" + renderer3.bounds.center.z.ToString("0.00") + " parent=" + ((renderer3.transform.parent == null) ? "-" : renderer3.transform.parent.name));
						}
					}
				}
				foreach (UIPanel item2 in UIPanel.list)
				{
					if (item2.gameObject.activeInHierarchy)
					{
						slog.AppendLine("   panel " + item2.name + " depth=" + item2.depth + " rq=" + item2.startingRenderQueue);
					}
				}
				allCameras = Camera.allCameras;
				foreach (Camera camera3 in allCameras)
				{
					slog.AppendLine($"   cam {camera3.name} depth={camera3.depth} clear={camera3.clearFlags} mask={camera3.cullingMask} ortho={camera3.orthographic} rt={camera3.targetTexture != null} near={camera3.nearClipPlane} far={camera3.farClipPlane} rect={camera3.rect}");
				}
				array10 = tabs;
				foreach (UIToggle uIToggle2 in array10)
				{
					Transform transform3 = ((uIToggle2 == null) ? null : uIToggle2.transform.Find("Icon"));
					UISprite uISprite3 = ((transform3 == null) ? null : transform3.GetComponent<UISprite>());
					if (!(uISprite3 == null))
					{
						Texture mainTexture = uISprite3.mainTexture;
						slog.AppendLine(string.Format("   {0} icon en={1} act={2} a={3:0.00} fa={4:0.00} vis={5} atlas={6} tex={7} d={8} panel={9} dc={10} rq={11} pos={12} scale={13} rot={14}", uIToggle2.name, uISprite3.enabled, uISprite3.gameObject.activeInHierarchy, uISprite3.alpha, uISprite3.finalAlpha, uISprite3.isVisible, (uISprite3.atlas == null) ? "NULL" : uISprite3.atlas.name, (mainTexture == null) ? "NULL" : (mainTexture.name + "/" + mainTexture.width), uISprite3.depth, (uISprite3.panel == null) ? "NULL" : uISprite3.panel.name, (uISprite3.drawCall == null) ? "NULL" : "ok", (uISprite3.drawCall == null) ? (-1) : uISprite3.drawCall.finalRenderQueue, transform3.position, transform3.lossyScale, transform3.eulerAngles));
					}
				}
			};
			tabsState("open");
			yield return Shot("store_open");
			UIToggle[] array5 = tabs;
			foreach (UIToggle tb in array5)
			{
				if (tb == null || !tb.gameObject.activeInHierarchy)
				{
					continue;
				}
				tb.gameObject.SendMessage("OnClick");
				yield return new WaitForSeconds(2f);
				tabsState("after " + tb.name);
				yield return Shot("store_tab_" + tb.name);
				if (tb == st.ActionCardTabButton)
				{
					InventoryTile ct = null;
					InventoryTile[] componentsInChildren4 = st.GetComponentsInChildren<InventoryTile>();
					int num6 = 0;
					if (num6 < componentsInChildren4.Length)
					{
						InventoryTile inventoryTile = componentsInChildren4[num6];
						ct = inventoryTile;
					}
					if (ct != null)
					{
						Func<Transform, string> func = (Transform tr) => (!(tr == null)) ? (tr.name + " lp=" + tr.localPosition.ToString() + " wp=" + ct.transform.InverseTransformPoint(tr.position).ToString() + " act=" + tr.gameObject.activeInHierarchy) : "-";
						slog.AppendLine("cardtile group " + func(ct.RarityStarGroup.transform));
						UISprite[] rarityStars = ct.RarityStars;
						foreach (UISprite uISprite in rarityStars)
						{
							slog.AppendLine("   star " + func(uISprite.transform) + " size=" + uISprite.width + "x" + uISprite.height);
						}
						slog.AppendLine("   name " + func(ct.CardName.transform) + " w=" + ct.CardName.width);
						slog.AppendLine("   cost " + func(ct.CardCost.transform));
						slog.AppendLine("   frame " + func(ct.FrameSprite.transform) + " size=" + ct.FrameSprite.width + "x" + ct.FrameSprite.height + " sprite=" + ct.FrameSprite.spriteName);
						CardData cardData = null;
						CardData cardData2 = null;
						CardData cardData3 = null;
						foreach (CardData item3 in CardDataManager.Instance.GetDatabase())
						{
							if (item3.IsLeaderCard)
							{
								if (cardData3 == null)
								{
									cardData3 = item3;
								}
							}
							else if (item3.Rarity == 4 && cardData == null)
							{
								cardData = item3;
							}
							else if (item3.Rarity == 2 && cardData2 == null)
							{
								cardData2 = item3;
							}
						}
						List<CardPrefabScript> shown = new List<CardPrefabScript>();
						CardData[] array6 = new CardData[3] { cardData, cardData2, cardData3 };
						for (int num8 = 0; num8 < array6.Length; num8++)
						{
							if (array6[num8] != null)
							{
								GameObject obj2 = NGUITools.AddChild(st.gameObject);
								obj2.transform.localPosition = new Vector3((float)(num8 - 1) * 330f, 0f, -600f);
								obj2.transform.localScale = Vector3.one * 1.6f;
								GameObject obj3 = NGUITools.AddChild(obj2, Singleton<PrefabReferences>.Instance.Card);
								CardPrefabScript component3 = obj3.GetComponent<CardPrefabScript>();
								component3.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
								component3.Populate(array6[num8]);
								obj3.AddComponent<UIPanel>().depth = 5000 + num8;
								shown.Add(component3);
							}
						}
						yield return new WaitForSeconds(2f);
						foreach (CardPrefabScript cp in shown)
						{
							if (cp.Card == null)
							{
								continue;
							}
							Func<Transform, string> func2 = (Transform tr) => (!(tr == null)) ? (tr.name + " lp=" + cp.Frame.transform.InverseTransformPoint(tr.position).ToString() + " act=" + tr.gameObject.activeInHierarchy) : "-";
							slog.AppendLine("bigcard " + cp.Card.ID + " rarity=" + cp.Card.Rarity + " leader=" + cp.Card.IsLeaderCard + " | name " + func2(cp.Name.transform) + " | type '" + cp.ActionType.text + "' " + func2(cp.ActionType.transform));
							rarityStars = cp.Frame.GetComponentsInChildren<UISprite>();
							foreach (UISprite uISprite2 in rarityStars)
							{
								if (uISprite2.name.StartsWith("RarityStar_"))
								{
									slog.AppendLine("   " + func2(uISprite2.transform));
								}
							}
						}
						yield return Shot("spell_card_open");
					}
				}
				if (tb == st.CreatureTabButton)
				{
					StoreBuyCreatureScript componentInChildren = st.GetComponentInChildren<StoreBuyCreatureScript>();
					InventoryTile inventoryTile2 = ((componentInChildren == null) ? null : componentInChildren.GetComponentInChildren<InventoryTile>());
					slog.AppendLine("creature tile: " + (componentInChildren != null) + " inventory tile: " + (inventoryTile2 != null) + " colliders: " + ((componentInChildren == null) ? "-" : string.Join(",", Array.ConvertAll(componentInChildren.GetComponentsInChildren<Collider>(includeInactive: true), (Collider x) => x.name + (x.enabled ? "" : "(off)")))));
					if (inventoryTile2 != null)
					{
						inventoryTile2.gameObject.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
						yield return Shot("store_creature_click");
					}
				}
				File.WriteAllText(Path.Combine(mDir, "store.txt"), slog.ToString());
			}
			RedeemUI.Prompt();
			yield return Shot("redeem");
			SimplePopupController instance2 = Singleton<SimplePopupController>.Instance;
			Component[] array7 = new Component[4] { instance2.YesButton, instance2.NoButton, instance2.OkButton, instance2.Input };
			foreach (Component component4 in array7)
			{
				if (component4 != null)
				{
					slog.AppendLine(component4.name + " parent=" + component4.transform.parent.name + " local=" + component4.transform.localPosition.ToString() + " world=" + component4.transform.position.ToString() + " active=" + component4.gameObject.activeInHierarchy);
				}
			}
			File.WriteAllText(Path.Combine(mDir, "store.txt"), slog.ToString());
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-chestshots") >= 0)
		{
			CalendarGiftController instance3 = Singleton<CalendarGiftController>.Instance;
			if (instance3 != null && instance3.CalendarButtonClose != null && instance3.CalendarButtonClose.activeInHierarchy)
			{
				instance3.CalendarButtonClose.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
			}
			yield return new WaitForSeconds(2f);
			List<InventorySlotItem> mine = Singleton<PlayerInfoScript>.Instance.SaveData.FindAllCreatures((InventorySlotItem x) => true);
			mine.Sort((InventorySlotItem a, InventorySlotItem b) => b.Rarity.CompareTo(a.Rarity));
			ChestSkins.Current = "HalloweenChest";
			Singleton<GachaOpenSequencer>.Instance.ShowGachaSequence(mine[0]);
			yield return Shot("chest_closed");
			Singleton<GachaOpenSequencer>.Instance.OnClickNextObject();
			yield return new WaitForSeconds(0.2f);
			ScreenCapture.CaptureScreenshot(Path.Combine(mDir, "chest_opening.png"));
			yield return new WaitForSeconds(1.5f);
			ScreenCapture.CaptureScreenshot(Path.Combine(mDir, "chest_open.png"));
			for (int i = 0; i < 6; i++)
			{
				if (!Singleton<GachaOpenSequencer>.Instance.Showing)
				{
					break;
				}
				Singleton<GachaOpenSequencer>.Instance.OnClickNextObject();
				yield return new WaitForSeconds(1f);
			}
			Singleton<GachaMultiResultController>.Instance.ShowMultiEggPanel(mine.GetRange(0, Mathf.Min(10, mine.Count)));
			yield return Shot("chest_multi");
			ChestSkins.Current = null;
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-newsshots") >= 0)
		{
			CalendarGiftController instance4 = Singleton<CalendarGiftController>.Instance;
			if (instance4 != null && instance4.CalendarButtonClose != null && instance4.CalendarButtonClose.activeInHierarchy)
			{
				instance4.CalendarButtonClose.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
			}
			yield return new WaitForSeconds(2f);
			ChangeLogPopupUI.Instance.ShowLatest();
			yield return Shot("news_patchnotes");
			AudioClip[] array8 = SLOTResourceManager.LoadAll<AudioClip>("vo/marshalllee") ?? new AudioClip[0];
			StringBuilder stringBuilder2 = new StringBuilder("clips: " + array8.Length + "\n");
			AudioClip[] array9 = array8;
			foreach (AudioClip audioClip in array9)
			{
				stringBuilder2.AppendLine(audioClip.name + " " + audioClip.length.ToString("0.00") + "s");
			}
			File.WriteAllText(Path.Combine(mDir, "vo_marshalllee.txt"), stringBuilder2.ToString());
			ChangeLogPopupUI.Instance.Hide();
			Singleton<GachaScreenController>.Instance.DailyChestID = "Event";
			Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Gacha").SendMessage("OnClick");
			yield return new WaitForSeconds(8f);
			yield return Shot("gacha_halloween");
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-labshots") >= 0)
		{
			yield return LabShots();
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-craftshots") >= 0)
		{
			yield return CraftShots();
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-uishots") >= 0)
		{
			Singleton<TownSettingsController>.Instance.Show();
			yield return Shot("lang_0_settings");
			Singleton<TownSettingsController>.Instance.langList.SendMessage("OnClick");
			yield return Shot("lang_1_list");
			File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
			yield break;
		}
		TradePanelController market = UnityEngine.Object.FindObjectOfType<TradePanelController>(includeInactive: true);
		if (market == null)
		{
			File.WriteAllText(Path.Combine(mDir, "error.txt"), "no TradePanelController");
			yield break;
		}
		market.Open();
		yield return Shot("1_market");
		market.OpenBuyCoins();
		yield return Shot("2_buy_coins");
		market.CloseBuyCoins();
		market.OnClickNewOffer();
		yield return Shot("3_new_offer_pick_friend");
		TradeOfferController offer = UnityEngine.Object.FindObjectOfType<TradeOfferController>(includeInactive: true);
		if (offer != null)
		{
			offer.OpenFor("d2cc4d4d-e490-4830-b354-98d99e6911f6", "pewpew67");
			yield return new WaitForSeconds(3f);
			int num9 = 0;
			TradeListItem[] componentsInChildren5 = offer.GetComponentsInChildren<TradeListItem>(includeInactive: false);
			foreach (TradeListItem tradeListItem in componentsInChildren5)
			{
				if (num9 >= 2)
				{
					break;
				}
				tradeListItem.OnEntryClicked();
				num9++;
			}
			UIInput[] componentsInChildren6 = offer.GetComponentsInChildren<UIInput>(includeInactive: false);
			int num6;
			for (num6 = 0; num6 < componentsInChildren6.Length; num6++)
			{
				componentsInChildren6[num6].value = "250";
			}
			yield return Shot("4_offer_deal");
			offer.OnClickTheirsTab();
			yield return Shot("5_offer_their_side");
			InventorySlotItem inventorySlotItem = null;
			foreach (InventorySlotItem inventorySlot in Singleton<PlayerInfoScript>.Instance.SaveData.InventorySlots)
			{
				if (inventorySlot != null && inventorySlot.Creature != null)
				{
					inventorySlotItem = inventorySlot;
					break;
				}
			}
			if (inventorySlotItem != null)
			{
				Singleton<CreatureDetailsController>.Instance.ShowCreature(inventorySlotItem, null, enableCardEditing: false);
				yield return Shot("6_creature_details_over_trade");
				Singleton<CreatureDetailsController>.Instance.OnClickClose();
				yield return new WaitForSeconds(1f);
			}
			offer.Close();
			yield return new WaitForSeconds(1f);
			market.OnClickSell();
			yield return new WaitForSeconds(3f);
			componentsInChildren5 = offer.GetComponentsInChildren<TradeListItem>(includeInactive: false);
			num6 = 0;
			if (num6 < componentsInChildren5.Length)
			{
				componentsInChildren5[num6].OnEntryClicked();
			}
			yield return Shot("7_sell_listing");
			offer.Close();
		}
		File.WriteAllText(Path.Combine(mDir, "done.txt"), "ok");
	}

	private IEnumerator CraftShots()
	{
		PlayerSaveData save = Singleton<PlayerInfoScript>.Instance.SaveData;
		StringBuilder log = new StringBuilder();
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		int num = Array.IndexOf(commandLineArgs, "-shotlang");
		string sfx = string.Empty;
		if (num >= 0 && num + 1 < commandLineArgs.Length)
		{
			Language.SwitchLanguage(commandLineArgs[num + 1]);
			sfx = "_" + commandLineArgs[num + 1];
			yield return new WaitForSeconds(2f);
		}
		log.AppendLine("start: gems " + save.HardCurrency + " craft " + save.CraftCurrency);
		yield return Shot("c1_town_hud");
		StoreScreenController store = Singleton<StoreScreenController>.Instance;
		store.Populate();
		yield return new WaitForSeconds(8f);
		StoreEntryScript craftTile = null;
		UIToggle[] componentsInChildren = store.GetComponentsInChildren<UIToggle>(includeInactive: true);
		foreach (UIToggle tab in componentsInChildren)
		{
			if (tab == null || tab.name != "Tab_Inventory/Stamina")
			{
				continue;
			}
			tab.gameObject.SetActive(value: true);
			tab.gameObject.SendMessage("OnClick");
			yield return new WaitForSeconds(2f);
			StoreEntryScript[] array = UnityEngine.Object.FindObjectsOfType<StoreEntryScript>();
			foreach (StoreEntryScript storeEntryScript in array)
			{
				if (storeEntryScript.Extra == StoreEntryScript.ExtrasType.CraftCurrency && storeEntryScript.gameObject.activeInHierarchy)
				{
					craftTile = storeEntryScript;
				}
			}
			if (craftTile != null)
			{
				log.AppendLine("craft tile on tab " + tab.name);
				break;
			}
		}
		log.AppendLine("craft tile found: " + (craftTile != null));
		yield return Shot("c2_store_craft_tile" + sfx);
		bool done = false;
		PlayerSaveData.ActionResult bought = null;
		save.ClaimCurrency("craft coin purchase", string.Empty, 0, -MiscParams.BuyCraftCurrencyCost, 0, 0, delegate(PlayerSaveData.ActionResult r)
		{
			bought = r;
			done = true;
		});
		for (int i = 0; i < 30; i++)
		{
			if (done)
			{
				break;
			}
			yield return new WaitForSeconds(1f);
		}
		log.AppendLine("purchase: " + ((bought != null) ? bought.reason : "no reply") + " -> gems " + save.HardCurrency + " craft " + save.CraftCurrency);
		Singleton<PlayerInfoScript>.Instance.Save();
		store.OnClickClose();
		yield return new WaitForSeconds(2f);
		yield return Shot("c3_hud_after_buy");
		CardCraftingController craft = Singleton<CardCraftingController>.Instance;
		craft.Open();
		yield return Shot("c4_crafting" + sfx);
		craft.OnClickCraft();
		yield return Shot("c4b_craft_prompt" + sfx);
		Singleton<SimplePopupController>.Instance.SendMessage((save.CraftCurrency < 3) ? "OnClickYes" : "OnClickNo", SendMessageOptions.DontRequireReceiver);
		craft.Close();
		yield return new WaitForSeconds(1f);
		Singleton<PVPPrepScreenController>.Instance.Show(PvpMode.Extreme, null);
		yield return Shot("c5_extreme_prep" + sfx);
		log.AppendLine("extreme flag " + Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode + " match version " + Singleton<TBPvPManager>.Instance.GetCompatibilityVersion(ranked: false, extreme: true) + " invite version " + Singleton<TBPvPManager>.Instance.GetCompatibilityVersion(ranked: false));
		File.WriteAllText(Path.Combine(mDir, "craftshots.txt"), log.ToString());
	}

	private IEnumerator LabShots()
	{
		StringBuilder log = new StringBuilder();
		CalendarGiftController instance = Singleton<CalendarGiftController>.Instance;
		if (instance != null && instance.CalendarButtonClose != null && instance.CalendarButtonClose.activeInHierarchy)
		{
			instance.CalendarButtonClose.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
		}
		yield return new WaitForSeconds(2f);
		EvoScreenController evo = Singleton<EvoScreenController>.Instance;
		evo.Populate();
		yield return Shot("l1_lab_empty");
		InventoryTile canEvo = null;
		InventoryTile inventoryTile = null;
		InventoryTile[] componentsInChildren = evo.GetComponentsInChildren<InventoryTile>(includeInactive: true);
		foreach (InventoryTile inventoryTile2 in componentsInChildren)
		{
			if (inventoryTile2 == null || inventoryTile2.InventoryItem == null || inventoryTile2.InventoryItem.SlotType != InventorySlotType.Creature)
			{
				continue;
			}
			if (inventoryTile2.InventoryItem.Creature.CanEverEvo())
			{
				if (canEvo == null)
				{
					canEvo = inventoryTile2;
				}
			}
			else if (inventoryTile == null)
			{
				inventoryTile = inventoryTile2;
			}
		}
		log.AppendLine("awakenable tile: " + ((canEvo != null) ? canEvo.InventoryItem.Creature.Form.ID : "none"));
		log.AppendLine("not awakenable tile: " + ((inventoryTile != null) ? inventoryTile.InventoryItem.Creature.Form.ID : "none"));
		MethodInfo drop = typeof(EvoScreenController).GetMethod("OnTileDropped", BindingFlags.Instance | BindingFlags.NonPublic);
		if (inventoryTile != null)
		{
			log.AppendLine("drop not-awakenable accepted: " + drop.Invoke(evo, new object[2] { inventoryTile, 0 }));
			yield return Shot("l2_not_awakenable_popup");
			Singleton<SimplePopupController>.Instance.OnClickYes();
			yield return new WaitForSeconds(1f);
		}
		if (canEvo != null)
		{
			InventorySlotItem item = canEvo.InventoryItem;
			log.AppendLine("drop awakenable accepted: " + drop.Invoke(evo, new object[2] { canEvo, 0 }));
			yield return new WaitForSeconds(3f);
			yield return Shot("l3_awaken_ready");
			log.AppendLine("passive panel shown: " + evo.PassiveUpParent.activeInHierarchy);
			UILabel[] componentsInChildren2 = evo.GetComponentsInChildren<UILabel>(includeInactive: false);
			foreach (UILabel uILabel in componentsInChildren2)
			{
				if (uILabel != null && uILabel.text != null && Regex.IsMatch(uILabel.text, "\\bLv\\b|Lvl|Level \\d", RegexOptions.IgnoreCase))
				{
					log.AppendLine("level text on lab: " + uILabel.name + " = " + uILabel.text.Replace("\n", " "));
				}
			}
			evo.OnClickBackToTown();
			yield return new WaitForSeconds(2f);
			Singleton<CreatureDetailsController>.Instance.ShowCreature(item, null, enableCardEditing: false);
			yield return Shot("l4_creature_details");
			componentsInChildren2 = Singleton<CreatureDetailsController>.Instance.GetComponentsInChildren<UILabel>(includeInactive: false);
			foreach (UILabel uILabel2 in componentsInChildren2)
			{
				if (uILabel2 != null && uILabel2.text != null && uILabel2.text.ToLower().Contains("passive"))
				{
					log.AppendLine("details label: " + uILabel2.name + " = " + uILabel2.text.Replace("\n", " "));
				}
			}
		}
		File.WriteAllText(Path.Combine(mDir, "labshots.txt"), log.ToString());
	}

	private IEnumerator Shot(string name)
	{
		yield return new WaitForSeconds(3f);
		ScreenCapture.CaptureScreenshot(Path.Combine(mDir, name + ".png"));
		yield return new WaitForSeconds(1f);
	}
}
