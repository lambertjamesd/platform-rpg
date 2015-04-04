using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

	public float maxAcceleration = 100.0f;
	public int stepSubdivisions = 10;
	public float maxOffsetError = 10.0f;
	
	private Transform followTarget;
	private Vector3 lastTargetPosition;
	private Vector3 velocity;
	public float cameraDistance = 10;

	// Use this for initialization
	void Start () {

	}

	public Transform FollowTarget
	{
		set
		{
			if (followTarget != value)
			{
				followTarget = value;

				if (followTarget != null)
				{
					lastTargetPosition = followTarget.transform.position;
				}
			}
		}

		get
		{
			return followTarget;
		}
	}

	private void UpdateAxis(float targetPosition, float targetVelocity, float deltaTime, ref float cameraPosition, ref float cameraVelocity)
	{
		float deltaPosition = cameraPosition - targetPosition;
		float deltaVelocity = cameraVelocity - targetVelocity;

		float timeTillStopped = Mathf.Abs(deltaVelocity) / maxAcceleration;
		float acceleration = 0.0f;
		float timeTillArrived = 0.0f;

		if (deltaPosition == 0.0f)
		{
			if (deltaVelocity == 0.0f)
			{
				// already on target
				return;
			}

			acceleration = -Mathf.Sign(deltaVelocity) * maxAcceleration;
			timeTillArrived = -2.0f * deltaVelocity / acceleration;
		}
		else
		{
			acceleration = -Mathf.Sign(deltaPosition) * maxAcceleration;
			timeTillArrived = (-deltaVelocity + Mathf.Sign(acceleration) * Mathf.Sqrt(deltaVelocity * deltaVelocity - 2 * acceleration * deltaPosition)) / acceleration;
		}

		if (Mathf.Abs(timeTillArrived) <= deltaTime && Mathf.Abs(timeTillStopped) <= deltaTime)
		{
			cameraPosition = targetPosition;
			cameraVelocity = targetVelocity;
		}
		else
		{
			if (timeTillArrived < timeTillStopped)
			{
				acceleration = -acceleration;
			}

			cameraPosition += cameraVelocity * deltaTime + 0.5f * acceleration * deltaTime * deltaTime;
			cameraVelocity += acceleration * deltaTime;
		}
	}

	void FixedUpdate () {

		Vector3 currentTargetPosition;
		Vector3 targetVelocity;
		
		Vector3 position = transform.position;
		
		if (followTarget == null)
		{
			currentTargetPosition = transform.position;
			targetVelocity = Vector3.zero;
		}
		else
		{
			currentTargetPosition = followTarget.transform.position;
			targetVelocity = (currentTargetPosition - lastTargetPosition) * (1.0f / Time.deltaTime);
			
			Vector3 expectedPosition = lastTargetPosition + targetVelocity * Time.deltaTime;
			Vector3 offset = currentTargetPosition - expectedPosition;

			// see if the target has teleported a large distance
			if (offset.magnitude > maxOffsetError)
			{
				position += offset;
			}

		}
		
		float timeStep = Time.deltaTime / stepSubdivisions;

		for (int i = 0; i < stepSubdivisions; ++i)
		{
			UpdateAxis(currentTargetPosition.x, targetVelocity.x, timeStep, ref position.x, ref velocity.x);
			UpdateAxis(currentTargetPosition.y, targetVelocity.y, timeStep, ref position.y, ref velocity.y);
		}
		
		transform.position = position;
		
		lastTargetPosition = currentTargetPosition;
	}
}
