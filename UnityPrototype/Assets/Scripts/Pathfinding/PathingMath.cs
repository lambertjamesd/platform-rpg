using UnityEngine;
using System.Collections;

public static class PathingMath {

	public static float ZERO_TOLERANCE = 0.00001f;

	// Finds a parabola that passes through the origin, p0, and p1
	public static bool ParabolaForPoints(Vector2 p0, Vector2 p1, out float a, out float b)
	{
		if (p0.x != 0.0f  && p1.x != 0.0f && p0.x != p1.x)
		{
			float recip = 1.0f / (p0.x * p0.x * p1.x - p1.x * p1.x * p0.x);
			a = (p1.x * p0.y - p0.x * p1.y) * recip;
			b = (p0.x * p0.x * p1.y - p1.x * p1.x * p0.y) * recip;
			return true;
		}
		else
		{
			a = 0.0f;
			b = 0.0f;

			return false;
		}
	}

	// Converts the coefficients for parabola in the form y(x) to x(t), y(t)
	public static bool ParabolaToParametric(float a, float b, float gravity, out float xVelocity, out float yVelocity)
	{
		float gravityCheck = gravity / (2.0f * a);

		if (gravityCheck < 0.0f)
		{
			xVelocity = 0.0f;
			yVelocity = 0.0f;
			return false;
		}
		else
		{
			xVelocity = Mathf.Sqrt(gravityCheck);
			yVelocity = b * xVelocity;
			return true;
		}
	}

	public static float HeightForJump(float yVelocity, float gravity)
	{
		float timeOfPeak = -yVelocity / gravity;
		return yVelocity * timeOfPeak + 0.5f * gravity * timeOfPeak * timeOfPeak;
	}

	public static float PeakPosition(float a, float b)
	{
		return -b / (2.0f * a);
	}

	public static float HeightForParabola(float a, float b)
	{
		float timeOfPeak = PeakPosition(a, b);
		return b * timeOfPeak + a * timeOfPeak * timeOfPeak;
	}

	public static float CalculateB(float maxJumpHeight, Vector2 target)
	{
		if (maxJumpHeight == 0.0f)
		{
			return 0.0f;
		}
		else
		{
			return 2.0f * (maxJumpHeight / target.x) * (1.0f + Mathf.Sqrt(1.0f - target.y / maxJumpHeight));
		}
	}

	public static float CalculateA(float b, float maxJumpHeight, Vector2 target)
	{
		if (Mathf.Abs(maxJumpHeight) < ZERO_TOLERANCE)
		{
			return target.y / (target.x * target.x);
		}
		else
		{
			return -0.25f * b * b / maxJumpHeight;
		}
	}

	public static void ParabolaForPeak(float maxJumpHeight, Vector2 target, out float a, out float b)
	{
		b = CalculateB(maxJumpHeight, target);
		a = CalculateA(b, maxJumpHeight, target);
	}

	// Finds a parabola that passes through the origin and endpoint and is tangent to the line segment (p0, p1)
	public static bool ParabolaForLine(Vector2 p0, Vector2 p1, Vector2 endpoint, out float a, out float b)
	{
		// vertical line cannot be tangent to a parabola
		if (Mathf.Abs(p0.x - p1.x) < ZERO_TOLERANCE)
		{
			a = 0.0f;
			b = 0.0f;
			return false;
		}
		
		float slope = (p1.y - p0.y) / (p0.x - p1.x);
		float endpointYOffset = slope * endpoint.x - endpoint.y;
		float x = 0.0f;

		if (Mathf.Abs(endpointYOffset) < ZERO_TOLERANCE)
		{
			x = 0.5f * endpoint.x;
		}
		else
		{
			float segmentYOffset = p0.y - slope * p0.x;
			
			float solutionTest = segmentYOffset * segmentYOffset + segmentYOffset * endpointYOffset;

			if (solutionTest < 0.0f)
			{
				// if the line segment from the origin to endpoint crosses the line formed
				// by p0 and p1, then there is no solution
				a = 0.0f;
				b = 0.0f;
				return false;
			}
			else 
			{
				x = endpoint.x * (-segmentYOffset + Mathf.Sign(segmentYOffset) * Mathf.Sqrt(solutionTest)) / endpointYOffset;
			}
		}

		float lerpValue = (x - p0.x) / (p1.x - p0.x);

		// make sure the point of contact lies on the line segment
		if (lerpValue < 0.0f || lerpValue > 1.0f)
		{
			a = 0.0f;
			b = 0.0f;
			return false;
		}

		float y = (x - p0.x) * slope + p0.y;

		return ParabolaForPoints(new Vector2(x, y), endpoint, out a, out b);
	}
}
