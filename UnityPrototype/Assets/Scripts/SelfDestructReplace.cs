using UnityEngine;
using System.Collections;

public class SelfDestructReplace : MonoBehaviour, ISelfDestruct
{
	public GameObject prefab;
	public bool preserveParent;
	
	public void DestroySelf()
	{
		GameObject result = (GameObject)GameObject.Instantiate(prefab, transform.position, transform.rotation);
		
		if (preserveParent)
		{
			result.transform.parent = transform.parent;
		}
		
		TimeManager.DestroyGameObject(gameObject);
	}
}