using System.Collections.Generic;
using UnityEngine;

public static class TeamRow
{
	public static Transform[] Fit(Transform[] nodes, int count)
	{
		if (nodes == null || nodes.Length < 2 || nodes[0] == null || nodes[0].parent == null)
		{
			return nodes;
		}
		bool flag = nodes[0].parent != nodes[1].parent;
		List<Transform> list = new List<Transform>();
		foreach (Transform transform in nodes)
		{
			if (transform != null)
			{
				list.Add(flag ? transform.parent : transform);
			}
		}
		List<Transform> list2 = new List<Transform>();
		List<Transform> list3 = new List<Transform>();
		foreach (Transform item in list)
		{
			TeamRowSlot teamRowSlot = item.GetComponent<TeamRowSlot>();
			if (teamRowSlot == null)
			{
				teamRowSlot = item.gameObject.AddComponent<TeamRowSlot>();
				teamRowSlot.Home = item.localPosition;
				teamRowSlot.HomeScale = item.localScale;
			}
			(teamRowSlot.Extra ? list3 : list2).Add(item);
		}
		int count2 = list2.Count;
		if (count2 < 2)
		{
			return nodes;
		}
		Transform transform2 = list2[count2 - 1];
		if (flag)
		{
			_ = nodes[0].name;
		}
		while (count2 + list3.Count < count)
		{
			Transform transform3 = Object.Instantiate(transform2.gameObject, transform2.parent, worldPositionStays: false).transform;
			transform3.name = transform2.name + "_extra" + list3.Count;
			transform3.GetComponent<TeamRowSlot>().Extra = true;
			Transform transform4 = (flag ? transform3.Find(nodes[count2 - 1].name) : transform3);
			if (transform4 != null)
			{
				foreach (Transform item2 in transform4)
				{
					Object.Destroy(item2.gameObject);
				}
			}
			list3.Add(transform3);
		}
		List<Transform> list4 = new List<Transform>(list2);
		list4.AddRange(list3);
		TeamRowSlot component = list2[0].GetComponent<TeamRowSlot>();
		TeamRowSlot component2 = list2[count2 - 1].GetComponent<TeamRowSlot>();
		Vector3 home = component.Home;
		Vector3 home2 = component2.Home;
		bool flag2 = count > count2;
		float num = (flag2 ? ((float)count2 / (float)count) : 1f);
		float num2 = (home2.x - home.x) / (float)(count2 - 1);
		float a = home.x - num2 * 0.5f * (1f - num);
		float b = home2.x + num2 * 0.5f * (1f - num);
		List<Transform> list5 = new List<Transform>();
		for (int j = 0; j < list4.Count; j++)
		{
			Transform transform5 = list4[j];
			TeamRowSlot component3 = transform5.GetComponent<TeamRowSlot>();
			if (flag2)
			{
				float t = (float)j / (float)(count - 1);
				transform5.localPosition = new Vector3(Mathf.Lerp(a, b, t), home.y, home.z);
				transform5.localScale = new Vector3(component3.HomeScale.x * num, component3.HomeScale.y * num, component3.HomeScale.z);
			}
			else if (!component3.Extra)
			{
				transform5.localPosition = component3.Home;
				transform5.localScale = component3.HomeScale;
			}
			transform5.gameObject.SetActive(!component3.Extra || (flag2 && j < count));
			Transform transform6 = (flag ? transform5.Find((j < nodes.Length && nodes[j] != null) ? nodes[j].name : nodes[count2 - 1].name) : transform5);
			list5.Add((transform6 != null) ? transform6 : transform5);
		}
		return list5.ToArray();
	}
}
