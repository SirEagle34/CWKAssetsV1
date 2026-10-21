using UnityEngine;

/// <summary>
/// Independent projectile reference for clone visuals.
/// The caller supplies the prefab so normal ShapeShift shots remain untouched.
/// </summary>
public sealed class CloneShot : MonoBehaviour
{
    [SerializeField] private GameObject shotPrefab;

    public GameObject ShotPrefab => shotPrefab;

    public void SetShotPrefab(GameObject prefab)
    {
        shotPrefab = prefab;
    }

    public GameObject Spawn(Transform origin, Transform target)
    {
        if (shotPrefab == null || origin == null)
        {
            return null;
        }

        GameObject shot = Instantiate(shotPrefab, origin.position, origin.rotation);
        if (target != null)
        {
            shot.transform.LookAt(target.position);
        }
        return shot;
    }
}
