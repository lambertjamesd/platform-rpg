using UnityEngine;

public class BoxArea : AreaEffect, IFixedUpdate
{
	private Vector3 halfSize;
	private BoundingBoxShape shape;
	private UpdateManager updateManager;
	
	private bool isUpdateAdded = false;
	
	private void EnsureAddedToUpdate()
	{
		if (!isUpdateAdded && updateManager != null)
		{
			this.AddToUpdateManager(updateManager);
			isUpdateAdded = true;
		}
	}
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		halfSize = instance.GetValue<Vector3>("size", Vector3.one) * 0.5f;
		shape = new BoundingBoxShape(new BoundingBox(-halfSize, halfSize));
		shape.CollisionLayers = instance.GetValue<int>("collideWith", ~0);

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		
		if (gameObject.activeSelf)
		{
			EnsureAddedToUpdate();
		}
	}
	
	public override Bounds bounds
	{
		get
		{
			return new Bounds(transform.position, halfSize * 0.5f);
		}
	}
	
	public void OnEnable()
	{
		EnsureAddedToUpdate();
	}
	
	new public void OnDisable()
	{
		this.RemoveFromUpdateManager(updateManager);
		isUpdateAdded = false;
		
		base.OnDisable();
	}
	
	public void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position, halfSize * 2.0f);
	}
	
	public void FixedUpdateTick(float dt) {
		shape.SetBoundingBox(new BoundingBox(transform.position - halfSize, transform.position + halfSize));
		UpdateContainedShapes(index.OverlapShape(shape), dt);
	}
}
