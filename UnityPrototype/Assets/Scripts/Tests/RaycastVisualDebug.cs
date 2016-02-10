using UnityEngine;
using System.Collections;

public class RaycastVisualDebug : MonoBehaviour {
	public Transform targetPosition;
	public bool infinite = true;
	public int castType = 0;
	public float radius = 0.5f;
	public float innerHeight = 1.0f;

	public ShapeVisualDebug[] shapesToTrace;
	public CustomCollider[] customShapes;

	private SimpleRaycastHit Cast(Ray2D ray, ICollisionShape shape)
	{
		switch (castType)
		{
		case 0:
			return shape.Raycast(ray);
		case 1:
			return shape.Spherecast(ray, radius);
		default:
			return shape.CapsuleCast(ray, radius, innerHeight);
		}
	}

	private void DrawShape(Vector2 position)
	{
		switch (castType)
		{
		case 0:
			Gizmos.DrawWireSphere(new Vector3(position.x, position.y, 0.0f), 0.1f);
			break;
		case 1:
			Gizmos.DrawSphere(new Vector3(position.x, position.y, 0.0f), radius);
			break;
		default:
			Gizmos.DrawSphere(new Vector3(position.x, position.y + innerHeight * 0.5f, 0.0f), radius);
			Gizmos.DrawSphere(new Vector3(position.x, position.y - innerHeight * 0.5f, 0.0f), radius);
			Gizmos.DrawCube(new Vector3(position.x, position.y, 0.0f), new Vector3(radius * 2.0f, innerHeight, 1.0f));
			break;
		}
	}

	public void OnDrawGizmos()
	{
		if (targetPosition != null)
		{
			Vector2 position = new Vector2(transform.position.x, transform.position.y);
			Vector2 target = new Vector2(targetPosition.position.x, targetPosition.position.y);

			Ray2D ray = new Ray2D(position, (target - position).normalized);

			SimpleRaycastHit nearestHit = null;
			
			if (shapesToTrace != null)
			{
				foreach (ShapeVisualDebug shape in shapesToTrace)
				{
					SimpleRaycastHit hit = Cast(ray, shape.GetShape());

					if (nearestHit == null || (hit != null && hit.Distance < nearestHit.Distance))
					{
						nearestHit = hit;
					}
				}
			}

			if (customShapes != null)
			{
				foreach (CustomCollider shape in customShapes)
				{
					shape.InitializeShape();
					shape.UpdateIndex();
					SimpleRaycastHit hit = Cast(ray, shape.GetShape());
					
					if (nearestHit == null || (hit != null && hit.Distance < nearestHit.Distance))
					{
						nearestHit = hit;
					}
				}
			}

			if (nearestHit != null)
			{
				Gizmos.color = Color.green;
				DrawShape(position);
				DrawShape(ray.GetPoint(nearestHit.Distance));
				Gizmos.DrawRay(new Vector3(nearestHit.Position.x, nearestHit.Position.y), new Vector3(nearestHit.Normal.x, nearestHit.Normal.y));
			}
			else
			{
				Gizmos.color = Color.red;
				DrawShape(position);
				Gizmos.DrawRay(transform.position, targetPosition.position - transform.position);
			}
		}
	}
}
