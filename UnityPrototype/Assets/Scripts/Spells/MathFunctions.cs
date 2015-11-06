using UnityEngine;
using System.Collections;

public static class MathFunctions {
	public static object Floor(object[] parameters)
	{
		return Mathf.Floor((float)parameters[0]);
	}
}
