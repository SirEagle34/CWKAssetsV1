using System.Collections;
using UnityEngine;

public class DWBattleLootObject : MonoBehaviour
{
	public PlayerType player;

	public int lane;

	public GameObject LootFlyFXPrefab;

	public GameObject LootAddFXPrefab;

	public ParticleSystem MainParticle;

	public UITexture UITex;

	public Transform UITexParent;

	public Texture2D EvoTexture;

	public GameObject AdditionalSpawnVFXPrefab;

	public Transform AdditionalSpawnVFXParent;

	private GameObject mAdditionalVFXObj;

	public bool SpawnAdditionalVFX = true;

	public GameObject ExtraSparkleFX;

	public bool PickUp;

	public float PickedUpScale;

	private void Start()
	{
		if (AdditionalSpawnVFXPrefab != null && SpawnAdditionalVFX)
		{
			if (AdditionalSpawnVFXParent != null)
			{
				mAdditionalVFXObj = AdditionalSpawnVFXParent.InstantiateAsChild(AdditionalSpawnVFXPrefab);
			}
			else
			{
				mAdditionalVFXObj = base.transform.InstantiateAsChild(AdditionalSpawnVFXPrefab);
			}
		}
	}

	private void OnClick()
	{
		GetComponent<Collider>().enabled = false;
		TriggerLootFly();
		RemoveAdditionalVFX();
	}

	public void RemoveAdditionalVFX()
	{
		if (mAdditionalVFXObj != null)
		{
			Object.Destroy(mAdditionalVFXObj);
		}
	}

	public void TriggerLootFly()
	{
		StartCoroutine(LootFlySequence());
	}

	private IEnumerator LootFlySequence()
	{
		Singleton<TutorialController>.Instance.AdvanceIfOnState("Q1_Drop2");
		Vector2 vector = Singleton<DWGameCamera>.Instance.MainCam.WorldToScreenPoint(base.transform.position);
		Vector3 position = Singleton<DWGameCamera>.Instance.BattleUICam.ScreenToWorldPoint(vector);
		position.z = 0f;
		base.transform.position = position;
		Transform nextLootContainer = Singleton<BattleHudController>.Instance.GetNextLootContainer();
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		if (componentsInChildren.Length != 0)
		{
			ParticleSystem[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
		}
		UITweener componentInChildren = GetComponentInChildren<UITweener>();
		if (componentInChildren != null)
		{
			componentInChildren.ResetToZero();
			Object.Destroy(componentInChildren);
		}
		base.transform.localScale = Vector3.zero;
		base.transform.parent = nextLootContainer;
		base.transform.localEulerAngles = Vector3.zero;
		_ = base.transform.localPosition;
		base.gameObject.ChangeLayer(Singleton<BattleHudController>.Instance.gameObject.layer);
		GameObject trailFx = null;
		if (LootFlyFXPrefab != null)
		{
			trailFx = base.transform.InstantiateAsChild(LootFlyFXPrefab);
		}
		float lootFlyTime = Singleton<BattleHudController>.Instance.LootFlyTime;
		Random.Range(Singleton<BattleHudController>.Instance.LootFlyAmplitudeX, 0f - Singleton<BattleHudController>.Instance.LootFlyAmplitudeX);
		Vector3 eulers = new Vector3(0f, 0f, 90f);
		Animator componentInChildren2 = GetComponentInChildren<Animator>();
		if (componentInChildren2 != null)
		{
			componentInChildren2.transform.Rotate(eulers);
		}
		iTween.MoveTo(base.gameObject, iTween.Hash("position", Vector3.zero, "islocal", true, "time", lootFlyTime, "lookahead", 0.5f, "easetype", iTween.EaseType.linear));
		iTween.ScaleTo(base.gameObject, new Vector3(PickedUpScale, PickedUpScale, PickedUpScale), lootFlyTime);
		yield return new WaitForSeconds(lootFlyTime);
		Object.Destroy(trailFx);
		if (LootAddFXPrefab != null)
		{
			base.transform.InstantiateAsChild(LootAddFXPrefab);
		}
		RewardManager.CreatureDropsPickedUp++;
		Singleton<BattleHudController>.Instance.UpdateLootCount();
		if (!PickUp)
		{
			yield break;
		}
		Singleton<BattleHudController>.Instance.LootCreatureTween.Play();
		yield return new WaitForSeconds(0.5f);
		Singleton<DWBattleLane>.Instance.CurrentBattleLootInventoryObjects.RemoveAt(0);
		if (Singleton<DWBattleLane>.Instance.CurrentBattleLootInventoryObjects.Count == 0)
		{
			Singleton<BattleHudController>.Instance.HideTapToCollectBanner();
			if (!Singleton<DWGame>.Instance.IsGameOver())
			{
				Singleton<DWBattleLane>.Instance.ResetLaneColliders(enable: true);
			}
		}
		else
		{
			Singleton<DWBattleLane>.Instance.TriggerBattleLootCollect();
		}
	}

	private GameObject GetDebugSphere(Vector3 pos)
	{
		GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		obj.transform.position = pos;
		obj.transform.localScale = Vector3.one * 0.1f;
		SLOTGame.SetLayerRecursive(obj, LayerMask.NameToLayer("GUI"));
		return obj;
	}

	private void Update()
	{
	}

	public void AlignTexToCamera(Camera cam)
	{
		if (UITexParent != null)
		{
			UITexParent.transform.LookAt(cam.transform);
		}
	}

	public void DropEvoMaterialForPostMatchIcon()
	{
		UITweener componentInChildren = GetComponentInChildren<UITweener>();
		if (componentInChildren != null)
		{
			Object.Destroy(componentInChildren);
		}
		base.transform.AddLocalPositionY(40f);
		SpawnAdditionalVFX = false;
		AlignTexToCamera(Singleton<DWGameCamera>.Instance.BattleUICam);
	}
}
