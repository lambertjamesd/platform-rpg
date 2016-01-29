using UnityEngine;
using System.Collections;

public class RaycastingTest : UnitTest
{
	private void VerifyRaycast(SimpleRaycastHit hit, Vector2 position, Vector2 normal, float distance)
	{
		Assert(hit != null, "Actually hit something");
		Assert(Vector2Helper.NearlyEquals(hit.Position, position), "Positions match");
		Assert(Vector2Helper.NearlyEquals(hit.Normal, normal), "Normals match");
		Assert(Mathf.Abs(hit.Distance - distance) < Raycasting.ERROR_TOLERANCE, "Distances match");
	}

	private void VerifyMiss(SimpleRaycastHit hit)
	{
		Assert(hit == null, "Nothing hit");
	}

	protected override void Run()
	{
		VerifyRaycast(
			Raycasting.RaycastLineSegment(
				new Ray2D(new Vector2(0.0f, -1.0f), new Vector2(0.0f, 1.0f)), 
				new Vector2(-1.0f, 0.0f), new Vector2(1.0f, 0.0f),
				new Vector2(0.0f, -1.0f)
			),
			new Vector2(0.0f, 0.0f),
			new Vector2(0.0f, -1.0f),
			1.0f
		);
		
		VerifyMiss(Raycasting.RaycastLineSegment(
			new Ray2D(new Vector2(0.0f, -1.0f), new Vector2(0.0f, 1.0f)), 
			new Vector2(-1.0f, 0.0f), new Vector2(1.0f, 0.0f),
			new Vector2(0.0f, 1.0f)
		));

		VerifyMiss(Raycasting.RaycastLineSegment(
			new Ray2D(new Vector2(-2.0f, -0.0f), new Vector2(1.0f, 1.0f).normalized),
			new Vector2(-0.5f, 0.5f),
			new Vector2(0.5f, -0.5f),
			new Vector2(-1.0f, -1.0f).normalized
		));

		VerifyRaycast(
			Raycasting.RaycastLineSegment(
				new Ray2D(new Vector2(-2.0f, -0.0f), new Vector2(1.0f, 1.0f).normalized),
				new Vector2(-1.0f, 1.0f),
				new Vector2(1.0f, -1.0f),
				new Vector2(-1.0f, -1.0f).normalized
			),
			new Vector2(-1.0f, 1.0f),
			new Vector2(-1.0f, -1.0f).normalized,
			Mathf.Sqrt(2.0f)
		);
		
		VerifyRaycast(
			Raycasting.SpherecastLineSegment(
				new Ray2D(new Vector2(0.0f, -2.0f), new Vector2(0.0f, 1.0f)), 
				1.0f,
				new Vector2(-1.0f, 0.0f), new Vector2(1.0f, 0.0f),
				new Vector2(0.0f, -1.0f)
			),
			new Vector2(0.0f, 0.0f),
			new Vector2(0.0f, -1.0f),
			1.0f
		);

		VerifyMiss(
			Raycasting.SpherecastLineSegment(
				new Ray2D(new Vector2(-1.0f - Mathf.Sqrt(2.0f) / 2.0f, -2.0f), new Vector2(0.0f, 1.0f)), 
				1.0f,
				new Vector2(-1.0f, 0.0f), new Vector2(1.0f, 0.0f),
				new Vector2(0.0f, -1.0f)
			)
		);
	}
}
