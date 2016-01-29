using UnityEngine;
using System.Collections;

public class BoundingBoxVisualDebug : ShapeVisualDebug {
	public Vector2 size = Vector2.one;

	public override ICollisionShape GetShape ()
	{
		Vector2 pos = new Vector2(transform.position.x, transform.position.y);

		return new BoundingBoxShape(new BoundingBox(pos - size * 0.5f, pos + size * 0.5f));
	}

	
	public void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0.0f));
	}
}
