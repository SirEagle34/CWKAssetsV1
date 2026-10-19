using System;
using UnityEngine;

[AddComponentMenu("NGUI/Tween/Tween Alpha")]
public class TweenAlpha : UITweener
{
	[Range(0f, 1f)]
	public float from = 1f;

	[Range(0f, 1f)]
	public float to = 1f;

	private float mRestoreValue;

	private bool mRestoreValueSet;

	private UIRect mRect;

	public UIRect cachedRect
	{
		get
		{
			if (mRect == null)
			{
				GameObject go = null;

				if (target != null)
				{
					go = target.gameObject;
				}
				else
				{
					go = base.gameObject;
				}

				if (go != null)
				{
					mRect = go.GetComponent<UIRect>();

					if (mRect == null)
					{
						mRect = go.GetComponentInChildren<UIRect>(true);
					}
				}
			}

			return mRect;
		}
	}

	[Obsolete("Use 'value' instead")]
	public float alpha
	{
		get
		{
			return value;
		}
		set
		{
			this.value = value;
		}
	}

	public float value
	{
		get
		{
			UIRect rect = cachedRect;

			if (rect == null)
			{
				return 0f;
			}

			return rect.alpha;
		}
		set
		{
			UIRect rect = cachedRect;

			if (rect == null)
			{
				enabled = false;
				return;
			}

			rect.alpha = value;
		}
	}

	protected override void OnUpdate(float factor, bool isFinished)
	{
		UIRect rect = cachedRect;

		if (rect == null)
		{
			enabled = false;
			return;
		}

		if (!mRestoreValueSet)
		{
			mRestoreValue = rect.alpha;
			mRestoreValueSet = true;
		}

		rect.alpha = Mathf.Lerp(from, to, factor);
	}

	public static TweenAlpha Begin(GameObject go, float duration, float alpha)
	{
		if (go == null)
		{
			return null;
		}

		TweenAlpha tweenAlpha = UITweener.Begin<TweenAlpha>(go, duration);

		if (tweenAlpha == null)
		{
			return null;
		}

		UIRect rect = tweenAlpha.cachedRect;

		if (rect == null)
		{
			tweenAlpha.enabled = false;
			return tweenAlpha;
		}

		tweenAlpha.from = rect.alpha;
		tweenAlpha.to = alpha;

		if (duration <= 0f)
		{
			tweenAlpha.Sample(1f, true);
			tweenAlpha.enabled = false;
		}

		return tweenAlpha;
	}

	public override void SetStartToCurrentValue()
	{
		UIRect rect = cachedRect;

		if (rect == null)
		{
			enabled = false;
			return;
		}

		from = rect.alpha;
	}

	public override void SetEndToCurrentValue()
	{
		UIRect rect = cachedRect;

		if (rect == null)
		{
			enabled = false;
			return;
		}

		to = rect.alpha;
	}

	public override void RestorePrevious()
	{
		UIRect rect = cachedRect;

		if (rect == null)
		{
			enabled = false;
			return;
		}

		if (!mRestoreValueSet)
		{
			mRestoreValue = rect.alpha;
			mRestoreValueSet = true;
		}

		rect.alpha = mRestoreValue;
	}
}