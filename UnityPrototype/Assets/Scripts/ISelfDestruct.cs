using UnityEngine;
using System.Collections;

public interface ISelfDestruct
{
	void DestroySelf();
}

public class SelfDestruct
{
	public static bool DestroySelf(GameObject target)
	{
		ISelfDestruct selfDestruct = target.GetInterfaceComponent<ISelfDestruct>();
		
		if (selfDestruct != null)
		{
			selfDestruct.DestroySelf();
			return true;
		}
		
		return false;
	}
}

public class SelfDestructTimed : MonoBehaviour, ISelfDestruct, IFixedUpdate
{
	public float zombieTime = 1.0f;
	private float remainingTime = 0.0f;
	private UpdateManager updateManager;

	public void DestroySelf()
	{
		updateManager = gameObject.GetComponentWithAncestors<UpdateManager>();
		updateManager.AddReciever(this);
		remainingTime = zombieTime;
	}
	
	public void FixedUpdateTick(float timestep)
	{
		remainingTime -= timestep;

		if (remainingTime <= 0.0)
		{
			updateManager.RemoveReciever(this);
			TimeManager.DestroyGameObject(gameObject);
		}
	}
}