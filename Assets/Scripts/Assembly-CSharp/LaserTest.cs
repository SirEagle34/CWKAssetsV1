using UnityEngine;

public class LaserTest : MonoBehaviour
{
	public MagicLaser laser;

	public Transform target;

	[ContextMenu("TEST → Fire Laser")]
	public void FireLaser()
	{
		if (laser == null || target == null)
		{
			Debug.LogWarning("Laser or Target not assigned");
		}
	}
}
