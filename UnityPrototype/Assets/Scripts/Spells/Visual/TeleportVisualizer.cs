using UnityEngine;
using System.Collections;

public class TeleportVisualizer : EffectGameObject {
	private LineRenderer lineRenderer;
	private float duration;
	private float currentTime;
	private CubicBezierCurve curve;
	private int resolution = 20;
	
	public override void StartEffect(EffectInstance instance)
	{
		base.StartEffect(instance);

		lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();
		lineRenderer.SetVertexCount(resolution + 1);
		duration = instance.GetValue<float>("duration", 1.0f);

		Vector3 start = instance.GetValue<Vector3>("start", Vector3.zero);
		Vector3 end = instance.GetValue<Vector3>("end", Vector3.zero);
		Vector3 handle = instance.GetValue<Vector3>("bendHandle", start + Random.onUnitSphere);

		curve = new CubicBezierCurve(new Vector3[]{start, handle, end, end});

		Recurve(0.0f);

		currentTime = 0.0f;
	}

	public void Update()
	{
		if (currentTime < duration)
		{
			Recurve(currentTime / duration);
			currentTime += Time.deltaTime;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Recurve(float startLerp)
	{
		float tWidth = 1.0f - startLerp;
		float dT = tWidth / resolution;

		float t = startLerp;

		for (int i = 0; i <= resolution; ++i)
		{
			lineRenderer.SetPosition(i, curve.Eval(t));
			t += dT;
		}
	}
}
