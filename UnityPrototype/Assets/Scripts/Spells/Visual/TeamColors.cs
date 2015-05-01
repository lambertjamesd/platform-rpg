using System;
using UnityEngine;

public static class TeamColors
{
	public static Color[] COLORS = new Color[]{
		Color.green,
		Color.cyan,
		Color.Lerp(Color.red, Color.yellow, 0.5f),
		Color.magenta
	};

	public static Color GetColor(int team)
	{
		return (team >= 0 && team < COLORS.Length) ? COLORS[team] : Color.red;
	}
}
