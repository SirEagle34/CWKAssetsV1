using UnityEngine;

public class FrontEndPIP : MonoBehaviour
{
	public int player;

	public LeaderData CurrentLeader;

	public GameObject leaderObj;

	private Camera PIP_Camera;

	private GameObject mLeaderRendererFor;

	private Renderer mLeaderRenderer;

	private Vector2 Size;

	private Rect VPRect;

	private GameObject mCamAlignObj;

	private UIWidget mIconPortrait;

	private Camera mUICam;

	private bool mInitDone;

	public bool mShowPIP;

	private GameObject mFramedLogFor;

	public void Init(int n, LeaderData leader, GameObject thisLeaderObj, UIWidget iconPortrait)
	{
		CurrentLeader = leader;
		leaderObj = thisLeaderObj;
		player = n;
		PIP_Camera = GetComponent<Camera>();
		mUICam = Singleton<TownController>.Instance.GetUICam();
		string text = ((n != 0) ? CurrentLeader.PIPTransformP2 : CurrentLeader.PIPTransformP1);
		mCamAlignObj = Singleton<FrontEndPIPController>.Instance.gameObject.FindInChildren(text);
		PIP_Camera.transform.position = mCamAlignObj.transform.position;
		mIconPortrait = iconPortrait;
		mLeaderRendererFor = null;
		mLeaderRenderer = null;
		mInitDone = true;
	}

	public void ShowPortrait(bool show)
	{
		mShowPIP = show;
	}

	private void Update()
	{
		if (!mInitDone)
		{
			return;
		}
		PIP_Camera.enabled = mShowPIP;
		if (!mShowPIP)
		{
			return;
		}
		Vector3[] worldCorners = mIconPortrait.worldCorners;
		Vector3 vector = mUICam.WorldToScreenPoint(worldCorners[0]);
		Vector3 vector2 = mUICam.WorldToScreenPoint(worldCorners[2]);
		vector.x /= Screen.width;
		vector.y /= Screen.height;
		vector2.x /= Screen.width;
		vector2.y /= Screen.height;
		Size.x = vector2.x - vector.x;
		Size.y = vector2.y - vector.y;
		VPRect.Set(vector.x, vector.y, Size.x, Size.y);
		PIP_Camera.rect = VPRect;
		if (mLeaderRendererFor != leaderObj)
		{
			mLeaderRendererFor = leaderObj;
			mLeaderRenderer = ((leaderObj != null) ? leaderObj.GetComponentInChildren<Renderer>() : null);
		}
		Renderer renderer = mLeaderRenderer;
		if (renderer != null)
		{
			if (mFramedLogFor != leaderObj)
			{
				mFramedLogFor = leaderObj;
				Vector3 center = renderer.bounds.center;
				Vector3 vector3 = PIP_Camera.WorldToViewportPoint(center);
				string[] obj = new string[24]
				{
					"[PIPFrame] leader='",
					(CurrentLeader != null) ? CurrentLeader.Prefab : "?",
					"' align='",
					(CurrentLeader != null) ? CurrentLeader.PIPTransformP1 : "?",
					"' camPos=",
					PIP_Camera.transform.position.ToString(),
					" camRot=",
					PIP_Camera.transform.eulerAngles.ToString(),
					" fov=",
					PIP_Camera.fieldOfView.ToString(),
					" rect=",
					PIP_Camera.rect.ToString(),
					" meshC=",
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				Vector3 vector4 = center;
				obj[13] = vector4.ToString();
				obj[14] = " viewport=";
				vector4 = vector3;
				obj[15] = vector4.ToString();
				obj[16] = " (inFront=";
				obj[17] = (vector3.z > 0f).ToString();
				obj[18] = " onX=";
				obj[19] = (vector3.x > 0f && vector3.x < 1f).ToString();
				obj[20] = " onY=";
				obj[21] = (vector3.y > 0f && vector3.y < 1f).ToString();
				obj[22] = ") isVisible=";
				obj[23] = renderer.isVisible.ToString();
				Debug.Log(string.Concat(obj));
			}
			if (!renderer.isVisible)
			{
				PIP_Camera.fieldOfView = 40f;
			}
			else
			{
				PIP_Camera.fieldOfView = 30f;
			}
		}
	}
}
