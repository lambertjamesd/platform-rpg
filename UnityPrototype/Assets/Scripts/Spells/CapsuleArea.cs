using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CapsuleArea : AreaEffect, IFixedUpdate {
	
	private UpdateManager updateManager;

	private Vector3 center;
	private Vector3 up;
	private float halfOffset;
	private float radius;
	private float innerHeight;
	private int collideWith;
	private bool lockRotation;
	private Vector3 lastPosition;

	private bool addedToUpdate = false;

	private void AddToUpdate()
	{
		if (!addedToUpdate && updateManager != null)
		{
			this.AddToUpdateManager(updateManager);
			addedToUpdate = true;
		}
	}

	private void RemoveFromUpdate()
	{
		if (addedToUpdate && updateManager != null)
		{
			this.RemoveFromUpdateManager(updateManager);
			addedToUpdate = false;
		}
	}

	public void CheckColliderSource(GameObject colliderSource, ref float height)
	{
		CharacterController character = colliderSource.GetComponent<CharacterController>();
		
		if (character != null)
		{
			height = character.height;
			center = character.center;
			radius = character.radius;
			up = Vector3.up;
			lockRotation = true;
			return;
		}

		CapsuleCollider capsuleCollider = colliderSource.GetComponent<CapsuleCollider>();

		if (capsuleCollider != null)
		{
			height = capsuleCollider.height;
			center = capsuleCollider.center;
			radius = capsuleCollider.radius;

			switch (capsuleCollider.direction)
			{
			case 0: up = Vector3.right; break;
			case 1: up = Vector3.up; break;
			case 2: up = Vector3.forward; break;
			default: up = Vector3.up; break;
			}

			lockRotation = false;
		}

		SphereCollider sphereCollider = colliderSource.GetComponent<SphereCollider>();

		if (sphereCollider != null)
		{
			height = 0.0f;
			center = sphereCollider.center;
			radius = sphereCollider.radius;
			up = Vector3.up;
			lockRotation = false;
		}
	}
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		center = instance.GetValue<Vector3>("center", Vector3.zero);
		up = instance.GetValue<Vector3>("capsuleUp", Vector3.up);
		float height = instance.GetValue<float>("height", 2.0f);
		radius = instance.GetValue<float>("radius", 0.5f);
		collideWith = instance.GetValue<int>("collideWith", ~0);
		lockRotation = instance.GetValue<bool>("lockRotation", false);
		
		GameObject colliderSource = instance.GetValue<GameObject>("colliderSource", null);

		if (colliderSource != null)
		{
			CheckColliderSource(colliderSource, ref height);
		}

		halfOffset = Mathf.Max(0.0f, height * 0.5f - radius);

		innerHeight = Mathf.Max(0.0f,height - radius * 2.0f);

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		AddToUpdate();

		lastPosition = transform.TransformPoint(center);
	}

	public override Bounds bounds
	{
		get
		{
			return new Bounds(transform.TransformPoint(center), new Vector3(radius, (halfOffset + radius) * 2.0f, radius));
		}
	}
	
	public void OnEnable() {
		AddToUpdate();
	}
	
	new public void OnDisable() {
		RemoveFromUpdate();
		
		base.OnDisable();
	}

	public void FixedUpdateTick (float dt) {
		Vector3 worldCenter = transform.TransformPoint(center);

		Vector2 moveAmount = worldCenter - lastPosition;

		HashSet<ICollisionShape> overlappingColliders = new HashSet<ICollisionShape>();

		CapsuleShape capsuleShape = new CapsuleShape(radius, innerHeight);
		capsuleShape.Center = worldCenter;
		capsuleShape.CollisionLayers = collideWith;

		overlappingColliders.UnionWith(index.OverlapShape(capsuleShape));

		if (moveAmount != Vector2.zero)
		{
			Vector2 rayDir = moveAmount.normalized;

			overlappingColliders.UnionWith(index.CapsulecastMulti(
				new Ray2D(worldCenter, rayDir),
				radius, innerHeight,
				Vector2.Dot(rayDir, moveAmount),
				-1,
				collideWith
			).Select(x => x.Shape));
		}
		
		UpdateContainedShapes(overlappingColliders.ToArray(), dt);

		lastPosition = worldCenter;
	}
	
	public void OnDrawGizmos()
	{
		Vector3 worldCenter = transform.TransformPoint(center);
		Vector3 worldUp = lockRotation ? up : transform.TransformDirection(up);
		Vector3 moveAmount = worldCenter - lastPosition;
		
		Vector3 a = worldCenter + worldUp * halfOffset;
		Vector3 b = worldCenter - worldUp * halfOffset;

		GizmoHelper.DrawThickLine(a, a - moveAmount, radius, Color.red);
		GizmoHelper.DrawThickLine(b, b - moveAmount, radius, Color.red);
		GizmoHelper.DrawThickLine(a - moveAmount, b - moveAmount, radius, Color.red);
		GizmoHelper.DrawThickLine(a, b, radius, Color.green);
	}
	
	public override object GetCurrentState()
	{
		object result = base.GetCurrentState();

		if (result == null)
		{
			return null;
		}
		else
		{
			return new object[]{
				result,
				lastPosition
			};
		}
	}
	
	public override void RewindToState(object state)
	{
		if (state == null)
		{
			base.RewindToState(null);
		}
		else
		{
			object[] stateArray = (object[])state;
			base.RewindToState(stateArray[0]);
			lastPosition = (Vector3)stateArray[1];
		}
	}
}
