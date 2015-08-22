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