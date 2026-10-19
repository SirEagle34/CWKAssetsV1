using UnityEngine;

public class IntroSalePopupController : Singleton<IntroSalePopupController>
{
	public UITweenController Mode1ShowTween;
	public UITweenController Mode1HideTween;

	public UITweenController Mode2ShowTween;
	public UITweenController Mode2HideTween;

	public GameObject Mode1Panel;
	public GameObject Mode2Panel;

	public UILabel Mode1HeaderLabel;
	public UILabel Mode1HardCurrencyLabel;
	public UILabel Mode1HardCurrencyLabelShadow;
	public UILabel Mode1SocialCurrencyLabel;
	public UILabel Mode1SoftCurrencyLabel;
	public UILabel Mode1PriceLabel;
	public UILabel Mode1PriceLabelShadow;

	public UILabel Mode2HardCurrencyLabel;
	public UILabel Mode2SocialCurrencyLabel;
	public UILabel Mode2SoftCurrencyLabel;
	public UILabel Mode2PriceLabel;
	public UILabel Mode2PriceLabelShadow;

	public UILabel Mode2PercentLabel;
	public UILabel Mode2PercentLabelShadow;

	private StoreScreenController.FoundProductData mIntroPackage;

	private bool mMode1;

	public int GetTestMode()
	{
		return mMode1 ? 1 : 2;
	}

	public void Show(StoreScreenController.FoundProductData introPackage)
	{
		mIntroPackage = introPackage;

		CurrencyPackageData package = mIntroPackage.PackageData;

		string priceText = package.FreeHardCurrency.ToString();

		if (Random.Range(0, 2) == 0)
		{
			mMode1 = true;

			Mode1ShowTween.Play();

			Mode1Panel.SetActive(true);
			Mode2Panel.SetActive(false);

			Mode1HeaderLabel.text =
				package.PaidHardCurrency.ToString();

			Mode1HardCurrencyLabel.text =
				package.FreeHardCurrency.ToString();

			Mode1HardCurrencyLabelShadow.text =
				Mode1HardCurrencyLabel.text;

			Mode1SocialCurrencyLabel.text =
				package.SocialCurrency.ToString();

			Mode1SoftCurrencyLabel.text =
				package.SoftCurrency.ToString();

			Mode1PriceLabel.text = priceText;
			Mode1PriceLabelShadow.text = priceText;
		}
		else
		{
			mMode1 = false;

			Mode2ShowTween.Play();

			Mode1Panel.SetActive(false);
			Mode2Panel.SetActive(true);

			int percent =
				100 * package.TotalHardCurrency /
				Mathf.Max(1, package.PaidHardCurrency);

			Mode2PercentLabel.text =
				"A " + percent + "% Value!";

			Mode2PercentLabelShadow.text =
				Mode2PercentLabel.text;

			Mode2HardCurrencyLabel.text =
				package.TotalHardCurrency.ToString();

			Mode2SocialCurrencyLabel.text =
				package.SocialCurrency.ToString();

			Mode2SoftCurrencyLabel.text =
				package.SoftCurrency.ToString();

			Mode2PriceLabel.text = priceText;
			Mode2PriceLabelShadow.text = priceText;
		}
	}

	public void OnClickAccept()
	{
		if (mIntroPackage == null ||
			mIntroPackage.PackageData == null)
		{
			return;
		}

		StoreScreenController.GrantOldProduct(
			mIntroPackage.PackageData.ID
		);

		Close();
	}

	public void OnClickDecline()
	{
		Close();
	}

	private void Close()
	{
		if (mMode1)
		{
			Mode1HideTween.Play();
		}
		else
		{
			Mode2HideTween.Play();
		}

		if (Singleton<TutorialController>.Instance
			.IsBlockActive("Bank"))
		{
			Singleton<TutorialController>.Instance
				.AdvanceTutorialState();
		}
	}
}