using UnityEngine;
using System.Collections;

public static class GUIHelper {
	public static Bounds ObjectBoundingBox(GameObject gameObject)
	{
		Collider collider = gameObject.GetComponent<Collider>();

		if (collider != null)
		{
			return collider.bounds;
		}

		MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();

		if (renderer != null)
		{
			return renderer.bounds;
		}

		AreaEffect areaEffect = gameObject.GetComponent<AreaEffect>();

		if (areaEffect != null)
		{
			return areaEffect.bounds;
		}

		return new Bounds(gameObject.transform.position, Vector3.zero);
	}
}