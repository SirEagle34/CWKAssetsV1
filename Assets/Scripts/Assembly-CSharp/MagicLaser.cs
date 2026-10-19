using UnityEngine;
using System.Collections;

public class MagicLaser : MonoBehaviour
{
    public ParticleSystem castSpark;
    public ParticleSystem beam;
    public ParticleSystem headSpark;

    public float castDelay = 0.1f;

    public IEnumerator FireSequence(Vector3 start, Vector3 end)
    {
        // Phase 1: Cast spark
        castSpark.transform.position = start;
        castSpark.Play();

        yield return new WaitForSeconds(castDelay);

        // Phase 2: Beam
        Vector3 dir = end - start;
        float dist = dir.magnitude;

        transform.position = start;
        transform.rotation = Quaternion.LookRotation(dir);
        beam.transform.localScale = new Vector3(1, 1, dist);

        beam.Play();

        // Head spark at beam tip
        headSpark.transform.position = end;
        headSpark.Play();
    }
}
