using System.Collections.Generic;
using UnityEngine;

public static class ChestSkins
{
	public static string Current;

	private static readonly Dictionary<string, Mesh> sFitted = new Dictionary<string, Mesh>();

	private static readonly Dictionary<string, Material> sMats = new Dictionary<string, Material>();

	public static void Apply(GameObject chest)
	{
		if (string.IsNullOrEmpty(Current) || chest == null)
		{
			return;
		}
		Mesh mesh = Resources.Load<Mesh>("ChestSkins/" + Current);
		if (mesh == null)
		{
			return;
		}
		Texture2D texture2D = Resources.Load<Texture2D>("ChestSkins/" + Current + "_tex");
		SkinnedMeshRenderer skinnedMeshRenderer = null;
		SkinnedMeshRenderer[] componentsInChildren = chest.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		foreach (SkinnedMeshRenderer skinnedMeshRenderer2 in componentsInChildren)
		{
			Mesh sharedMesh = skinnedMeshRenderer2.sharedMesh;
			if (sharedMesh != null && sharedMesh.bindposes.Length == 2 && skinnedMeshRenderer2.bones.Length == 2 && !sharedMesh.name.Contains("Outline") && (skinnedMeshRenderer == null || sharedMesh.bounds.size.sqrMagnitude > skinnedMeshRenderer.sharedMesh.bounds.size.sqrMagnitude))
			{
				skinnedMeshRenderer = skinnedMeshRenderer2;
			}
		}
		if (skinnedMeshRenderer == null)
		{
			return;
		}
		Mesh sharedMesh2 = skinnedMeshRenderer.sharedMesh;
		Renderer[] componentsInChildren2 = chest.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren2)
		{
			Mesh mesh2 = ((renderer is SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)renderer).sharedMesh : (renderer.GetComponent<MeshFilter>() ? renderer.GetComponent<MeshFilter>().sharedMesh : null));
			if (renderer != skinnedMeshRenderer && mesh2 != null && (mesh2.name.StartsWith("LootChest") || mesh2.name.StartsWith("LootEgg")))
			{
				renderer.enabled = false;
			}
		}
		string key = Current + "|" + sharedMesh2.name;
		if (!sFitted.TryGetValue(key, out var value))
		{
			Bounds bounds = mesh.bounds;
			Bounds bounds2 = sharedMesh2.bounds;
			float num = Mathf.Min(bounds2.size.x / bounds.size.x, Mathf.Min(bounds2.size.y / bounds.size.y, bounds2.size.z / bounds.size.z));
			Vector3 vector = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
			Matrix4x4 matrix4x = Matrix4x4.TRS(new Vector3(bounds2.center.x, bounds2.center.y, bounds2.min.z) - vector * num, Quaternion.identity, Vector3.one * num);
			value = Object.Instantiate(mesh);
			Matrix4x4[] bindposes = sharedMesh2.bindposes;
			value.bindposes = new Matrix4x4[2]
			{
				bindposes[0] * matrix4x,
				bindposes[1] * matrix4x
			};
			sFitted[key] = value;
		}
		if (!sMats.TryGetValue(key, out var value2))
		{
			value2 = new Material(skinnedMeshRenderer.sharedMaterial);
			if (texture2D != null)
			{
				value2.mainTexture = texture2D;
			}
			sMats[key] = value2;
		}
		skinnedMeshRenderer.sharedMesh = value;
		skinnedMeshRenderer.sharedMaterials = new Material[1] { value2 };
		skinnedMeshRenderer.updateWhenOffscreen = true;
	}
}
