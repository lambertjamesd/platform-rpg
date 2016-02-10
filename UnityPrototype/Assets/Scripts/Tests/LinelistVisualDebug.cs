using UnityEngine;

public class LinelistVisualDebug : ShapeVisualDebug
{
	public Vector2[] relativePoints = null;
	public bool closed = false;
	public bool noendpoint = false;
	
	public override ICollisionShape GetShape ()
	{
		if (relativePoints != null)
		{
			Vector2 offset = new Vector2(transform.position.x, transform.position.y);

			Vector2[] worldPoint = new Vector2[relativePoints.Length];

			for (int i = 0; i < relativePoints.Length; ++i)
			{
				worldPoint[i] = relativePoints[i] + offset;
			}
			
			return new LineListShape(worldPoint, closed, noendpoint);
		}
		else
		{
			return null;
		}
	}
	
	
	public void OnDrawGizmos()
	{
		if (relativePoints != null)
		{
			Vector3 pos = transform.position;

			for (int i = 1; i < relativePoints.Length; ++i)
			{
				Vector3 prevPoint = pos + new Vector3(relativePoints[i - 1].x, relativePoints[i - 1].y);
				Vector3 point = pos + new Vector3(relativePoints[i].x, relativePoints[i].y);

				Gizmos.DrawLine(prevPoint, point);
			}

			if (closed)
			{
				Vector3 prevPoint = pos + new Vector3(relativePoints[0].x, relativePoints[0].y);
				Vector3 point = pos + new Vector3(relativePoints[relativePoints.Length - 1].x, relativePoints[relativePoints.Length - 1].y);
				
				Gizmos.DrawLine(prevPoint, point);
			}
		}
	}
}