using UnityEngine;
using System.Collections;

public static class SpellColors {

	public static object HealColor(object[] input)
	{
		return new Color(0.0f, 1.0f, 0.0f, 0.25f);
	}

	public static object SpeedColor(object[] input)
	{
		return new Color(0.9f, 0.0f, 0.7f, 0.25f);
	}
	
	public static object TeleportColor(object[] input)
	{
		return new Color(0.0f, 0.8f, 0.7f, 0.25f);
	}
}
