using UnityEngine;
using System.Collections;

public class LaserDeathVisualizer : LaserVisualizer {

	public Renderer target;
	public float duration = 1.0f;
	public Vector2 finalUVScale = new Vector2(1.0f, 1.0f);
	public Vector2 finalUVOffset = Vector2.zero;
	public AnimationCurve curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

	private Vector2 startUVScale;
	private Vector2 startUVOffset;
	private float lifetime;

	public void Start()
	{
		startUVScale = target.material.mainTextureScale;
		startUVOffset = target.material.mainTextureOffset;
	}

	public void Update()
	{
		float t = curve.Evaluate(lifetime / duration);

		target.material.mainTextureScale = Vector2.Lerp(startUVScale, finalUVScale, t);
		target.material.mainTextureOffset = Vector2.Lerp(startUVOffset, finalUVOffset, t);

		lifetime += Time.deltaTime;

		if (lifetime > duration)
		{
			SelfDestruct.DestroySelf(gameObject);
		}
	}
}
