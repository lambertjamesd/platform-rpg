using UnityEngine;
using System.Collections;

public static class CollisionLayers {
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
}
