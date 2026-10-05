using System.Collections;
using UnityEngine;

public class AssetBundleDownloadController : MonoBehaviour
{
	public GameObject Background;

	public UITexture LoadingLogo;

	private void Start()
	{
		Background.SetActive(true);
		LoadingScreenController.LoadLoadingScreenLogo(LoadingLogo);

		StartCoroutine(LoadPrimaryBundles());
	}

	private IEnumerator LoadPrimaryBundles()
	{
		yield return null;

		SLOTResourceManager resourceManager =
			Singleton<SLOTResourceManager>.Instance;

		// Bundle'lar yoksa indirir.
		// Bundle'lar varsa mevcut dosyaları kullanır.
		// Yeni sürüm varsa sadece gerekli bundle'ları günceller.
		yield return StartCoroutine(
			resourceManager.LoadPrimaryBundlesCouroutine()
		);

		Singleton<FontManager>.Instance.LoadLanguages();

		DetachedSingleton<SceneFlowManager>.Instance.LoadConnectSceneDirect();
	}

	private void OnDestroy()
	{
		if (LoadingLogo != null)
		{
			LoadingLogo.UnloadTexture();
		}
	}
}