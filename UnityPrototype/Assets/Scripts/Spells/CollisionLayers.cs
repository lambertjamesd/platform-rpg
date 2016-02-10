using UnityEngine;
using System.Collections;

public static class CollisionLayers {
	public const int ObstacleLayers = 0x1;
	public const int TeamLayers = 0xF0;
	public const int TeamLayerOffset = 4;

	public const int WeaponLayerBitmask = 0xF00;
	public const int WeaponLayerOffset = 8;

	public static int EnemyLayers(int teamIndex)
	{
		int mask = ~(1 << (teamIndex + TeamLayerOffset));
		return TeamLayers & mask;
	}

	public static int AllyLayers(int teamIndex)
	{
		return 1 << (teamIndex + TeamLayerOffset);
	}
	
	public static int WeaponLayers(int teamIndex)
	{
		return 1 << (teamIndex + WeaponLayerOffset);
	}

	public static object AllyLayerMask(object[] parameters)
	{
		return AllyLayers((int)parameters[0]);
	}

	public static object EnemyLayerMask(object[] parameters)
	{
		return EnemyLayers((int)parameters[0]);
	}
	
	public static object WeaponLayerMask(object[] parameters)
	{
		return WeaponLayers((int)parameters[0]);
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
		SpacialIndex spacialIndex = (SpacialIndex)parameters[5];

		Vector3 dir = (end - start).normalized;
		float distance = Vector3.Dot(dir, end - start);
		ShapeRaycastHit hit;
		
		if (radius <= 0.0f)
		{
			hit = spacialIndex.Raycast(new Ray2D(start, dir), distance, -1, collisionLayers);
		}
		else
		{
			hit = spacialIndex.Spherecast(new Ray2D(start, dir), radius, distance, -1, collisionLayers);
		}
		
		if (debug)
		{
			if (hit != null)
			{
				Debug.DrawLine(start, hit.Distance * dir + start, Color.red);
			}
			else
			{
				Debug.DrawLine(start, end, Color.green);
			}
		}
		
		return hit != null;
	}
}
