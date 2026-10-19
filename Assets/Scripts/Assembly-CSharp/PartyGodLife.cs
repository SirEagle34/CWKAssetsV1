using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyGodLife : MonoBehaviour
{
	private const float BobHeight = 0.025f;

	private const float BobSeconds = 2.4f;

	private const float TurnDegrees = 20f;

	private const float BrowLift = 0.022f;

	private const float JawDegrees = 12f;

	private const float OutlineWidth = 0.006f;

	private BattleCharacterAnimState mState;

	private Transform mJaw;

	private Transform mBrow;

	private Vector3 mBrowRest;

	private Quaternion mJawRest;

	private float mHeight = 1f;

	private Vector3 mAppliedPos;

	private Quaternion mAppliedRot = Quaternion.identity;

	private float mTurn;

	private float mTurnSide = 1f;

	private float mTalk;

	private bool mWasCard;

	private Color32[] mSkin;

	private int mSkinSize;

	public static void Attach(GameObject root)
	{
		if (root.GetComponent<PartyGodLife>() == null)
		{
			root.AddComponent<PartyGodLife>().Setup();
		}
	}

	private void Setup()
	{
		mState = GetComponent<BattleCharacterAnimState>();
		SkinnedMeshRenderer componentInChildren = GetComponentInChildren<SkinnedMeshRenderer>();
		if (!(componentInChildren == null) && !(componentInChildren.sharedMesh == null))
		{
			Toonify(componentInChildren);
			if (componentInChildren.sharedMesh.isReadable)
			{
				AddJaw(componentInChildren);
				AddOutline(componentInChildren);
			}
		}
	}

	private static bool IsCard(CharAnimType t)
	{
		if (t != CharAnimType.PlayCard && t != CharAnimType.PlayBigCard && t != CharAnimType.HeroCard_00)
		{
			return t == CharAnimType.HeroCard_01;
		}
		return true;
	}

	private static bool IsTalking(CharAnimType t)
	{
		if (!IsCard(t) && t != CharAnimType.HR1 && t != CharAnimType.HR2 && t != CharAnimType.BigActionReact && t != CharAnimType.Fidget_Taunt && t != CharAnimType.P1Intro && t != CharAnimType.P2Intro && t != CharAnimType.Win)
		{
			return t == CharAnimType.Lose;
		}
		return true;
	}

	private void Update()
	{
		Undo();
	}

	private void LateUpdate()
	{
		CharAnimType t = ((mState != null) ? mState.GetCurrentAnimType() : CharAnimType.Idle);
		bool flag = IsCard(t);
		if (flag && !mWasCard)
		{
			mTurnSide = 0f - mTurnSide;
		}
		mWasCard = flag;
		float num = 1f - Mathf.Exp(-6f * Time.deltaTime);
		mTurn = Mathf.Lerp(mTurn, flag ? mTurnSide : 0f, num);
		mTalk = Mathf.Lerp(mTalk, IsTalking(t) ? 1f : 0f, num * 2f);
		Apply(Time.time, mTurn, mTalk);
	}

	public void Undo()
	{
		base.transform.localPosition -= mAppliedPos;
		base.transform.localRotation = base.transform.localRotation * Quaternion.Inverse(mAppliedRot);
		mAppliedPos = Vector3.zero;
		mAppliedRot = Quaternion.identity;
	}

	public void Apply(float time, float turn, float talk)
	{
		float num = time * MathF.PI * 2f / 2.4f;
		Vector3 b = new Vector3(0f, Mathf.Sin(num) * 0.025f * mHeight, 0f);
		mAppliedPos = base.transform.localRotation * Vector3.Scale(base.transform.localScale, b);
		mAppliedRot = Quaternion.Euler(0f, turn * 20f, Mathf.Sin(num * 0.5f) * 2f);
		base.transform.localPosition += mAppliedPos;
		base.transform.localRotation = base.transform.localRotation * mAppliedRot;
		if (mJaw != null)
		{
			float num2 = Mathf.Clamp01(0.5f + 0.35f * Mathf.Sin(time * 8f) + 0.2f * Mathf.Sin(time * 13f + 1f));
			mJaw.localRotation = mJawRest * Quaternion.AngleAxis(num2 * talk * 12f, Vector3.right);
		}
		if (mBrow != null)
		{
			float num3 = talk * (0.55f + 0.45f * Mathf.Sin(time * 3.3f)) + (1f - talk) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 1.3f)), 12f);
			Vector3 vector = base.transform.TransformVector(Vector3.up * num3 * 0.022f * mHeight);
			mBrow.localPosition = mBrowRest + mBrow.parent.InverseTransformVector(vector);
		}
	}

	private void Toonify(SkinnedMeshRenderer smr)
	{
		Material sharedMaterial = smr.sharedMaterial;
		if (sharedMaterial == null || sharedMaterial.mainTexture == null)
		{
			return;
		}
		RenderTexture temporary = RenderTexture.GetTemporary(1024, 1024, 0);
		Graphics.Blit(sharedMaterial.mainTexture, temporary);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = temporary;
		Texture2D texture2D = new Texture2D(1024, 1024, TextureFormat.RGB24, mipChain: true);
		texture2D.ReadPixels(new Rect(0f, 0f, 1024f, 1024f), 0, 0);
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		Color32[] pixels = texture2D.GetPixels32();
		if (sharedMaterial.mainTexture.name == "partygod_toon")
		{
			mSkin = pixels;
			mSkinSize = 1024;
			UnityEngine.Object.Destroy(texture2D);
			return;
		}
		int num = 1025;
		double[] array = new double[num * num];
		double[] array2 = new double[num * num];
		double[] array3 = new double[num * num];
		double[] array4 = new double[num * num];
		double[] array5 = new double[num * num];
		for (int i = 0; i < 1024; i++)
		{
			for (int j = 0; j < 1024; j++)
			{
				Color32 color = pixels[i * 1024 + j];
				double num2 = 0.299 * (double)(int)color.r + 0.587 * (double)(int)color.g + 0.114 * (double)(int)color.b;
				int num3 = (i + 1) * num + j + 1;
				array[num3] = (double)(int)color.r + array[num3 - 1] + array[num3 - num] - array[num3 - num - 1];
				array2[num3] = (double)(int)color.g + array2[num3 - 1] + array2[num3 - num] - array2[num3 - num - 1];
				array3[num3] = (double)(int)color.b + array3[num3 - 1] + array3[num3 - num] - array3[num3 - num - 1];
				array4[num3] = num2 + array4[num3 - 1] + array4[num3 - num] - array4[num3 - num - 1];
				array5[num3] = num2 * num2 + array5[num3 - 1] + array5[num3 - num] - array5[num3 - num - 1];
			}
		}
		Color32[] array6 = new Color32[pixels.Length];
		for (int k = 0; k < 1024; k++)
		{
			for (int l = 0; l < 1024; l++)
			{
				double num4 = double.MaxValue;
				double num5 = 0.0;
				double num6 = 0.0;
				double num7 = 0.0;
				for (int m = 0; m < 4; m++)
				{
					int num8 = Mathf.Clamp(((m & 1) == 0) ? (l - 2) : l, 0, 1023);
					int num9 = Mathf.Clamp(((m & 1) == 0) ? l : (l + 2), 0, 1023);
					int num10 = Mathf.Clamp(((m & 2) == 0) ? (k - 2) : k, 0, 1023);
					int num11 = Mathf.Clamp(((m & 2) == 0) ? k : (k + 2), 0, 1023);
					double num12 = (num9 - num8 + 1) * (num11 - num10 + 1);
					double num13 = Box(array4, num, num8, num10, num9, num11) / num12;
					double num14 = Box(array5, num, num8, num10, num9, num11) / num12 - num13 * num13;
					if (num14 < num4)
					{
						num4 = num14;
						num5 = Box(array, num, num8, num10, num9, num11) / num12;
						num6 = Box(array2, num, num8, num10, num9, num11) / num12;
						num7 = Box(array3, num, num8, num10, num9, num11) / num12;
					}
				}
				Color.RGBToHSV(new Color((float)num5 / 255f, (float)num6 / 255f, (float)num7 / 255f), out var H, out var S, out var V);
				V = Mathf.Lerp(V, Mathf.Min(1f, (Mathf.Round(V * 5f) + 0.35f) / 5f), 0.6f);
				S = Mathf.Min(1f, S * 1.3f);
				V = Mathf.Pow(V, 0.85f);
				array6[k * 1024 + l] = Color.HSVToRGB(H, S, V);
			}
		}
		PaintTeeth(smr, pixels, array6, 1024);
		mSkin = array6;
		mSkinSize = 1024;
		texture2D.SetPixels32(array6);
		texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: true);
		Material material = new Material(sharedMaterial);
		material.mainTexture = texture2D;
		smr.sharedMaterial = material;
	}

	private bool IsFur(Vector2[] uv, int i)
	{
		if (mSkin == null || uv == null || i >= uv.Length)
		{
			return true;
		}
		int num = Mathf.Clamp((int)(Mathf.Repeat(uv[i].x, 1f) * (float)mSkinSize), 0, mSkinSize - 1);
		int num2 = Mathf.Clamp((int)(Mathf.Repeat(uv[i].y, 1f) * (float)mSkinSize), 0, mSkinSize - 1);
		Color.RGBToHSV(mSkin[num2 * mSkinSize + num], out var H, out var S, out var V);
		if (H > 0.02f && H < 0.17f && S > 0.2f)
		{
			return V > 0.12f;
		}
		return false;
	}

	private void PaintTeeth(SkinnedMeshRenderer smr, Color32[] src, Color32[] dst, int n)
	{
		Mesh sharedMesh = smr.sharedMesh;
		if (!sharedMesh.isReadable)
		{
			return;
		}
		Vector3[] vertices = sharedMesh.vertices;
		Vector2[] uv = sharedMesh.uv;
		int[] triangles = sharedMesh.triangles;
		if (uv.Length != vertices.Length)
		{
			return;
		}
		Matrix4x4 matrix4x = base.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
		Bounds bounds = new Bounds(matrix4x.MultiplyPoint3x4(vertices[0]), Vector3.zero);
		for (int i = 0; i < vertices.Length; i++)
		{
			bounds.Encapsulate(matrix4x.MultiplyPoint3x4(vertices[i]));
		}
		bool[] array = new bool[vertices.Length];
		for (int j = 0; j < vertices.Length; j++)
		{
			Vector3 vector = matrix4x.MultiplyPoint3x4(vertices[j]);
			float num = (vector.y - bounds.min.y) / bounds.size.y;
			array[j] = num > 0.18f && num < 0.45f && Mathf.Abs(vector.x - bounds.center.x) < bounds.size.x * 0.33f && (vector.z - bounds.min.z) / bounds.size.z > 0.45f;
		}
		for (int k = 0; k < triangles.Length; k += 3)
		{
			int num2 = triangles[k];
			int num3 = triangles[k + 1];
			int num4 = triangles[k + 2];
			if (!array[num2] || !array[num3] || !array[num4])
			{
				continue;
			}
			Vector2 vector2 = uv[num2] * n;
			Vector2 vector3 = uv[num3] * n;
			Vector2 vector4 = uv[num4] * n;
			int num5 = Mathf.Max(0, (int)Mathf.Min(vector2.x, Mathf.Min(vector3.x, vector4.x)));
			int num6 = Mathf.Min(n - 1, (int)Mathf.Max(vector2.x, Mathf.Max(vector3.x, vector4.x)) + 1);
			int num7 = Mathf.Max(0, (int)Mathf.Min(vector2.y, Mathf.Min(vector3.y, vector4.y)));
			int num8 = Mathf.Min(n - 1, (int)Mathf.Max(vector2.y, Mathf.Max(vector3.y, vector4.y)) + 1);
			if ((num6 - num5) * (num8 - num7) > 4096)
			{
				continue;
			}
			for (int l = num7; l <= num8; l++)
			{
				for (int m = num5; m <= num6; m++)
				{
					Color32 color = src[l * n + m];
					int num9 = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
					int num10 = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
					if (num9 > 140 && (float)(num9 - num10) < (float)num9 * 0.35f)
					{
						dst[l * n + m] = new Color32(250, 250, 246, byte.MaxValue);
					}
				}
			}
		}
	}

	private bool IsTooth(Vector2[] uv, int i)
	{
		if (mSkin == null || uv == null || i >= uv.Length)
		{
			return false;
		}
		int num = Mathf.Clamp((int)(Mathf.Repeat(uv[i].x, 1f) * (float)mSkinSize), 0, mSkinSize - 1);
		int num2 = Mathf.Clamp((int)(Mathf.Repeat(uv[i].y, 1f) * (float)mSkinSize), 0, mSkinSize - 1);
		Color32 color = mSkin[num2 * mSkinSize + num];
		if (color.r > 200 && color.g > 200)
		{
			return color.b > 200;
		}
		return false;
	}

	private static double Box(double[] s, int W, int x0, int y0, int x1, int y1)
	{
		return s[(y1 + 1) * W + x1 + 1] - s[y0 * W + x1 + 1] - s[(y1 + 1) * W + x0] + s[y0 * W + x0];
	}

	private void AddJaw(SkinnedMeshRenderer smr)
	{
		Mesh mesh = UnityEngine.Object.Instantiate(smr.sharedMesh);
		Vector3[] vertices = mesh.vertices;
		Vector2[] uv = mesh.uv;
		BoneWeight[] boneWeights = mesh.boneWeights;
		Matrix4x4[] bindposes = mesh.bindposes;
		Transform[] bones = smr.bones;
		if (boneWeights.Length != vertices.Length || bones.Length == 0)
		{
			return;
		}
		Matrix4x4 matrix4x = base.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
		Vector3[] array = new Vector3[vertices.Length];
		Bounds bounds = new Bounds(matrix4x.MultiplyPoint3x4(vertices[0]), Vector3.zero);
		for (int i = 0; i < vertices.Length; i++)
		{
			array[i] = matrix4x.MultiplyPoint3x4(vertices[i]);
			bounds.Encapsulate(array[i]);
		}
		mHeight = bounds.size.y;
		float[] array2 = new float[vertices.Length];
		float[] array3 = new float[vertices.Length];
		bool[] array4 = new bool[vertices.Length];
		for (int j = 0; j < vertices.Length; j++)
		{
			array4[j] = IsTooth(uv, j);
		}
		int[] triangles = mesh.triangles;
		for (int k = 0; k < 2; k++)
		{
			bool[] array5 = (bool[])array4.Clone();
			for (int l = 0; l < triangles.Length; l += 3)
			{
				if (array4[triangles[l]] || array4[triangles[l + 1]] || array4[triangles[l + 2]])
				{
					int num = triangles[l];
					bool flag;
					array5[triangles[l + 1]] = (flag = (array5[triangles[l + 2]] = true));
					array5[num] = flag;
				}
			}
			array4 = array5;
		}
		int[] array6 = new int[bones.Length];
		for (int m = 0; m < vertices.Length; m++)
		{
			float num2 = (array[m].y - bounds.min.y) / bounds.size.y;
			float num3 = Mathf.Abs(array[m].x - bounds.center.x) / (bounds.size.x * 0.5f);
			float value = (array[m].z - bounds.min.z) / bounds.size.z;
			float num4 = 0.24f + 0.65f * num3 * num3;
			float num5 = 1f - Mathf.InverseLerp(num4, num4 + 0.06f, num2);
			if (num5 > 0f && num5 < 1f && array4[m])
			{
				num5 = ((num2 < num4 + 0.015f) ? 1f : 0f);
			}
			array2[m] = num5 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.62f, num3))) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.55f, value));
			array3[m] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.655f, 0.68f, num2)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.71f, 0.745f, num2))) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.7f, num3))) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.55f, value)) * (IsFur(uv, m) ? 1f : 0f);
			if (array2[m] > 0.5f && boneWeights[m].boneIndex0 < array6.Length)
			{
				array6[boneWeights[m].boneIndex0]++;
			}
		}
		int num6 = 0;
		for (int n = 1; n < array6.Length; n++)
		{
			if (array6[n] > array6[num6])
			{
				num6 = n;
			}
		}
		Vector3 point = new Vector3(bounds.center.x, bounds.min.y + 0.3f * bounds.size.y, bounds.min.z + 0.4f * bounds.size.z);
		Matrix4x4 matrix4x2 = bones[num6].localToWorldMatrix * bindposes[num6];
		mJaw = new GameObject("PG_Jaw").transform;
		mJaw.SetParent(bones[num6], worldPositionStays: true);
		mJaw.position = matrix4x2.MultiplyPoint3x4(matrix4x.inverse.MultiplyPoint3x4(point));
		mJaw.rotation = base.transform.rotation;
		Matrix4x4 matrix4x3 = mJaw.worldToLocalMatrix * matrix4x2;
		mJawRest = mJaw.localRotation;
		mBrow = new GameObject("PG_Brow").transform;
		mBrow.SetParent(bones[num6], worldPositionStays: true);
		mBrow.position = matrix4x2.MultiplyPoint3x4(matrix4x.inverse.MultiplyPoint3x4(new Vector3(bounds.center.x, bounds.min.y + 0.7f * bounds.size.y, bounds.max.z)));
		mBrow.rotation = base.transform.rotation;
		Matrix4x4 matrix4x4 = mBrow.worldToLocalMatrix * matrix4x2;
		mBrowRest = mBrow.localPosition;
		int num7 = bones.Length;
		int num8 = num7 + 1;
		Transform[] array7 = new Transform[num7 + 2];
		bones.CopyTo(array7, 0);
		array7[num7] = mJaw;
		array7[num8] = mBrow;
		Matrix4x4[] array8 = new Matrix4x4[num7 + 2];
		bindposes.CopyTo(array8, 0);
		array8[num7] = matrix4x3;
		array8[num8] = matrix4x4;
		for (int num9 = 0; num9 < vertices.Length; num9++)
		{
			int num10 = ((array2[num9] >= array3[num9]) ? num7 : num8);
			float num11 = Mathf.Max(array2[num9], array3[num9]);
			if (!(num11 < 0.001f))
			{
				BoneWeight boneWeight = boneWeights[num9];
				float num12 = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2;
				float num13 = ((num12 > 0f) ? ((1f - num11) / num12) : 0f);
				int[] array9 = new int[4] { boneWeight.boneIndex0, boneWeight.boneIndex1, boneWeight.boneIndex2, num10 };
				float[] array10 = new float[4]
				{
					boneWeight.weight0 * num13,
					boneWeight.weight1 * num13,
					boneWeight.weight2 * num13,
					num11
				};
				Array.Sort(array10, array9);
				boneWeights[num9] = new BoneWeight
				{
					boneIndex0 = array9[3],
					weight0 = array10[3],
					boneIndex1 = array9[2],
					weight1 = array10[2],
					boneIndex2 = array9[1],
					weight2 = array10[1],
					boneIndex3 = array9[0],
					weight3 = array10[0]
				};
			}
		}
		mesh.bindposes = array8;
		mesh.boneWeights = boneWeights;
		smr.bones = array7;
		smr.sharedMesh = mesh;
	}

	private void AddOutline(SkinnedMeshRenderer smr)
	{
		Mesh mesh = UnityEngine.Object.Instantiate(smr.sharedMesh);
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;
		if (normals.Length != vertices.Length)
		{
			return;
		}
		float num = 0.006f * mesh.bounds.size.magnitude * 0.6f;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] += normals[i] * num;
		}
		mesh.vertices = vertices;
		Matrix4x4 matrix4x = base.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
		Vector3[] vertices2 = smr.sharedMesh.vertices;
		Bounds bounds = new Bounds(matrix4x.MultiplyPoint3x4(vertices2[0]), Vector3.zero);
		for (int j = 0; j < vertices2.Length; j++)
		{
			bounds.Encapsulate(matrix4x.MultiplyPoint3x4(vertices2[j]));
		}
		bool[] array = new bool[vertices2.Length];
		for (int k = 0; k < vertices2.Length; k++)
		{
			Vector3 vector = matrix4x.MultiplyPoint3x4(vertices2[k]);
			float num2 = (vector.y - bounds.min.y) / bounds.size.y;
			array[k] = Mathf.Abs(vector.x - bounds.center.x) < bounds.size.x * 0.36f && (vector.z - bounds.min.z) / bounds.size.z > 0.5f && num2 > 0.06f && num2 < 0.85f;
		}
		for (int l = 0; l < mesh.subMeshCount; l++)
		{
			int[] triangles = mesh.GetTriangles(l);
			List<int> list = new List<int>(triangles.Length);
			for (int m = 0; m < triangles.Length; m += 3)
			{
				if (!array[triangles[m]] || !array[triangles[m + 1]] || !array[triangles[m + 2]])
				{
					list.Add(triangles[m + 1]);
					list.Add(triangles[m]);
					list.Add(triangles[m + 2]);
				}
			}
			mesh.SetTriangles(list, l);
		}
		GameObject obj = new GameObject("PG_Outline");
		obj.transform.SetParent(smr.transform.parent, worldPositionStays: false);
		obj.transform.localPosition = smr.transform.localPosition;
		obj.transform.localRotation = smr.transform.localRotation;
		obj.transform.localScale = smr.transform.localScale;
		obj.layer = smr.gameObject.layer;
		SkinnedMeshRenderer skinnedMeshRenderer = obj.AddComponent<SkinnedMeshRenderer>();
		skinnedMeshRenderer.bones = smr.bones;
		skinnedMeshRenderer.rootBone = smr.rootBone;
		skinnedMeshRenderer.sharedMesh = mesh;
		skinnedMeshRenderer.localBounds = smr.localBounds;
		Material material = new Material(smr.sharedMaterial);
		Texture2D texture2D = new Texture2D(1, 1);
		texture2D.SetPixel(0, 0, new Color(0.08f, 0.05f, 0.04f));
		texture2D.Apply();
		material.mainTexture = texture2D;
		Material[] array2 = new Material[mesh.subMeshCount];
		for (int n = 0; n < array2.Length; n++)
		{
			array2[n] = material;
		}
		skinnedMeshRenderer.sharedMaterials = array2;
	}
}
