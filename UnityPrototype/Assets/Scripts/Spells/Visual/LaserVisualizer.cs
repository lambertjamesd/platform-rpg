using UnityEngine;
using System.Collections;

public class LaserVisualizer : MonoBehaviour, ISelfDestruct {
	private const float MAX_DISTANCE = 50.0f;
	
	public Transform baseTransform;
	public Transform targetTransform;
	public LaserDeathVisualizer death;
	private float length = 0.0f;
	private float radius;

	public void SetRadius(float radius)
	{
		baseTransform.localScale = Vector3.one * radius;
		this.radius = radius;
	}

	public void SetInnerLength(float innerLength)
	{
		targetTransform.position = transform.position + transform.TransformDirection(Vector3.up) * Mathf.Min(innerLength, MAX_DISTANCE);
		length = innerLength;
	}

	public void DestroySelf()
	{
		if (death != null)
		{
			LaserDeathVisualizer deathInstance = (LaserDeathVisualizer)Instantiate(death, transform.position, transform.rotation);
			deathInstance.transform.parent = transform.parent;
			deathInstance.SetInnerLength(length);
			deathInstance.SetRadius(radius);
		}

		TimeManager.DestroyGameObject(gameObject);
	}
}
