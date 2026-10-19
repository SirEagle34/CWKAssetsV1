using UnityEngine;

public class LeaderboardRowNGUI : MonoBehaviour
{
	public UILabel placementLabel;

	public UILabel idLabel;

	public UILabel nameLabel;

	public UILabel levelLabel;

	public UILabel pointsLabel;

	public const int RowHeight = 64;

	private const int Width = 280;

	private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

	private static readonly Color Silver = new Color(0.85f, 0.88f, 0.95f);

	private static readonly Color Bronze = new Color(0.9f, 0.6f, 0.35f);

	private bool mStripe;

	private bool mLaidOut;

	public void SetData(int placement, string id, string name, int level, int points, bool isMe)
	{
		mStripe = placement % 2 == 0;
		Layout(isMe);
		UILabel uILabel = Child<UILabel>("Val_Placement");
		UILabel uILabel2 = Child<UILabel>("Val_Title");
		UILabel uILabel3 = Child<UILabel>("Val_point");
		if (uILabel != null)
		{
			uILabel.text = placement.ToString();
		}
		if (uILabel2 != null)
		{
			uILabel2.text = name;
		}
		PvpRankData rank = PvpRankDataManager.Instance.GetRank(level);
		UILabel uILabel4 = Child<UILabel>("Val_point (3)");
		if (uILabel4 != null)
		{
			uILabel4.text = ((rank != null) ? rank.RankName : string.Empty);
		}
		if (uILabel3 != null)
		{
			uILabel3.text = points + " " + RevivalText.Translate("pts");
		}
		UITexture uITexture = Child<UITexture>("League Texture");
		if (uITexture != null)
		{
			uITexture.color = placement switch
			{
				3 => Bronze, 
				2 => Silver, 
				1 => Gold, 
				_ => new Color(0.55f, 0.6f, 0.75f), 
			};
		}
	}

	private void Layout(bool isMe)
	{
		UISprite uISprite = Child<UISprite>("Rect");
		if (uISprite != null)
		{
			Place(uISprite.transform, 0f, 0f);
			uISprite.width = 280;
			uISprite.height = 58;
			uISprite.color = (isMe ? new Color(0.72f, 0.48f, 0.1f) : (mStripe ? new Color(0.12f, 0.2f, 0.42f) : new Color(0.16f, 0.26f, 0.52f)));
		}
		if (mLaidOut)
		{
			return;
		}
		mLaidOut = true;
		LabelShadow[] componentsInChildren = GetComponentsInChildren<LabelShadow>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Object.DestroyImmediate(componentsInChildren[i]);
		}
		CurrencyUI.RemoveCopiedShadows(base.gameObject);
		BoxCollider component = GetComponent<BoxCollider>();
		if (component != null)
		{
			component.center = Vector3.zero;
			component.size = new Vector3(280f, 64f, 1f);
		}
		UITexture uITexture = Child<UITexture>("League Texture");
		if (uITexture != null)
		{
			Place(uITexture.transform, -110f, 0f);
			uITexture.width = 46;
			uITexture.height = 46;
		}
		Style(Child<UILabel>("Val_Placement"), -110f, 0f, 40, 20, UIWidget.Pivot.Center);
		UILabel label = Child<UILabel>("Val_Title");
		Style(label, -78f, 10f, 140, 20, UIWidget.Pivot.Left);
		Outline(label, Color.white);
		UILabel label2 = Child<UILabel>("Val_point (3)");
		Style(label2, -78f, -12f, 140, 15, UIWidget.Pivot.Left);
		Outline(label2, new Color(1f, 0.84f, 0.3f));
		UILabel label3 = Child<UILabel>("Val_point");
		Style(label3, 128f, 0f, 70, 18, UIWidget.Pivot.Right);
		Outline(label3, Color.white);
		Outline(Child<UILabel>("Val_Placement"), Color.white);
		string[] array = new string[3] { "Val_point (1)", "Val_point (2)", "Val_ID" };
		foreach (string n in array)
		{
			Transform transform = base.transform.Find(n);
			if (transform != null)
			{
				transform.gameObject.SetActive(value: false);
			}
		}
	}

	private static void Outline(UILabel label, Color color)
	{
		if (!(label == null))
		{
			label.color = color;
			label.effectStyle = UILabel.Effect.Outline;
			label.effectColor = new Color(0.04f, 0.06f, 0.16f);
		}
	}

	private T Child<T>(string childName) where T : Component
	{
		Transform transform = base.transform.Find(childName);
		if (!(transform != null))
		{
			return null;
		}
		return transform.GetComponent<T>();
	}

	private static void Place(Transform t, float x, float y)
	{
		t.localPosition = new Vector3(x, y, 0f);
		t.localScale = Vector3.one;
	}

	private static void Style(UILabel label, float x, float y, int width, int size, UIWidget.Pivot pivot)
	{
		if (!(label == null))
		{
			label.gameObject.SetActive(value: true);
			label.pivot = pivot;
			label.alignment = pivot switch
			{
				UIWidget.Pivot.Right => NGUIText.Alignment.Right, 
				UIWidget.Pivot.Left => NGUIText.Alignment.Left, 
				_ => NGUIText.Alignment.Center, 
			};
			Place(label.transform, x, y);
			label.overflowMethod = UILabel.Overflow.ShrinkContent;
			label.width = width;
			label.height = size + 6;
			label.fontSize = size;
		}
	}
}
