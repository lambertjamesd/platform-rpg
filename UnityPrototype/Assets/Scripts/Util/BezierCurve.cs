using UnityEngine;
using System.Collections;

public class CubicBezierCurve {
	private Vector3[] handleLocations;

	public CubicBezierCurve(Vector3[] handles)
	{
		handleLocations = handles;
	}

	public Vector3 Eval(float t)
	{
		float tRev = 1.0f - t;
		return tRev * tRev * tRev * handleLocations[0] + 3.0f * (tRev * tRev * t * handleLocations[1] + tRev * t * t * handleLocations[2]) + t * t * t * handleLocations[3];
	}
}
