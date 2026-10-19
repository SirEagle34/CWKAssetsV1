using System.Collections;
using UnityEngine;

public class BounceOnRevive : MonoBehaviour
{
    private Vector3 mBaseScale = Vector3.zero;

    public void PlayRevive()
    {
        StartCoroutine(ReviveBounce());
    }

    private IEnumerator ReviveBounce()
    {
        BattleCreatureAnimState animState = GetComponentInChildren<BattleCreatureAnimState>();
        Transform scaleObj = (animState != null) ? animState.transform : transform;

        if (mBaseScale == Vector3.zero)
        {
            mBaseScale = scaleObj.localScale;
        }

        // (İstersen revive sound ekleyebilirsin)
        // Singleton<SLOTAudioManager>.Instance.PlaySound("creature/revive");

        Transform tweenObject = Singleton<PrefabReferences>.Instance.BounceTween.transform;
        bool done = false;

        Singleton<PrefabReferences>.Instance.BounceTween.PlayWithCallback(() =>
        {
            done = true;
        });

        while (!done)
        {
            scaleObj.localScale = new Vector3(
                mBaseScale.x * tweenObject.localScale.x,
                mBaseScale.y * tweenObject.localScale.y,
                mBaseScale.z * tweenObject.localScale.z
            );
            yield return null;
        }

        scaleObj.localScale = mBaseScale;

        // 🔥 REVIVE ANİMASYONU
        if (animState != null)
        {
            animState.anim.SetBool("Revive", true);
            animState.ResetFigdetTimer();
        }
    }
}
