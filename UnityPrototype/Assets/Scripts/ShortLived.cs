using UnityEngine;
using System.Collections;

public class ShortLived : MonoBehaviour, IFixedUpdate
{
	public bool useFixedUpdate;
	public float lifetime = 0;
	
	private UpdateManager updateManager;
	
	public void Start()
	{
		if (useFixedUpdate)
		{
			updateManager = gameObject.GetComponentWithAncestors<UpdateManager>();
			updateManager.AddReciever(this);
		}
	}
	
	private void Tick(float deltaTime)
	{
		if (lifetime >= 0.0)
		{
			lifetime -= deltaTime;
			
			if (lifetime < 0.0)
			{
				if (updateManager != null)
				{
					updateManager.RemoveReciever(this);
				}
				
				Destroy(gameObject);
			}
		}
	}
	
	public void Update()
	{
		Tick(Time.deltaTime);
	}
	
	public void FixedUpdateTick(float deltaTime)
	{
		Tick(deltaTime);
	}
}