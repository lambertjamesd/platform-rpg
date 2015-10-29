using UnityEngine;
using System.Collections;

public static class CollisionLayers {
	public const int ObstacleLayers = 0x1;
	public const int TeamLayers = 0x24900;
	public const int WeaponLayers = 0x49200;
	public const int HitboxLayers = 0x92400;

	public static int EnemyLayers(int layerIndex)
	{
		int mask = ~(1 << layerIndex);
		return TeamLayers & mask;
	}

	public static int AllyLayers(int layerIndex)
	{
		return 1 << layerIndex;
	}

	public static object AllyLayerMask(object[] parameters)
	{
		return AllyLayers((int)parameters[0]);
	}

	public static object EnemyLayerMask(object[] parameters)
	{
		return EnemyLayers((int)parameters[0]);
	}

	public static object CharacterLayerMask(object[] parameters)
	{
		return TeamLayers;
	}
	
	public static object IntToBitmask(object[] parameters)
	{
		return parameters[0];
	}

	public static object ObstacleLayerMask(object[] parameters)
	{
		return ObstacleLayers;
	}

	public static object IsBlocked(object[] parameters)
	{
		Vector3 start = (Vector3)parameters[0];
		Vector3 end = (Vector3)parameters[1];
		int collisionLayers = (int)(parameters[2] ?? ObstacleLayers);
		float radius = (float)(parameters[3] ?? 0.0f);
		bool debug = (bool)(parameters[4] ?? false);

		Vector3 dir = (end - start).normalized;
		float distance = Vector3.Dot(dir, end - start);
		bool result;
		RaycastHit hit;
		
		if (radius <= 0.0f)
		{
			result = Physics.Raycast(start, dir, out hit, distance, collisionLayers);
		}
		else
		{
			result = Physics.SphereCast(start, radius, dir, out hit, distance, collisionLayers);
		}
		
		if (debug)
		{
			if (result)
			{
				Debug.DrawLine(start, hit.distance * dir + start, Color.red);
			}
			else
			{
				Debug.DrawLine(start, end, Color.green);
			}
		}
		
		return result;
	}
}
