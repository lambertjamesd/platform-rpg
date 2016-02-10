using UnityEngine;
using System.Collections;

public class CustomCharacterController : CustomCapsule {
	static readonly float SKIN_THICKNESS = 0.1f;
	static readonly float NORMAL_OFFSET = 0.0001f;
	static readonly float MIN_MOVE = 0.001f;
	static readonly int MAX_DEPTH = 5;

	public float slopeLimit = 60.0f;
	
	public int moveCollisionGroup = -1;
	public int moveCollisionLayers = ~0;

	private void MoveInternal(Vector2 amount, int depth = 0)
	{
		if (amount.sqrMagnitude >= MIN_MOVE * MIN_MOVE && depth < MAX_DEPTH)
		{
			Vector2 direction = amount.normalized;
			float distance = Vector2.Dot(amount, direction);
			Vector2 currentPos = shape.Center;
			
			Ray2D castRay = new Ray2D(currentPos - direction * SKIN_THICKNESS, direction);
			
			ShapeRaycastHit hit = index.Capsulecast(
				castRay,
				radius,
				innerHeight,
				distance + SKIN_THICKNESS,
				moveCollisionGroup,
				moveCollisionLayers
				);
			
			if (hit != null)
			{
				float moveDistance = hit.Distance - SKIN_THICKNESS;
				Vector2 subOffset = direction * moveDistance;
				transform.position += new Vector3(
					subOffset.x + Mathf.Sign(hit.Normal.x) * NORMAL_OFFSET, 
					subOffset.y + Mathf.Sign(hit.Normal.y) * NORMAL_OFFSET, 
					0.0f
					);
				HandleHit(hit);
				
				float remainingDistance = distance - moveDistance;
				
				if (remainingDistance >= MIN_MOVE)
				{
					Vector2 newDirection = Vector2Helper.ProjectOnto(direction, Vector2Helper.Rotate90(hit.Normal));
					MoveInternal(newDirection * remainingDistance, depth + 1);
				}
			}
			else
			{
				transform.position += new Vector3(amount.x, amount.y, 0.0f);
			}
		}
	}

	private void HandleHit(ShapeRaycastHit hit)
	{
		gameObject.SendMessage("OnCustomControllerHit", hit);
	}

	public void Move(Vector2 amount)
	{
		MoveInternal(amount);
		moveSignal.Moved();
	}
}
