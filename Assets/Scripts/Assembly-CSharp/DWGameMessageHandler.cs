using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DWGameMessageHandler : Singleton<DWGameMessageHandler>
{
	private enum HitReactType
	{
		None = 0,
		Small = 1,
		Big = 2
	}

	private class CreatureHit
	{
		public float RawDamage;

		public float CappedDamage;

		public bool Crit;

		public bool Miss;

		public bool Shield;

		public DamageType HitType;

		public HitReactType HeroReactType;

		public bool Killing;

		public CreatureHit(GameMessage attackMessage, GameMessage damageMessage, bool killing)
		{
			Crit = attackMessage.IsCritical;
			Miss = attackMessage.IsMiss;
			Shield = attackMessage.IsShield;
			HeroReactType = HitReactType.Small;
			Killing = false;
			if (damageMessage != null)
			{
				if (damageMessage.Creature != null)
				{
					if (killing)
					{
						HeroReactType = HitReactType.Big;
						Killing = true;
					}
					else
					{
						HeroReactType = HitReactType.Small;
						Killing = false;
					}
				}
				RawDamage = damageMessage.RawAmount;
				CappedDamage = damageMessage.Amount;
				if (damageMessage.MagicDamage && damageMessage.PhysicalDamage)
				{
					HitType = DamageType.Hybrid;
				}
				else if (damageMessage.PhysicalDamage)
				{
					HitType = DamageType.Physical;
				}
				else
				{
					HitType = DamageType.Magic;
				}
				damageMessage.Presented = true;
			}
			else
			{
				RawDamage = (CappedDamage = 0f);
				HitType = DamageType.Physical;
			}
		}
	}

	private class CreatureAttackTarget
	{
		public CreatureState Creature;

		public List<CreatureHit> Hits = new List<CreatureHit>();
	}

	private class CreatureAttacker
	{
		public CreatureState Creature;

		public List<CreatureAttackTarget> Targets = new List<CreatureAttackTarget>();
	}

	private const float Y_POSITION_ADJUST = 2f;

	private const float MELEE_POSITION_OFFSET = 5f;

	public Color StatusTextBuffColor;

	public Color StatusTextDebuffColor;

	public Color PassiveTextColor;

	public float HitFlashMatDuration = 0.2f;

	public float TimeBetweenTeamAttacks = 0.7f;

	public float RangedAttackFinishTime = 0.7f;

	public float RotateToAttackTime = 0.3f;

	public Material HitFlashMaterial;

	public Material FreezeDesatMaterial;

	public Material StealthMaterial;

	public GameObject TransmogrifyCreature;

	public float CritCamYOffset;

	public float CritCamZOffset;

	public float CritCamLookAtYOffset;

	public float NormalCameraShake;

	public float NormalCameraShakeTime;

	public float CritCameraShake;

	public float CritCameraShakeTime;

	private Vector3 followVelocity;

	public GameObject NonLaneActionStartFXPrefab;

	public GameObject EffectNameDisplayPrefab;

	public GameObject ShieldHitFX;

	public bool PrintDebugActionName;

	public Transform ProjectileFollower;

	public Transform BoardCenterPos;

	public Transform PlayerLaneCenterPos;

	public Transform OpponentLaneCenterPos;

	private bool mProcessingMessages;

	private List<GameMessage> mQueuedMessages = new List<GameMessage>();

	private bool[] mHeroCardAlreadyPlayed = new bool[2];

	private bool mInBattleFinishMoment;

	private int mCreatureAttacksInProgress;

	private List<Transform> mFollowingProjectiles = new List<Transform>();

	private CardPrefabScript mDragDrawCard;

	private float mEffectFinishTimer = -1f;

	private Dictionary<Transform, Transform> _boneCache = new Dictionary<Transform, Transform>();

	public bool IsEffectDone()
	{
		if (!mProcessingMessages && mQueuedMessages.Count == 0 && CardProgress.Instance.State == CardState.Idle)
		{
			return !AttackProgress.AttacksInProgress;
		}
		return false;
	}

	public void ProcessMessages(List<GameMessage> messages)
	{
		if (mQueuedMessages.Count == 0 && !Singleton<PauseController>.Instance.Paused)
		{
			UICamera.LockInput();
		}
		int count = mQueuedMessages.Count;
		mQueuedMessages.AddRange(messages);
		for (int i = count; i < mQueuedMessages.Count; i++)
		{
			mQueuedMessages[i].Index = i;
		}
		CheckMessageQueue();
	}

	private void CheckMessageQueue()
	{
		if (!mProcessingMessages && mQueuedMessages.Count > 0 && CardProgress.Instance.State == CardState.Idle && !AttackProgress.AttacksInProgress)
		{
			StartCoroutine(PlayEffectSequence());
		}
	}

	private void Update()
	{
		if (!Singleton<PauseController>.Instance.Paused)
		{
			CheckMessageQueue();
			UpdateProjectileFollower();
			if (mEffectFinishTimer > 0f)
			{
				mEffectFinishTimer -= Time.deltaTime;
			}
		}
	}

	private GameMessage GetDamageMessage(GameMessage attackMessage)
	{
		for (int i = attackMessage.Index + 1; i < mQueuedMessages.Count; i++)
		{
			if (mQueuedMessages[i].Action == GameEvent.DAMAGE_CREATURE)
			{
				return mQueuedMessages[i];
			}
			if (mQueuedMessages[i].Action == GameEvent.ATTACK_HIT_END)
			{
				break;
			}
		}
		return null;
	}

	private IEnumerator PlayEffectSequence()
	{
		mProcessingMessages = true;
		GameState currentState = Singleton<DWGame>.Instance.GetCurrentGameState();
		Singleton<DWBattleLane>.Instance.HideAvailableActionIndicators();
		bool anyCreaturesDied = false;
		bool anyCreaturesPushed = false;
		bool dragAttackHappened = false;
		bool cardAttackHappened = false;
		bool cardPlayed = false;
		bool turnStarted = false;
		bool gameEnded = false;
		HitReactType hitReactType = HitReactType.None;
		bool moreHitsToComeInThisAttack = false;
		bool moreAttacksToCome = false;
		bool inCritAttack = false;
		CreatureState currentAttackTarget = null;
		for (int i = 0; i < mQueuedMessages.Count; i++)
		{
			GameMessage message = mQueuedMessages[i];
			if (message.Presented)
			{
				continue;
			}
			if (!Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				while (Singleton<PauseController>.Instance.Paused)
				{
					yield return null;
				}
			}
			if (message.Action == GameEvent.START_TURN)
			{
				if (currentState.IsP1Turn())
				{
					Singleton<BattleHudController>.Instance.OnStartP1Turn();
					Singleton<TutorialController>.Instance.AdvanceIfOnEnemyTurn();
				}
				else
				{
					Singleton<BattleHudController>.Instance.OnStartP2Turn();
				}
				while (Singleton<BattleHudController>.Instance.IsBannerTweenPlaying())
				{
					yield return null;
				}
				turnStarted = true;
			}
			else if (message.Action == GameEvent.GAIN_ACTION_POINTS)
			{
				Singleton<BattleHudController>.Instance.AdjustActionPoints(message.WhichPlayer.Type, (int)message.Amount);
			}
			else if (message.Action == GameEvent.SPEND_ACTION_POINTS || message.Action == GameEvent.LOSE_ACTION_POINTS)
			{
				Singleton<BattleHudController>.Instance.AdjustActionPoints(message.WhichPlayer.Type, -(int)message.Amount);
			}
			else if (message.Action == GameEvent.CARD_PLAYED)
			{
				cardPlayed = true;
				if (!string.IsNullOrEmpty(message.Card.DirectDamageFX))
				{
					string audioClipName = "battle/SFX_SpellCast" + Random.Range(1, 6);
					Singleton<SLOTAudioManager>.Instance.PlaySound(audioClipName, 2f);
				}
				yield return StartCoroutine(ShowCardPlayEffect(message, mQueuedMessages));
			}
			else if (message.Action == GameEvent.CREATURE_DEPLOYED)
			{
				cardPlayed = true;
				if (!message.IsDrag && !Singleton<DWGame>.Instance.IsTutorialSetup)
				{
					if (message.WhichPlayer.Type == PlayerType.User)
					{
						yield return StartCoroutine(Singleton<HandCardController>.Instance.ShowUserCreaturePullAnim(message.Creature.Data, message.Lane.Index));
					}
					else
					{
						yield return StartCoroutine(Singleton<HandCardController>.Instance.ShowOpponentPlayAnim(message.Creature.Data, message.Lane.Index));
					}
					Vector3 position = Singleton<DWBattleLane>.Instance.LaneObjParents[message.Lane.Index].position;
					PlayDeployEffect(position, null, message);
					Singleton<DWGame>.Instance.DeployCreatureFromCard(message.WhichPlayer.Type, message.Creature.Data, message.Lane.Index, fromPullCard: true);
				}
			}
			else if (message.Action == GameEvent.ATTACK_START)
			{
				currentAttackTarget = null;
				for (int j = i + 1; j < mQueuedMessages.Count; j++)
				{
					if (mQueuedMessages[j].Action == GameEvent.CREATURE_ATTACKED && mQueuedMessages[j].IsCritical && !mQueuedMessages[j].IsCounter)
					{
						inCritAttack = true;
						break;
					}
					if (mQueuedMessages[j].Action == GameEvent.ATTACK_END)
					{
						inCritAttack = false;
						break;
					}
				}
				moreAttacksToCome = false;
				for (int k = i + 1; k < mQueuedMessages.Count - 1 && (mQueuedMessages[k].Action != GameEvent.CREATURE_ATTACKED || !mQueuedMessages[k].IsCounter); k++)
				{
					if (mQueuedMessages[k].Action == GameEvent.ATTACK_START && mQueuedMessages[k + 1].Action != GameEvent.ATTACK_END)
					{
						if (mQueuedMessages[k].Creature.Owner == message.Creature.Owner)
						{
							moreAttacksToCome = true;
						}
						break;
					}
				}
			}
			else if (message.Action == GameEvent.ATTACK_HIT_START)
			{
				List<GameMessage> list = new List<GameMessage>();
				CreatureState creatureState = null;
				bool flag = false;
				for (int l = i + 1; l < mQueuedMessages.Count; l++)
				{
					if (mQueuedMessages[l].Action == GameEvent.CREATURE_ATTACKED && creatureState == null)
					{
						creatureState = mQueuedMessages[l].SecondCreature;
					}
					bool flag2 = false;
					if (mQueuedMessages[l].Action == GameEvent.ATTACK_START && mQueuedMessages[l].Creature.Owner != message.Creature.Owner)
					{
						flag2 = true;
					}
					if (flag2 || mQueuedMessages[l].Action == GameEvent.ATTACK_HIT_END)
					{
						if (flag2)
						{
							moreHitsToComeInThisAttack = false;
							break;
						}
						for (int m = l + 1; m < mQueuedMessages.Count; m++)
						{
							if (mQueuedMessages[m].Action == GameEvent.ATTACK_END)
							{
								moreHitsToComeInThisAttack = false;
								break;
							}
							if (mQueuedMessages[m].Action != GameEvent.ATTACK_HIT_START)
							{
								continue;
							}
							if (mQueuedMessages[m + 1].Action == GameEvent.CREATURE_ATTACKED)
							{
								if (mQueuedMessages[m + 1].SecondCreature == creatureState)
								{
									moreHitsToComeInThisAttack = true;
									break;
								}
							}
							else if (mQueuedMessages[m + 1].Action == GameEvent.AREA_ATTACK_START)
							{
								moreHitsToComeInThisAttack = false;
								break;
							}
						}
						break;
					}
					list.Add(mQueuedMessages[l]);
					if (mQueuedMessages[l].Action != GameEvent.CREATURE_ATTACKED || !mQueuedMessages[l].IsCounter)
					{
						continue;
					}
					flag = true;
					for (int n = l + 1; n < mQueuedMessages.Count; n++)
					{
						if (mQueuedMessages[n].Action == GameEvent.CREATURE_DIED && mQueuedMessages[n].Creature == mQueuedMessages[l].SecondCreature)
						{
							list.Add(mQueuedMessages[n]);
							break;
						}
						if (mQueuedMessages[n].Action == GameEvent.ATTACK_HIT_END)
						{
							break;
						}
					}
					break;
				}
				bool num = !flag && mQueuedMessages.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.CARD_PLAYED) == null;
				bool isAiDragAttack = num && currentState.IsP2Turn();
				if (num)
				{
					dragAttackHappened = true;
				}
				else
				{
					cardAttackHappened = true;
				}
				bool useCritCamera = inCritAttack && !moreAttacksToCome;
				if (list.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.AREA_ATTACK_START) != null)
				{
					List<GameMessage> list2 = new List<GameMessage>();
					foreach (GameMessage item3 in list)
					{
						if (!item3.Presented && item3.Action == GameEvent.CREATURE_ATTACKED)
						{
							list2.Add(item3);
						}
						if (item3.Action == GameEvent.AREA_ATTACK_END)
						{
							break;
						}
					}
					if (list2.Count == 0)
					{
						continue;
					}
					CreatureAttacker creatureAttacker = new CreatureAttacker
					{
						Creature = list2[0].Creature
					};
					CreatureState creatureState2 = null;
					foreach (GameMessage item4 in list2)
					{
						item4.Presented = true;
						CreatureAttackTarget target2 = new CreatureAttackTarget
						{
							Creature = item4.SecondCreature
						};
						creatureAttacker.Targets.Add(target2);
						if (creatureState2 == null)
						{
							creatureState2 = target2.Creature;
						}
						GameMessage damageMessage = GetDamageMessage(item4);
						if (damageMessage != null)
						{
							damageMessage.Presented = true;
						}
						bool killing = list.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.CREATURE_DIED && gameMessage5.Creature == target2.Creature && !gameMessage5.AutoReviveDeath) != null;
						CreatureHit item = new CreatureHit(item4, damageMessage, killing);
						target2.Hits.Add(item);
					}
					if (moreAttacksToCome)
					{
						StartCoroutine(CreatureAttack(creatureAttacker, useCritCamera, firstAttack: true, !moreHitsToComeInThisAttack, isAiDragAttack));
						yield return new WaitForSeconds(0.6f);
					}
					else
					{
						yield return StartCoroutine(CreatureAttack(creatureAttacker, useCritCamera, firstAttack: true, !moreHitsToComeInThisAttack, isAiDragAttack));
					}
				}
				else
				{
					GameMessage gameMessage = list.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.CREATURE_ATTACKED);
					if (gameMessage == null)
					{
						continue;
					}
					CreatureAttacker creatureAttacker2 = new CreatureAttacker
					{
						Creature = gameMessage.Creature
					};
					gameMessage.Presented = true;
					CreatureAttackTarget target3 = new CreatureAttackTarget
					{
						Creature = gameMessage.SecondCreature
					};
					creatureAttacker2.Targets.Add(target3);
					GameMessage damageMessage2 = GetDamageMessage(gameMessage);
					if (damageMessage2 != null)
					{
						damageMessage2.Presented = true;
					}
					bool killing2 = list.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.CREATURE_DIED && gameMessage5.Creature == target3.Creature && !gameMessage5.AutoReviveDeath) != null;
					CreatureHit item2 = new CreatureHit(gameMessage, damageMessage2, killing2);
					target3.Hits.Add(item2);
					bool firstAttack = currentAttackTarget != target3.Creature;
					currentAttackTarget = target3.Creature;
					if (moreAttacksToCome)
					{
						StartCoroutine(CreatureAttack(creatureAttacker2, useCritCamera, firstAttack, !moreHitsToComeInThisAttack, isAiDragAttack));
						yield return new WaitForSeconds(0.6f);
					}
					else
					{
						yield return StartCoroutine(CreatureAttack(creatureAttacker2, useCritCamera, firstAttack, !moreHitsToComeInThisAttack, isAiDragAttack));
					}
				}
			}
			else if (message.Action == GameEvent.CREATURE_DIED)
			{
				if (!message.AutoReviveDeath)
				{
					while (mCreatureAttacksInProgress > 0)
					{
						yield return null;
					}
					anyCreaturesDied = true;
					if (mQueuedMessages.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.CREATURE_ATTACKED) == null)
					{
						Singleton<BattleHudController>.Instance.PlayWhiteScreenFlashTween();
					}
					GameMessage gameMessage2 = mQueuedMessages.Find((GameMessage gameMessage5) => gameMessage5.Action == GameEvent.PASSIVE_TRIGGERED && gameMessage5.Creature == message.Creature);
					if (gameMessage2 != null)
					{
						if (gameMessage2.Presented)
						{
							gameMessage2 = null;
						}
						else
						{
							gameMessage2.Presented = true;
						}
					}
					OnCreatureDied(message.Creature, gameMessage2);
					yield return new WaitForSeconds(0.5f);
					if (message.WhichPlayer.Type == PlayerType.User)
					{
						Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(Singleton<DWGame>.Instance.GetCharacter(PlayerType.User), VOEvent.LoseCreature);
					}
					else
					{
						Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(Singleton<DWGame>.Instance.GetCharacter(PlayerType.User), VOEvent.DestroyCreature);
					}
				}
				else
				{
					DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(message.Creature);
					yield return StartCoroutine(Singleton<DWBattleLane>.Instance.PlayDerez(laneObject, message.Creature, revive: true, showDeath: true));
				}
				if (hitReactType < HitReactType.Big)
				{
					hitReactType = HitReactType.Big;
					_ = message.Creature.Owner.Type;
				}
			}
			else if (message.Action == GameEvent.DAMAGE_CREATURE)
			{
				if (message.Amount != 0f)
				{
					ShowCreatureDamage(isFatal: mQueuedMessages.Count > i + 1 && mQueuedMessages[i + 1].Action == GameEvent.CREATURE_DIED && !mQueuedMessages[i + 1].AutoReviveDeath && mQueuedMessages[i + 1].Creature == message.Creature, type: (message.MagicDamage && message.PhysicalDamage) ? DamageType.Hybrid : ((!message.PhysicalDamage) ? DamageType.Magic : DamageType.Physical), creature: message.Creature, rawAmount: (int)message.RawAmount, cappedAmount: (int)message.Amount, isCrit: message.IsCritical, isMissOrShield: false);
					if (message.Parent != null && message.Parent.Action == GameEvent.START_TURN)
					{
						mEffectFinishTimer = 1f;
						yield return new WaitForSeconds(0.11f);
					}
					if (hitReactType < HitReactType.Small)
					{
						hitReactType = HitReactType.Small;
						_ = message.Creature.Owner.Type;
					}
				}
			}
			else if (message.Action == GameEvent.HEAL_CREATURE)
			{
				ShowCreatureHealing(message.Creature, (int)message.RawAmount, (int)message.Amount);
			}
			else if (message.Action == GameEvent.SET_CREATURE_HP)
			{
				CreatureHPBar hPBar = Singleton<BattleHudController>.Instance.GetHPBar(message.Creature);
				if (hPBar != null)
				{
					hPBar.SetHealth((int)message.Amount);
				}
			}
			else if (message.Action == GameEvent.DRAW_CARD || message.Action == GameEvent.INITIAL_DRAW_CARD)
			{
				List<GameMessage> list3 = mQueuedMessages.FindAll((GameMessage gameMessage5) => !gameMessage5.Presented && gameMessage5.Parent == message.Parent && (gameMessage5.Action == GameEvent.DRAW_CARD || gameMessage5.Action == GameEvent.INITIAL_DRAW_CARD));
				List<CardData> drawnCards = new List<CardData>();
				foreach (GameMessage groupedMessage2 in list3)
				{
					if (groupedMessage2.Action == GameEvent.DRAW_CARD)
					{
						GameMessage gameMessage3 = mQueuedMessages.Find((GameMessage gameMessage5) => !gameMessage5.Presented && gameMessage5.Parent == message.Parent && gameMessage5.Action == GameEvent.FAILED_DRAW_HAND_FULL && gameMessage5.WhichPlayer == groupedMessage2.WhichPlayer && gameMessage5.Card == groupedMessage2.Card);
						if (gameMessage3 != null)
						{
							Singleton<HandCardController>.Instance.ShowFailedCardDraw(gameMessage3.WhichPlayer.Type, gameMessage3.Card, groupedMessage2.Creature);
							gameMessage3.Presented = true;
						}
						else
						{
							if (groupedMessage2.Creature == null)
							{
								HeroPortraitScript heroPortraitScript = Singleton<BattleHudController>.Instance.HeroPortraits[message.WhichPlayer.Type.IntValue];
								yield return StartCoroutine(heroPortraitScript.OnPowerUpMeterFull());
								yield return StartCoroutine(PlayHeroCardDrawAnim(groupedMessage2.WhichPlayer.Type));
								StartCoroutine(PlayDrawCardDelayed());
								Singleton<HandCardController>.Instance.ShowCardDraw(groupedMessage2.WhichPlayer.Type, groupedMessage2.Card);
							}
							else
							{
								StartCoroutine(PlayDrawCardDelayed());
								Singleton<HandCardController>.Instance.ShowCardDraw(groupedMessage2.WhichPlayer.Type, groupedMessage2.Card, groupedMessage2.Creature);
							}
							drawnCards.Add(groupedMessage2.Card);
						}
					}
					else if (groupedMessage2.Action == GameEvent.INITIAL_DRAW_CARD)
					{
						CreatureItem sourceCreature = ((groupedMessage2.Creature == null) ? null : groupedMessage2.Creature.Data);
						Singleton<HandCardController>.Instance.ShowCardDraw(groupedMessage2.WhichPlayer.Type, groupedMessage2.Card, sourceCreature);
					}
					groupedMessage2.Presented = true;
				}
				yield return null;
				while (Singleton<HandCardController>.Instance.CardEventsInProgress())
				{
					yield return null;
				}
				yield return new WaitForSeconds(0.5f);
				if (message.WhichPlayer.Type == PlayerType.User)
				{
					foreach (CardData item5 in drawnCards)
					{
						DetachedSingleton<ConditionalTutorialController>.Instance.OnCardDrawn(item5);
					}
				}
			}
			else if (message.Action == GameEvent.FORCE_DISCARD_CARD)
			{
				foreach (GameMessage item6 in mQueuedMessages.FindAll((GameMessage gameMessage5) => !gameMessage5.Presented && gameMessage5.Action == message.Action))
				{
					Singleton<HandCardController>.Instance.ShowCardDiscard(item6.WhichPlayer.Type, item6.Card);
					item6.Presented = true;
				}
				while (Singleton<HandCardController>.Instance.CardEventsInProgress())
				{
					yield return null;
				}
			}
			else if (message.Action == GameEvent.MANUAL_DISCARD_CARD)
			{
				if (message.WhichPlayer.Type == PlayerType.Opponent)
				{
					Singleton<HandCardController>.Instance.ShowCardDiscard(PlayerType.Opponent, message.Card);
					while (Singleton<HandCardController>.Instance.CardEventsInProgress())
					{
						yield return null;
					}
					yield return new WaitForSeconds(1f);
				}
			}
			else if (message.Action == GameEvent.ENABLE_STEALTH)
			{
				if (Singleton<BattleHudController>.Instance.TargetedCreature == message.Creature)
				{
					Singleton<BattleHudController>.Instance.ClearTargetReticle();
				}
			}
			else if (message.Action == GameEvent.ENABLE_INVISIBLE)
			{
				if (Singleton<BattleHudController>.Instance.TargetedCreature == message.Creature)
				{
					Singleton<BattleHudController>.Instance.ClearTargetReticle();
				}
			}
			else if (message.Action == GameEvent.ENABLE_BRAVERY)
			{
				CreatureState targetedCreature = Singleton<BattleHudController>.Instance.TargetedCreature;
				if (targetedCreature != null && targetedCreature != message.Creature && !targetedCreature.HasBravery)
				{
					Singleton<BattleHudController>.Instance.ClearTargetReticle();
				}
			}
			else if (message.Action == GameEvent.ADVANCE_APMETER)
			{
				Singleton<BattleHudController>.Instance.HeroPortraits[message.WhichPlayer.Type.IntValue].FillPowerUpMeter(message.Amount);
				Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_HeroMeterAdd");
			}
			else if (message.Action == GameEvent.ENABLE_LANDCORN || message.Action == GameEvent.ENABLE_LANDNICE || message.Action == GameEvent.ENABLE_LANDPLAINS || message.Action == GameEvent.ENABLE_LANDSAND || message.Action == GameEvent.ENABLE_LANDSWAMP || message.Action == GameEvent.ENABLE_LANDICY || message.Action == GameEvent.ENABLE_LANDMAGMA)
			{
				if (message.Creature.Owner.Type == PlayerType.User)
				{
					DetachedSingleton<ConditionalTutorialController>.Instance.OnGainedLandscape(message.Creature, message.Status);
				}
			}
			else if (message.Action == GameEvent.GAIN_BUFF || message.Action == GameEvent.GAIN_DEBUFF)
			{
				if (message.Creature.Owner.Type == PlayerType.User)
				{
					DetachedSingleton<ConditionalTutorialController>.Instance.OnGainedStatusEffect(message.Creature, message.Status);
				}
			}
			else if (message.Action == GameEvent.APMETER_FULL)
			{
				if (message.WhichPlayer.Type == PlayerType.User)
				{
					DetachedSingleton<ConditionalTutorialController>.Instance.OnAPMeterFull();
				}
			}
			else if (message.Action == GameEvent.PASSIVE_TRIGGERED)
			{
				DWBattleLaneObject laneObject2 = Singleton<DWBattleLane>.Instance.GetLaneObject(message.Creature);
				if (laneObject2 != null)
				{
					PlayPassiveTriggerEffect(laneObject2.CreatureObject.transform.position, laneObject2, message);
					yield return new WaitForSeconds(1.2f);
				}
			}
			else if (message.Action == GameEvent.SHAPE_SHIFT)
			{
				yield return StartCoroutine(Singleton<DWGame>.Instance.ShapeShiftVisual(message.Creature));
			}
			else if (message.Action == GameEvent.PUSH_CREATURE_TO_HAND)
			{
				Vector3 drawFromPos = Singleton<DWBattleLane>.Instance.RemoveCreatureObj(message.Creature, null, showDeath: false);
				Singleton<HandCardController>.Instance.ShowCreatureCardDraw(message.Creature.Owner.Type, message.Creature.Data, drawFromPos);
				anyCreaturesPushed = true;
			}
			else if (message.Action == GameEvent.RAISE_DEAD)
			{
				bool raisedOnBoard = message.Creature.Lane != null && message.WhichPlayer.HasCreatureInPlay(message.Creature);
				yield return StartCoroutine(GraveyardCinematic.Instance.Raise(message.WhichPlayer, message.Creature, message.SecondCreature, raisedOnBoard));
				if (raisedOnBoard)
				{
					yield return StartCoroutine(Singleton<DWGame>.Instance.RaisedCreatureVisual(message.Creature));
				}
				else
				{
					if (message.WhichPlayer.Type == PlayerType.User)
					{
						Singleton<HandCardController>.Instance.ShowCreatureHand();
					}
					Singleton<HandCardController>.Instance.ShowCreatureCardDraw(message.WhichPlayer.Type, message.Creature.Data);
				}
				yield return StartCoroutine(GraveyardCinematic.Instance.Dawn());
				anyCreaturesPushed = true;
			}
			else if (message.Action == GameEvent.PUSH_CARD_FROM_GRAVEYARD_TO_HAND)
			{
				Vector3 drawFromPos2 = Singleton<DWBattleLane>.Instance.RemoveCreatureObj(message.Creature, null, showDeath: false);
				_ = message.Creature.Data;
				Singleton<HandCardController>.Instance.ShowCreatureCardDraw(message.Creature.Owner.Type, message.Creature.Data, drawFromPos2);
				anyCreaturesPushed = true;
			}
			else if (message.Action == GameEvent.STEAL_CREATURE_TO_HAND)
			{
				Vector3 drawFromPos3 = Singleton<DWBattleLane>.Instance.RemoveCreatureObj(message.Creature, null, showDeath: false);
				Singleton<HandCardController>.Instance.ShowCreatureCardDraw(message.WhichPlayer.Type, message.Creature.Data, drawFromPos3);
				anyCreaturesPushed = true;
			}
			GameEventFXData eventFX = DataStatus.EventFX(message);
			if (eventFX != null)
			{
				if (eventFX.CanGroup)
				{
					List<GameMessage> groupedMessages = mQueuedMessages.FindAll((GameMessage gameMessage5) => !gameMessage5.Presented && gameMessage5.Action == message.Action && gameMessage5.Parent == message.Parent && gameMessage5.Creature.Owner == message.Creature.Owner && gameMessage5.CausedByPassive == message.CausedByPassive);
					bool raisedOnBoard = eventFX.ActionFXSpawnPoint == GameEventFXSpawnPoint.BoardCenter || eventFX.ActionFXSpawnPoint == GameEventFXSpawnPoint.BoardSide;
					if (groupedMessages.Count > 0)
					{
						for (int ii = 0; ii < groupedMessages.Count; ii++)
						{
							GameMessage gameMessage4 = groupedMessages[ii];
							gameMessage4.Presented = true;
							if (ii == 0 || !raisedOnBoard)
							{
								yield return StartCoroutine(PerformEventFX(gameMessage4, eventFX));
								float num2 = ((ii >= groupedMessages.Count - 1 || raisedOnBoard) ? eventFX.ActionFXWaitTime : eventFX.ActionFXGroupedWaitTime);
								if (num2 > 0f)
								{
									yield return new WaitForSeconds(num2);
								}
							}
						}
					}
					else
					{
						yield return StartCoroutine(PerformEventFX(message, eventFX));
						if (eventFX.ActionFXWaitTime > 0f)
						{
							yield return new WaitForSeconds(eventFX.ActionFXWaitTime);
						}
					}
				}
				else
				{
					yield return StartCoroutine(PerformEventFX(message, eventFX));
					if (eventFX.ActionFXWaitTime > 0f)
					{
						yield return new WaitForSeconds(eventFX.ActionFXWaitTime);
					}
				}
			}
			else
			{
				yield return StartCoroutine(PerformEventFX(message, null));
			}
			message.Presented = true;
			if (!Singleton<DWGame>.Instance.InDeploymentPhase())
			{
				int count = Singleton<DWBattleLane>.Instance.BattleLaneObjects[0].Count;
				int count2 = Singleton<DWBattleLane>.Instance.BattleLaneObjects[1].Count;
				if ((count == 0 && Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode) || count2 == 0)
				{
					gameEnded = true;
					Singleton<BattleHudController>.Instance.EnemyThinkingObject.SetActive(value: false);
					break;
				}
			}
		}
		mQueuedMessages.Clear();
		if (mDragDrawCard != null)
		{
			NGUITools.Destroy(mDragDrawCard.gameObject);
			mDragDrawCard = null;
		}
		while (mEffectFinishTimer > 0f)
		{
			yield return null;
		}
		Singleton<DWBattleLane>.Instance.UpdateHPBarAttackValues();
		UICamera.UnlockInput();
		if (dragAttackHappened)
		{
			Singleton<TutorialController>.Instance.AdvanceIfOnAttack();
		}
		if (cardAttackHappened)
		{
			Singleton<TutorialController>.Instance.AdvanceIfTargetingAttack();
		}
		if (cardPlayed)
		{
			Singleton<TutorialController>.Instance.AdvanceIfOnCardPlay(targeting: false);
			if (Singleton<DWGame>.Instance.InDeploymentPhase() && !Singleton<DWGame>.Instance.IsTutorialSetup)
			{
				if (!Singleton<DWGame>.Instance.CurrentBoardState.IsFirstTurn())
				{
					UICamera.LockInput();
					yield return new WaitForSeconds(0.8f);
					UICamera.UnlockInput();
				}
				if (currentState.IsP1Turn())
				{
					Singleton<BattleHudController>.Instance.ClearPvpTimer(timeUp: false);
					Singleton<DWGame>.Instance.EndPlayerTurn();
				}
			}
		}
		if (Singleton<DWBattleLane>.Instance.LootObjectsToCollect())
		{
			DetachedSingleton<ConditionalTutorialController>.Instance.OnLootDropped();
			Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(Singleton<DWGame>.Instance.GetCharacter(PlayerType.User), VOEvent.LootDrop);
			yield return new WaitForSeconds(0.6f);
			bool raisedOnBoard = UICamera.IsInputLocked() && currentState.IsP2Turn();
			if (raisedOnBoard)
			{
				UICamera.UnlockInput();
			}
			Singleton<HandCardController>.Instance.HideHand();
			Singleton<BattleHudController>.Instance.EnemyThinkingObject.SetActive(value: false);
			Singleton<BattleHudController>.Instance.ShowTapToCollectBanner();
			Singleton<DWBattleLane>.Instance.TriggerBattleLootCollect();
			bool pauseMenuInputLocked = Singleton<PauseController>.Instance.IsInputBlocked();
			Singleton<PauseController>.Instance.BlockInput(blocked: false);
			while (Singleton<DWBattleLane>.Instance.LootObjectsToCollect())
			{
				yield return null;
			}
			Singleton<PauseController>.Instance.BlockInput(pauseMenuInputLocked);
			Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
			Singleton<HandCardController>.Instance.ShowHand();
			if (raisedOnBoard)
			{
				UICamera.LockInput();
			}
		}
		else if (anyCreaturesDied)
		{
			UICamera.LockInput();
			while (Singleton<DWBattleLane>.Instance.CurrencyObjectsToCollect())
			{
				yield return null;
			}
			UICamera.UnlockInput();
			Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
		}
		else if (anyCreaturesPushed)
		{
			Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
		}
		if (turnStarted)
		{
			if (currentState.IsP1Turn())
			{
				DetachedSingleton<ConditionalTutorialController>.Instance.OnTurnStart();
			}
			else if (!gameEnded)
			{
				if (!Singleton<DWGame>.Instance.InDeploymentPhase())
				{
					Singleton<DWGameCamera>.Instance.MoveCameraToP2Setup();
					if (!Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle"))
					{
						Singleton<BattleHudController>.Instance.EnemyThinkingTween.Play();
						float thoughtBubbleHeightAdjust = Singleton<DWGame>.Instance.GetCharacter(PlayerType.Opponent).ThoughtBubbleHeightAdjust;
						Singleton<BattleHudController>.Instance.ThoughtBubbleParent.localPosition = new Vector3(0f, thoughtBubbleHeightAdjust, 0f);
					}
					Singleton<DWBattleLane>.Instance.StartP2ThinkingDelay();
				}
				else
				{
					Singleton<DWBattleLane>.Instance.SkipP2ThinkingDelay();
				}
			}
		}
		Singleton<TutorialController>.Instance.AdvanceIfOnState("Q1_PickUpDrop");
		Singleton<TutorialController>.Instance.AdvanceIfOnState("Q1_BattlePlayCard");
		if (!Singleton<DWGame>.Instance.InDeploymentPhase() && Singleton<DWGame>.Instance.GetCreatureCount(PlayerType.Opponent) <= 0)
		{
			if (!IsTransitioningBetweenWaves())
			{
				if (Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle"))
				{
					Singleton<BattleHudController>.Instance.OnGameEnd();
					Singleton<HandCardController>.Instance.HideHand();
					Singleton<TutorialController>.Instance.AdvanceTutorialState();
					Singleton<DWGameCamera>.Instance.MoveCameraToP2Setup();
					Singleton<DWGame>.Instance.SetGameState(GameState.EndGameWait);
				}
				else
				{
					Singleton<DWGame>.Instance.SetGameState(GameState.EndGameWait);
					yield return new WaitForSeconds(0.7f);
					Singleton<DWGame>.Instance.SetGameState(GameState.P2Defeated);
					if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
					{
						Singleton<DWGame>.Instance.turnNumber = 0;
						Singleton<DWGame>.Instance.battleDuration = 0f;
					}
				}
			}
		}
		else if (!Singleton<DWGame>.Instance.InDeploymentPhase() && !Singleton<DWGame>.Instance.WaitingForCreatureDeployAfterRevive && Singleton<DWGame>.Instance.GetCreatureCount(PlayerType.User) <= 0)
		{
			if (!IsTransitioningBetweenWaves() && !Singleton<DWGame>.Instance.IsGameOver())
			{
				Singleton<DWGame>.Instance.LostDuringMyTurn = currentState.IsP1Turn();
				Singleton<DWGame>.Instance.SetGameState(GameState.EndGameWait);
				yield return new WaitForSeconds(0.7f);
				Singleton<DWGame>.Instance.SetGameState(GameState.P1Defeated);
				if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
				{
					Singleton<DWGame>.Instance.turnNumber = 0;
					Singleton<DWGame>.Instance.battleDuration = 0f;
				}
			}
		}
		else if (currentState.IsP1Turn())
		{
			Singleton<DWBattleLane>.Instance.UpdateAvailableActionIndicators();
		}
		mProcessingMessages = false;
		static IEnumerator PlayDrawCardDelayed()
		{
			yield return new WaitForSeconds(1f);
			Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Action_Generic", 1.3f);
		}
	}

	public void PlayDeployEffect(Vector3 position, DWBattleLaneObject attachToLane, GameMessage message)
	{
		GameObject gameObject = SLOTResourceManager.Load("VFX/Creatures/VFX_RezIn_Red") as GameObject;
		if (gameObject == null)
		{
			Debug.LogError("VFX_RezIn_Red load failed!");
			return;
		}
		GameObject gameObject2 = Object.Instantiate(gameObject, position, Quaternion.identity);
		ParticleSystem[] componentsInChildren = gameObject2.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Play(withChildren: true);
		}
		if (attachToLane != null && attachToLane.CreatureObject != null)
		{
			gameObject2.transform.SetParent(attachToLane.CreatureObject.transform);
			gameObject2.transform.localPosition = Vector3.zero;
		}
	}

	public void PlayPassiveTriggerEffect(Vector3 position, DWBattleLaneObject attachToLane, GameMessage message)
	{
		GameObject value = null;
		Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(message.Creature.Data.Form.RezInVFX, out value);
		if (value == null)
		{
			value = (GameObject)SLOTResourceManager.Load("VFX/Creatures/" + message.Creature.Data.Form.RezInVFX);
		}
		if (value != null)
		{
			base.transform.InstantiateAsChild(value).transform.position = position;
		}
		Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Action_TriggerEnchant");
		string str = message.Creature.Data.PassiveNameString();
		Vector3 position2 = ((!(attachToLane != null)) ? position : attachToLane.CreatureObject.transform.position);
		ShowEffectLabelPopup(position2, attachToLane, str, PassiveTextColor, Color.black, 25);
	}

	private Transform GetCachedBone(Transform root, string boneName)
	{
		if (root == null || string.IsNullOrEmpty(boneName))
		{
			return null;
		}
		if (_boneCache.TryGetValue(root, out var value) && value != null)
		{
			return value;
		}
		Transform transform = Singleton<DWGame>.Instance.FindAllInChildren(root, boneName)?.FirstOrDefault();
		if (transform != null)
		{
			_boneCache[root] = transform;
		}
		return transform;
	}

	private IEnumerator PerformEventFX(GameMessage ms, GameEventFXData eventFX)
	{
		PlayerType player = ((ms.Creature == null) ? ms.WhichPlayer.Type : ms.Creature.Owner.Type);
		if (ms.Creature != null)
		{
			CreatureBuffBar buffBar = Singleton<BattleHudController>.Instance.GetBuffBar(ms.Creature);
			if (buffBar != null)
			{
				yield return StartCoroutine(buffBar.SetPersistentVFX(ms));
			}
		}
		if (PrintDebugActionName)
		{
			ms.Action.ToString().StartsWith("ENABLE");
		}
		if (PrintDebugActionName)
		{
			ms.Action.ToString().StartsWith("STATUS");
		}
		if (ms.Action.ToString().StartsWith("STATUS_ACTION"))
		{
			ShowStatusEffectLabelPopup(ms);
		}
		DisableCreaturePersistentFX(ms);
		if (eventFX == null)
		{
			yield break;
		}
		EnableCreaturePersistentFX(ms);
		Transform gameEventEffectTarget = GetGameEventEffectTarget(eventFX.ActionFXSpawnPoint, player, ms.Creature, ms.SecondCreature);
		GameObject gameObject = null;
		if (!(gameEventEffectTarget != null))
		{
			yield break;
		}
		GameObject gameObject2 = SLOTResourceManager.Load("VFX/Actions/" + eventFX.ActionFXPrefab, typeof(GameObject)) as GameObject;
		if (gameObject2 != null)
		{
			if (eventFX.RezInOut != "In")
			{
				gameObject = gameEventEffectTarget.InstantiateAsChild(gameObject2);
				if (eventFX.ActionFXSpawnPoint == GameEventFXSpawnPoint.EnemyPortrait)
				{
					gameObject.ChangeLayerToParent();
				}
				else if ((eventFX.ActionFXSpawnPoint == GameEventFXSpawnPoint.Creature || eventFX.ActionFXSpawnPoint == GameEventFXSpawnPoint.Creature2) && player == PlayerType.User)
				{
					gameObject.transform.eulerAngles = new Vector3(0f, 90f, 0f);
				}
			}
			UIWidget component = gameEventEffectTarget.GetComponent<UIWidget>();
			if (component != null && gameObject != null)
			{
				VFXRenderQueueSorter vFXRenderQueueSorter = gameObject.GetComponent<VFXRenderQueueSorter>();
				if (vFXRenderQueueSorter == null)
				{
					vFXRenderQueueSorter = gameObject.AddComponent<VFXRenderQueueSorter>();
				}
				vFXRenderQueueSorter.mTarget = component;
			}
			if (eventFX.AttractPoint != GameEventFXData.ParticleAttractorPoint.None)
			{
				ParticleAttractor[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleAttractor>();
				foreach (ParticleAttractor particleAttractor in componentsInChildren)
				{
					if (eventFX.AttractPoint == GameEventFXData.ParticleAttractorPoint.MyPortrait)
					{
						particleAttractor.SetTarget(Singleton<BattleHudController>.Instance.HeroPortraits[(int)ms.Creature.Owner.Type].transform);
					}
					else if (eventFX.AttractPoint == GameEventFXData.ParticleAttractorPoint.Creature)
					{
						DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(ms.Creature);
						particleAttractor.SetTarget(laneObject.transform);
					}
				}
			}
		}
		ShowEffectValuePopup(ms, eventFX.Keyword);
	}

	private bool ExistsInDisableEvents(GameMessage ms)
	{
		foreach (StatusData item in StatusDataManager.Instance.GetDatabase())
		{
			if (item.DisableMessage == ms.Action)
			{
				return true;
			}
		}
		return false;
	}

	private StatusData GetStatusData(GameMessage ms)
	{
		return StatusDataManager.Instance.GetDatabase().Find((StatusData match) => match.EnableMessage == ms.Action);
	}

	private IEnumerator ShowCardPlayEffect(GameMessage cardPlayedMessage, List<GameMessage> messages)
	{
		if (cardPlayedMessage.Card.TargetType1 != SelectionType.Lane)
		{
			NoneLaneTriggerEffect(cardPlayedMessage);
		}
		if (cardPlayedMessage.Card.DirectDamageFX != string.Empty && (!cardPlayedMessage.Card.ShapeShift || !ShapeShiftCinematic.Instance.InCloseUp) && (!cardPlayedMessage.Card.Graveyard || !GraveyardCinematic.Instance.InCloseUp))
		{
			Transform transform = GetGameEventEffectTarget(player: (cardPlayedMessage.Creature == null) ? cardPlayedMessage.WhichPlayer.Type : cardPlayedMessage.Creature.Owner.Type, spawnPointType: cardPlayedMessage.Card.FXSpawnPoint, creature: cardPlayedMessage.Creature);
			if (transform != null)
			{
				GameObject gameObject = SLOTResourceManager.Load("VFX/Actions/" + cardPlayedMessage.Card.DirectDamageFX, typeof(GameObject)) as GameObject;
				if (gameObject != null)
				{
					transform.transform.InstantiateAsChild(gameObject);
					yield return new WaitForSeconds(0.8f);
				}
			}
		}
		yield return new WaitForSeconds(0.3f);
	}

	private void NoneLaneTriggerEffect(GameMessage ms)
	{
		UILabel componentInChildren = Singleton<BattleHudController>.Instance.transform.InstantiateAsChild(NonLaneActionStartFXPrefab).GetComponentInChildren<UILabel>();
		if (componentInChildren != null)
		{
			componentInChildren.text = ms.Card.Name + "!";
		}
	}

	private void DisableCreaturePersistentFX(GameMessage ms)
	{
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(ms.Creature);
		if (!(laneObject == null))
		{
			if (ms.Action == GameEvent.DISABLE_FROZEN)
			{
				laneObject.UnfreezeCreature();
			}
			if (ms.Action == GameEvent.DISABLE_STEALTH)
			{
				laneObject.UnStealthCreature();
			}
			if (ms.Action == GameEvent.DISABLE_INVISIBLE)
			{
				laneObject.UnStealthCreature();
			}
			if (ms.Action == GameEvent.DISABLE_TRANSMOGRIFY)
			{
				laneObject.UntransmogrifyCreature();
			}
			laneObject.RemoveStatusEffectObjFromCreature(ms);
		}
	}

	private void EnableCreaturePersistentFX(GameMessage ms)
	{
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(ms.Creature);
		if (!(laneObject == null))
		{
			if (ms.Action == GameEvent.ENABLE_FROZEN)
			{
				laneObject.FreezeCreature();
			}
			if (ms.Action == GameEvent.ENABLE_STEALTH)
			{
				laneObject.StealthCreature();
			}
			if (ms.Action == GameEvent.ENABLE_INVISIBLE)
			{
				laneObject.StealthCreature();
			}
			if (ms.Action == GameEvent.ENABLE_TRANSMOGRIFY)
			{
				laneObject.TransmogrifyCreature();
			}
			laneObject.ApplyStatusEffectObjOnCreature(ms);
		}
	}

	private void OnCreatureDied(CreatureState creature, GameMessage passiveTriggered)
	{
		if (creature.Owner.Type == PlayerType.User)
		{
			Singleton<DWBattleLane>.Instance.CurrentBattleTomestoneCreature.Add(creature);
			DetachedSingleton<ConditionalTutorialController>.Instance.OnCreatureDied();
			Singleton<HandCardController>.Instance.ShowCreatureHand();
		}
		Singleton<BattleHudController>.Instance.OnCreatureDied(creature);
		Singleton<DWBattleLane>.Instance.RemoveCreatureObj(creature, passiveTriggered);
	}

	private Transform GetGameEventEffectTarget(GameEventFXSpawnPoint spawnPointType, PlayerType player = null, CreatureState creature = null, CreatureState secondCreature = null)
	{
		switch (spawnPointType)
		{
		case GameEventFXSpawnPoint.UI_Deck:
			return Singleton<BattleHudController>.Instance.UIEffectTargetDeck.transform;
		case GameEventFXSpawnPoint.UI_HeroPortrait:
			return Singleton<BattleHudController>.Instance.UIEffectTargets[player.IntValue].transform;
		case GameEventFXSpawnPoint.UI_DiscardPile:
			return Singleton<BattleHudController>.Instance.UIEffectTargetDiscardPile.transform;
		case GameEventFXSpawnPoint.UI_MagicPoint:
			return Singleton<BattleHudController>.Instance.MagicPoints[player.IntValue].transform;
		case GameEventFXSpawnPoint.Creature:
		{
			GameObject creatureObject2 = Singleton<DWBattleLane>.Instance.GetCreatureObject(creature);
			if (creatureObject2 == null)
			{
				return null;
			}
			return creatureObject2.transform;
		}
		case GameEventFXSpawnPoint.Creature2:
		{
			GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(secondCreature);
			if (creatureObject == null)
			{
				return null;
			}
			return creatureObject.transform;
		}
		case GameEventFXSpawnPoint.BoardSide:
			if (player == PlayerType.User)
			{
				return PlayerLaneCenterPos;
			}
			return OpponentLaneCenterPos;
		case GameEventFXSpawnPoint.BoardCenter:
			return BoardCenterPos;
		case GameEventFXSpawnPoint.EnemyPortrait:
			return Singleton<BattleHudController>.Instance.HeroPortraits[(int)(!creature.Owner.Type)].transform;
		default:
			return null;
		}
	}

	public void PlayActionTriggerAnim(CreatureState creature)
	{
		GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(creature);
		if (!Singleton<DWBattleLane>.Instance.GetLaneObject(creature).IsFrozen)
		{
			creatureObject.GetComponent<Animation>().Play("Floop");
		}
	}

	public void PlayNegativeActionTriggerAnim(CreatureState creature)
	{
		GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(creature);
		if (!Singleton<DWBattleLane>.Instance.GetLaneObject(creature).IsFrozen)
		{
			creatureObject.GetComponent<Animation>().Play("Hammer");
		}
	}

	public Transform FindInChildren(Transform tr, string childName)
	{
		Transform[] componentsInChildren = tr.gameObject.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name.Contains(childName))
			{
				return transform;
			}
		}
		return null;
	}

	public void FlashDamagedCreature(CreatureState creature)
	{
		BattleCreatureAnimState componentInChildren = Singleton<DWBattleLane>.Instance.GetCreatureObject(creature).GetComponentInChildren<BattleCreatureAnimState>();
		if (!Singleton<DWBattleLane>.Instance.GetLaneObject(creature).IsFrozen)
		{
			_ = componentInChildren == null;
		}
	}

	private IEnumerator AssignOriginalShaderBack(BattleCreatureAnimState animState)
	{
		yield return new WaitForSeconds(HitFlashMatDuration);
		for (int i = 0; i < animState.orignalMeshes.Count; i++)
		{
			animState.orignalMeshes[i].material = animState.originalMats[i];
		}
	}

	public void PlayCreatureAnim(CreatureState creature, string trigger)
	{
		if (!Singleton<DWBattleLane>.Instance.GetLaneObject(creature).IsFrozen)
		{
			Singleton<DWBattleLane>.Instance.GetCreatureObject(creature).GetComponentsInChildren<Animator>(includeInactive: true)[0].SetTrigger(trigger);
		}
	}

	public Vector3 GetAttackPosition(CreatureState creature)
	{
		if (creature == null)
		{
			return Vector3.zero;
		}
		GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(creature);
		if (creatureObject == null)
		{
			Debug.LogWarning("⚠ GetAttackPosition: creatureObject is NULL");
			return Vector3.zero;
		}
		Vector3 position = creatureObject.transform.position;
		position.y += 2f;
		return position;
	}

	private Vector3 GetMeleeAttackPosition(CreatureState attacker, CreatureState target)
	{
		Vector3 position = Singleton<DWBattleLane>.Instance.GetCreatureObject(attacker).transform.position;
		Vector3 position2 = Singleton<DWBattleLane>.Instance.GetCreatureObject(target).transform.position;
		return position2 - (position2 - position).normalized * 5f;
	}

	private void ShowEffectValuePopup(GameMessage ms, KeyWordData keyword)
	{
		if (GetStatusData(ms) != null && keyword != null)
		{
			Color outlineColor = ((ms.Status.StatusType != StatusType.Buff) ? StatusTextDebuffColor : StatusTextBuffColor);
			string str = "-" + keyword.DisplayName + "-\n" + keyword.PopupText;
			DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(ms.Creature);
			ShowEffectLabelPopup(laneObject.CreatureObject.transform.position, laneObject, str, Color.white, outlineColor);
		}
	}

	private void ShowStatusEffectLabelPopup(GameMessage ms)
	{
		if (ms.Status != null && ms.Status.FXData.Keyword != null)
		{
			Color outlineColor = ((ms.Status.StatusType != StatusType.Buff) ? StatusTextDebuffColor : StatusTextBuffColor);
			DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(ms.Creature);
			if (laneObject != null)
			{
				ShowEffectLabelPopup(laneObject.CreatureObject.transform.position, laneObject, ms.Status.FXData.Keyword.DisplayName, Color.white, outlineColor);
			}
		}
	}

	private void ShowEffectLabelPopup(Vector3 position, DWBattleLaneObject attachToLane, string str, Color color, Color outlineColor, int fontSize = 20)
	{
		Singleton<BattleHudController>.Instance.transform.InstantiateAsChild(EffectNameDisplayPrefab).GetComponent<ShowEffectDisplayPopup>().Init(attachToLane, str, position, color, outlineColor, fontSize);
	}

	private IEnumerator CreatureAttack(CreatureAttacker attacker, bool useCritCamera, bool firstAttack, bool lastAttack, bool isAiDragAttack)
	{
		mCreatureAttacksInProgress++;
		PlayerType activePlayer = ((!Singleton<DWGame>.Instance.GetCurrentGameState().IsP1Turn()) ? PlayerType.Opponent : PlayerType.User);
		DWBattleLaneObject attackerLane = Singleton<DWBattleLane>.Instance.GetLaneObject(attacker.Creature);
		GameObject attackerObj = Singleton<DWBattleLane>.Instance.GetCreatureObject(attacker.Creature);
		Animator animator = attackerObj.GetComponentInChildren<Animator>();
		Vector3 targetPos = GetMeleeAttackPosition(attacker.Creature, attacker.Targets[0].Creature);
		CreatureData creatureData = attacker.Creature.Data.Form;
		bool isMagicAttack = creatureData.AlwaysMagicVFX || attacker.Targets[0].Hits[0].HitType == DamageType.Magic || attacker.Targets[0].Hits[0].HitType == DamageType.Hybrid;
		if (activePlayer == PlayerType.Opponent)
		{
			attackerLane.HealthBar.SetAttackValues(attacker.Targets[0].Hits[0].HitType);
		}
		if (firstAttack)
		{
			attackerLane.AttackStartPosition = attackerObj.transform.position;
			if (isAiDragAttack)
			{
				GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(attacker.Targets[0].Creature);
				yield return StartCoroutine(Singleton<DWBattleLane>.Instance.ShowAiAttackArrow(attackerObj, creatureObject));
			}
			if (useCritCamera)
			{
				float num = 0.3f;
				iTween.EaseType easeType = iTween.EaseType.easeInOutCubic;
				Vector3 vector = (attackerLane.AttackStartPosition + targetPos) / 2f;
				Vector3 vector2 = ((activePlayer != PlayerType.User) ? targetPos : attackerLane.AttackStartPosition);
				vector.y += CritCamLookAtYOffset;
				iTween.MoveTo(Singleton<DWGameCamera>.Instance.MainCamLookAt.gameObject, iTween.Hash("position", vector, "time", num, "easetype", easeType));
				Vector3 vector3 = new Vector3(0f, CritCamYOffset + attacker.Creature.Data.Form.Height, CritCamZOffset);
				if (vector.z < vector2.z)
				{
					vector3.z *= -1f;
				}
				Vector3 vector4 = vector - vector2;
				Vector3 vector5 = vector3 + vector2 - vector4.normalized * 15f;
				iTween.MoveTo(Singleton<DWGameCamera>.Instance.MainCam.gameObject, iTween.Hash("position", vector5, "time", num, "easetype", easeType));
				yield return new WaitForSeconds(num);
				Time.timeScale = 0.75f * PlayerPrefs.GetFloat("BattleSpeedMultiplier", 1f);
			}
			iTween.RotateTo(attackerObj, iTween.Hash("rotation", Quaternion.LookRotation(targetPos - attackerLane.AttackStartPosition).eulerAngles, "time", RotateToAttackTime, "easetype", iTween.EaseType.easeInOutCubic));
			if (!isMagicAttack)
			{
				animator.SetTrigger("Lunge");
				if (useCritCamera)
				{
					Singleton<DWGameCamera>.Instance.SetObjectLock(attackerObj.transform);
				}
				iTween.MoveTo(attackerObj, iTween.Hash("position", targetPos, "time", creatureData.AttackMovementTime, "easetype", creatureData.AttackMovementStyle));
				if (creatureData.LungeSound != string.Empty)
				{
					Singleton<SLOTAudioManager>.Instance.PlaySound("creature/" + creatureData.LungeSound);
				}
				else
				{
					Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Attack_Lunge");
				}
				yield return new WaitForSeconds(creatureData.AttackMovementTime);
			}
			else
			{
				animator.SetTrigger("Charge");
				if (creatureData.ChargeSound != string.Empty)
				{
					Singleton<SLOTAudioManager>.Instance.PlaySound(creatureData.ChargeSound);
				}
				GameObject value = null;
				Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(creatureData.AttackChargeVFX, out value);
				if (value == null)
				{
					value = SLOTResourceManager.Load("VFX/Creatures/" + creatureData.AttackChargeVFX, typeof(GameObject)) as GameObject;
				}
				if (value != null)
				{
					foreach (Transform item in Singleton<DWGame>.Instance.FindAllInChildren(attackerObj.transform, creatureData.ShootVFXAttachBone))
					{
						item.InstantiateAsChild(value);
					}
				}
				yield return new WaitForSeconds(creatureData.ChargeTime);
			}
		}
		GameObject mTrailObj = null;
		if (!isMagicAttack)
		{
			animator.SetTrigger("Attack");
			if (creatureData.SmashSound != string.Empty)
			{
				Singleton<SLOTAudioManager>.Instance.PlaySound(creatureData.SmashSound);
			}
			float trailFrameDelay = creatureData.WeaponTrailStartTime;
			yield return new WaitForSeconds(trailFrameDelay);
			if (creatureData.WeaponTrailVFX != null)
			{
				Transform transform = FindInChildren(attackerObj.transform, "TrailAttach");
				if (transform != null)
				{
					GameObject value2 = null;
					Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(creatureData.WeaponTrailVFX, out value2);
					if (value2 == null)
					{
						value2 = (GameObject)SLOTResourceManager.Load("VFX/Creatures/" + creatureData.WeaponTrailVFX);
					}
					if (value2 != null)
					{
						mTrailObj = SLOTGame.InstantiateFX(value2, transform.position, transform.rotation);
						if (mTrailObj != null)
						{
							mTrailObj.transform.parent = transform;
						}
					}
				}
			}
			float num2 = creatureData.AttackStartTime - trailFrameDelay;
			if (num2 > 0f)
			{
				yield return new WaitForSeconds(num2);
			}
		}
		else
		{
			animator.SetTrigger("Shoot");
			yield return new WaitForSeconds(creatureData.ShootStartTime);
			if (creatureData.ShootSound != string.Empty)
			{
				Singleton<SLOTAudioManager>.Instance.PlaySound(creatureData.ShootSound);
			}
			List<GameObject> projectiles = new List<GameObject>();
			mFollowingProjectiles.Clear();
			foreach (CreatureAttackTarget target in attacker.Targets)
			{
				GameObject value3 = null;
				Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(creatureData.ShootVFX, out value3);
				if (value3 == null)
				{
					value3 = SLOTResourceManager.Load("VFX/Creatures/" + creatureData.ShootVFX, typeof(GameObject)) as GameObject;
				}
				if (!(value3 != null))
				{
					continue;
				}
				foreach (Transform item2 in Singleton<DWGame>.Instance.FindAllInChildren(attackerObj.transform, creatureData.ShootVFXAttachBone))
				{
					Quaternion rotation = Quaternion.LookRotation(targetPos - item2.position);
					GameObject gameObject = SLOTGame.InstantiateFX(value3, item2.position, rotation);
					projectiles.Add(gameObject);
					mFollowingProjectiles.Add(gameObject.transform);
					UpdateProjectileFollower();
					VFXRotateParticleScript componentInChildren = gameObject.GetComponentInChildren<VFXRotateParticleScript>();
					if (componentInChildren != null)
					{
						componentInChildren.ShouldRotateParticle = true;
						componentInChildren.RotationAxis = rotation.eulerAngles;
					}
					bool flag = useCritCamera && projectiles.Count == 1 && lastAttack;
					Vector3 attackPosition = GetAttackPosition(target.Creature);
					if (creatureData.ShootVFXType != "Dragon")
					{
						if (flag)
						{
							Singleton<DWGameCamera>.Instance.SetObjectLock(ProjectileFollower);
						}
						iTween.MoveTo(gameObject, iTween.Hash("position", attackPosition, "time", creatureData.ShootTravelTime, "easetype", creatureData.ShootTravelStyle));
						continue;
					}
					if (flag)
					{
						Vector3 meleeAttackPosition = GetMeleeAttackPosition(attacker.Creature, target.Creature);
						Vector3 vector6 = meleeAttackPosition - Singleton<DWGameCamera>.Instance.MainCamLookAt.transform.position;
						Vector3 vector7 = Singleton<DWGameCamera>.Instance.MainCam.transform.position + vector6;
						iTween.MoveTo(Singleton<DWGameCamera>.Instance.MainCamLookAt.gameObject, iTween.Hash("position", meleeAttackPosition, "time", creatureData.ShootTravelTime, "easetype", iTween.EaseType.easeInCubic));
						iTween.MoveTo(Singleton<DWGameCamera>.Instance.MainCam.gameObject, iTween.Hash("position", vector7, "time", creatureData.ShootTravelTime, "easetype", iTween.EaseType.easeInCubic));
					}
					float distance = Vector3.Distance(item2.position, attackPosition);
					DragonShootModifier(gameObject, distance);
				}
			}
			yield return new WaitForSeconds(creatureData.ShootTravelTime);
			mFollowingProjectiles.Clear();
			foreach (GameObject item3 in projectiles)
			{
				StartCoroutine(KillParticleAfterDeath(item3));
			}
		}
		foreach (CreatureAttackTarget target2 in attacker.Targets)
		{
			CreatureHit creatureHit = target2.Hits[0];
			GameObject value4 = null;
			Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(creatureData.HitVFX, out value4);
			if (value4 == null)
			{
				value4 = SLOTResourceManager.Load("VFX/Creatures/" + creatureData.HitVFX, typeof(GameObject)) as GameObject;
			}
			Vector3 attackPosition2 = GetAttackPosition(target2.Creature);
			if (creatureHit.Shield)
			{
				GameObject gameObject2 = SLOTGame.InstantiateFX(ShieldHitFX, attackPosition2, Quaternion.identity);
				if (target2.Creature.Owner.Type == PlayerType.User)
				{
					gameObject2.transform.position = new Vector3(-13f, gameObject2.transform.position.y, gameObject2.transform.position.z);
					gameObject2.transform.eulerAngles = new Vector3(0f, 180f, 0f);
				}
			}
			else if (creatureHit.Miss)
			{
				Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Attack_Miss");
				DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(target2.Creature);
				ShowEffectLabelPopup(laneObject.CreatureObject.transform.position, laneObject, KFFLocalization.Get("!!MISS"), Color.white, Color.black);
			}
			else
			{
				if (value4 != null)
				{
					SLOTGame.InstantiateFX(value4, attackPosition2, Quaternion.identity);
				}
				if (creatureHit.Crit)
				{
					value4 = null;
					Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(creatureData.CritHitVFX, out value4);
					if (value4 == null)
					{
						value4 = SLOTResourceManager.Load("VFX/Creatures/" + creatureData.CritHitVFX, typeof(GameObject)) as GameObject;
					}
					if ((bool)value4)
					{
						Object.Instantiate(value4, attackPosition2, Quaternion.identity);
					}
				}
			}
			if (creatureHit.HeroReactType == HitReactType.Small)
			{
				int n = Random.Range(0, 2);
				Singleton<CharacterAnimController>.Instance.PlayRandomHitReaction(!attacker.Creature.Owner.Type, n);
			}
			else if (creatureHit.HeroReactType == HitReactType.Big)
			{
				Singleton<CharacterAnimController>.Instance.PlayHeroAnim(!attacker.Creature.Owner.Type, CharAnimType.BigActionReact);
			}
			ShowCreatureDamage(target2.Creature, (int)creatureHit.RawDamage, (int)creatureHit.CappedDamage, creatureHit.Crit, creatureHit.HitType, creatureHit.Shield || creatureHit.Miss, creatureHit.Killing);
		}
		if (isAiDragAttack)
		{
			Singleton<BattleLogController>.Instance.CanShowDragAttack = true;
		}
		if (!isMagicAttack)
		{
			yield return new WaitForSeconds(creatureData.MultiAttackInterval - creatureData.AttackStartTime);
		}
		else
		{
			yield return new WaitForSeconds(creatureData.MultiShootInterval - creatureData.ShootTravelTime - creatureData.ShootStartTime);
		}
		if (mTrailObj != null)
		{
			Object.Destroy(mTrailObj);
		}
		if (lastAttack)
		{
			if (!mInBattleFinishMoment)
			{
				Time.timeScale = PlayerPrefs.GetFloat("BattleSpeedMultiplier", 1f);
			}
			if (!isMagicAttack)
			{
				if (useCritCamera)
				{
					Singleton<DWGameCamera>.Instance.SetObjectLock(null);
				}
				iTween.MoveTo(attackerObj, iTween.Hash("position", attackerLane.AttackStartPosition, "time", creatureData.AttackMovementTime, "easetype", iTween.EaseType.easeInOutQuad));
				yield return new WaitForSeconds(creatureData.AttackMovementTime);
			}
			else
			{
				yield return new WaitForSeconds(RangedAttackFinishTime);
				if (useCritCamera)
				{
					Singleton<DWGameCamera>.Instance.SetObjectLock(null);
				}
			}
			float y = ((attacker.Creature.Owner.Type != PlayerType.User) ? (-90f) : 90f);
			iTween.RotateTo(attackerObj, iTween.Hash("rotation", new Vector3(0f, y, 0f), "time", RotateToAttackTime, "easetype", iTween.EaseType.easeInOutCubic));
			if (useCritCamera)
			{
				Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
			}
		}
		mCreatureAttacksInProgress--;
	}

	private void ShowCreatureDamage(CreatureState creature, int rawAmount, int cappedAmount, bool isCrit, DamageType type, bool isMissOrShield, bool isFatal)
	{
		Singleton<BattleHudController>.Instance.ShowCreatureDamage(creature, rawAmount, cappedAmount, isCrit, type);
		if (!isMissOrShield)
		{
			FlashDamagedCreature(creature);
			PlayCreatureAnim(creature, "HitReaction");
			if (isCrit)
			{
				Singleton<DWGameCamera>.Instance.CameraShake(CritCameraShake, CritCameraShakeTime);
			}
			else
			{
				Singleton<DWGameCamera>.Instance.CameraShake(NormalCameraShake, NormalCameraShakeTime);
			}
		}
		if (!isFatal)
		{
			return;
		}
		Singleton<BattleHudController>.Instance.PlayWhiteScreenFlashTween();
		int num = 0;
		foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[creature.Owner.Type.IntValue])
		{
			if (!item.DyingThisFrame)
			{
				num++;
			}
		}
		Singleton<DWBattleLane>.Instance.GetLaneObject(creature).DyingThisFrame = true;
		if (num == 1)
		{
			if (creature.Owner.Type == PlayerType.Opponent)
			{
				RegisterPlayerWin();
			}
			else
			{
				RegisterPlayerLoss();
			}
			Singleton<PauseController>.Instance.HideIfShowing();
			Singleton<PauseController>.Instance.HideButton();
			Singleton<QuickMessageController>.Instance.HideIfShowing();
			Singleton<QuickMessageController>.Instance.HideButton();
			StartCoroutine(BattleFinishMoment(creature.Owner.Type));
		}
	}

	public void RegisterPlayerWin()
	{
		DetachedSingleton<MissionManager>.Instance.ProcessWin();
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<MultiplayerMessageHandler>.Instance.SendLeaveGame("won");
			PvpMatchResultDetails resultDetails = Singleton<PlayerInfoScript>.Instance.RegisterPvpMatchResult(won: true);
			Singleton<PvpBattleResultsController>.Instance.RegisterResults(resultDetails);
			Singleton<PlayerInfoScript>.Instance.Save();
			return;
		}
		if (Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
		{
			TutorialDataManager.Instance.GetBlock("Q1").Completed = true;
		}
		else if (Singleton<TutorialController>.Instance.IsBlockActive("Q2"))
		{
			TutorialDataManager.Instance.GetBlock("Q2").Completed = true;
		}
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		if (currentActiveQuest.IsRandomDungeonBattle && Singleton<PlayerInfoScript>.Instance.StateData.NoReviveRandomDungeonRun && (int)Singleton<PlayerInfoScript>.Instance.SaveData.RandomDungeonLevel < RandomDungeonFloorDataManager.Instance.GetDatabase().Count && currentActiveQuest.IsFinalRandomBattle)
		{
			++Singleton<PlayerInfoScript>.Instance.SaveData.RandomDungeonLevel;
		}
		Singleton<BattleResultsController>.Instance.PopulateRewards();
		if (currentActiveQuest.BroadcastWin)
		{
			Singleton<PlayerInfoScript>.Instance.StateData.QuestWinToBroadcast = currentActiveQuest;
		}
	}

	public void RegisterPlayerLoss()
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<MultiplayerMessageHandler>.Instance.SendLeaveGame("lost");
			PvpMatchResultDetails resultDetails = Singleton<PlayerInfoScript>.Instance.RegisterPvpMatchResult(won: false);
			Singleton<PvpBattleResultsController>.Instance.RegisterResults(resultDetails);
			Singleton<PlayerInfoScript>.Instance.Save();
		}
	}

	private IEnumerator BattleFinishMoment(PlayerType loser)
	{
		mInBattleFinishMoment = true;
		if (Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
		{
			UICamera.LockInput();
		}
		if (loser == PlayerType.Opponent)
		{
			Singleton<SLOTMusic>.Instance.PlayVictoryMusic();
		}
		else
		{
			Singleton<SLOTMusic>.Instance.PlayLoserMusic();
		}
		Time.timeScale = 0.2f;
		yield return new WaitForSeconds(2f * Time.timeScale);
		Time.timeScale = PlayerPrefs.GetFloat("BattleSpeedMultiplier", 1f);
		mInBattleFinishMoment = false;
	}

	private void ShowCreatureHealing(CreatureState creature, int rawAmount, int cappedAmount)
	{
		Singleton<BattleHudController>.Instance.ShowCreatureHealing(creature, rawAmount, cappedAmount);
	}

	private bool IsTransitioningBetweenWaves()
	{
		return Singleton<DWGame>.Instance.GetCurrentGameState() == GameState.EndGameWait;
	}

	private IEnumerator PlayHeroCardDrawAnim(PlayerType player)
	{
		if (player == PlayerType.User)
		{
			Singleton<HandCardController>.Instance.HideHand();
			Singleton<HandCardController>.Instance.HideOpponentTween.Play();
			Singleton<BattleHudController>.Instance.HideHudForInfoPopupTween.Play();
			Singleton<BattleHudController>.Instance.PlayFloopBanner();
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Winner();
		}
		else
		{
			Singleton<DWGameCamera>.Instance.MoveCameraToP2Setup();
		}
		yield return new WaitForSeconds(0.5f);
		Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(Singleton<DWGame>.Instance.GetCharacter(player), VOEvent.DrawLeaderCard);
		if (!mHeroCardAlreadyPlayed[player.IntValue])
		{
			mHeroCardAlreadyPlayed[player.IntValue] = true;
			Singleton<CharacterAnimController>.Instance.PlayHeroAnim(player, CharAnimType.HeroCard_01);
		}
		else
		{
			Singleton<CharacterAnimController>.Instance.PlayHeroAnim(player, CharAnimType.HeroCard_00);
		}
		BattleCharacterAnimState animState = Singleton<CharacterAnimController>.Instance.playerAnimState[player];
		bool isDone = false;
		float timePast = 0f;
		while (!isDone)
		{
			timePast += Time.deltaTime;
			if (animState.GetCurrentAnimType() == CharAnimType.HeroCard_00 || animState.GetCurrentAnimType() == CharAnimType.HeroCard_01)
			{
				if (animState.IsCurrentAnimDone())
				{
					isDone = true;
				}
				else
				{
					yield return null;
				}
			}
			else if (timePast >= 2.66f)
			{
				isDone = true;
			}
			else
			{
				yield return null;
			}
		}
		Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
		yield return new WaitForSeconds(0.2f);
		if (player == PlayerType.User)
		{
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: false);
			Singleton<HandCardController>.Instance.ShowHand();
			Singleton<HandCardController>.Instance.ShowOpponentTween.Play();
			Singleton<BattleHudController>.Instance.ShowHudAfterInfoPopupTween.Play();
		}
	}

	private IEnumerator KillParticleAfterDeath(GameObject particleObj)
	{
		ParticleSystem[] componentsInChildren = particleObj.GetComponentsInChildren<ParticleSystem>();
		ParticleSystem[] array = componentsInChildren;
		foreach (ParticleSystem particleSystem in array)
		{
			if (particleSystem != null)
			{
				particleSystem.loop = false;
				particleSystem.enableEmission = false;
				particleSystem.Stop();
			}
		}
		ParticleSystem[] array2 = componentsInChildren;
		ParticleSystem[] array3 = array2;
		foreach (ParticleSystem ps2 in array3)
		{
			while (ps2 != null && ps2.IsAlive(withChildren: false))
			{
				yield return null;
			}
		}
		Object.Destroy(particleObj);
	}

	private float GetMinMaxDistanceBetweenCreatures(bool min)
	{
		float num = ((!min) ? 0f : 100f);
		foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[0])
		{
			foreach (DWBattleLaneObject item2 in Singleton<DWBattleLane>.Instance.BattleLaneObjects[1])
			{
				float b = Vector3.Distance(item.transform.position, item2.transform.position);
				num = ((!min) ? Mathf.Max(num, b) : Mathf.Min(num, b));
			}
		}
		return num;
	}

	private void DragonShootModifier(GameObject obj, float distance)
	{
		float minMaxDistanceBetweenCreatures = GetMinMaxDistanceBetweenCreatures(min: true);
		ParticleSystem[] componentsInChildren = obj.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].startSpeed *= distance / minMaxDistanceBetweenCreatures;
		}
	}

	private void UpdateProjectileFollower()
	{
		if (mFollowingProjectiles.Count == 0)
		{
			return;
		}
		Vector3 zero = Vector3.zero;
		foreach (Transform mFollowingProjectile in mFollowingProjectiles)
		{
			if (mFollowingProjectile != null)
			{
				zero += mFollowingProjectile.position;
			}
		}
		zero /= (float)mFollowingProjectiles.Count;
		ProjectileFollower.position = Vector3.SmoothDamp(ProjectileFollower.position, zero, ref followVelocity, 0.08f);
	}

	public void SetDragDrawCard(CardPrefabScript card)
	{
		mDragDrawCard = card;
	}

	public CardPrefabScript RetrieveDragDrawCard()
	{
		CardPrefabScript cardPrefabScript = mDragDrawCard;
		mDragDrawCard = null;
		if (cardPrefabScript != null)
		{
			cardPrefabScript.SetTargetAlpha(1f, 20f);
		}
		return cardPrefabScript;
	}

	public void ResetForReplay()
	{
		StopAllCoroutines();
	}
}
