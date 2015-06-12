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

	public static bool FreefallSpeedToPoint(float edgeSlope, Vector2 target, float gravity, out float speed)
	{
		float zeroCheck = 2.0f * (target.y - edgeSlope * target.x);

		if (Mathf.Abs(zeroCheck) < ZERO_TOLERANCE || (gravity / zeroCheck) < 0.0f)
		{
			speed = 0.0f;
			return false;
		}
		else
		{
			float xVelocity = target.x * Mathf.Sqrt(gravity / zeroCheck);
			speed = Mathf.Abs(xVelocity) * Mathf.Sqrt(1 + edgeSlope * edgeSlope);
			return true;
		}
	}

	public static bool FreefallSpeedToLine(float edgeSlope, Vector2 targetA, Vector2 targetB, float gravity, out float speed, out float xPosition)
	{
		if (Mathf.Abs(targetA.x - targetB.x) < ZERO_TOLERANCE)
		{
			speed = 0.0f;
			xPosition = 0.0f;
			return false;
		}

		float lineSlope = (targetA.y - targetB.y) / (targetA.x - targetB.x);
		float slopeDifference = lineSlope - edgeSlope;
		Vector2 lineOffset = targetB - targetA;
		float zeroCheck = slopeDifference * (lineOffset.x * (edgeSlope + 0.5f * slopeDifference) - lineOffset.y);

		if (Mathf.Abs(zeroCheck) < ZERO_TOLERANCE)
		{
			speed = 0.0f;
			xPosition = 0.0f;
			return false;
		}

		float sqrtCheck = gravity * ColliderMath.Cross2D(targetA, lineOffset) / zeroCheck;

		if (sqrtCheck < 0.0f)
		{
			speed = 0.0f;
			xPosition = 0.0f;
			return false;
		}

		float xVelocity = Mathf.Sqrt(sqrtCheck);
		xPosition = (lineSlope - edgeSlope) * xVelocity * xVelocity / gravity;
		speed = Mathf.Abs(xVelocity) * Mathf.Sqrt(1 + edgeSlope * edgeSlope);
		return true;
	}

	public static float HeightForJump(float yVelocity, float gravity)
	{
		float timeOfPeak = -yVelocity / gravity;
		return yVelocity * timeOfPeak + 0.5f * gravity * timeOfPeak * timeOfPeak;
	}

	public static float TimeForFreefall(float height, float gravity)
	{
		return Mathf.Sqrt(Mathf.Abs(2.0f * height / gravity));
	}

	public static float TimeUntilSpeed(float velocity, float gravity)
	{
		return velocity / gravity;
	}

	public static float FallingDamageRatio(Vector2 velocity, Vector2 collisionNormal, float gravity, float minDamageDistance, float maxDamageDistance)
	{
		float normalVelocity = Vector2.Dot(velocity, collisionNormal);
		float fallTime = TimeUntilSpeed(normalVelocity, gravity);

		float minTime = TimeForFreefall(minDamageDistance, gravity);
		float maxTime = TimeForFreefall(maxDamageDistance, gravity);

		return Mathf.Clamp01((fallTime - minTime) / (maxTime - minTime));
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

	public static float EvaluateParabola(float a, float b, float c, float t)
	{
		return a * t * t + b * t + c;
	}

	public static void ParabolaForProjectile(float slope, float xVel, float gravity, out float a, out float b)
	{
		a = 0.5f * gravity / (xVel * xVel);
		b = slope;
	}

	public static void ParabolaForPeak(float maxJumpHeight, Vector2 target, out float a, out float b)
	{
		b = CalculateB(maxJumpHeight, target);
		a = CalculateA(b, maxJumpHeight, target);
	}

	public static bool PathCrossing(Vector2 v0, float gravity, Vector2 pointA, Vector2 pointB, out Vector2 result)
	{
		float x;

		if (Mathf.Abs(pointA.x - pointB.x) < ZERO_TOLERANCE || Mathf.Abs(v0.x) < ZERO_TOLERANCE)
		{
			x = pointA.x;
		}
		else
		{
			float lineSlope = (pointB.y - pointA.y) / (pointB.x - pointA.x);
			float velocitySlope = v0.y / v0.x;

			float a = gravity / (2.0f * v0.x * v0.x);
			float b = velocitySlope - lineSlope;
			float c = lineSlope * pointA.x - pointA.y;
			float sign = c < 0.0f ? b : gravity * v0.x; 

			if (!PathagareanTheorem(a, b, c, sign, out x)) {
				result = Vector2.zero;
				return false;
			}
		}

		if (Mathf.Abs(pointA.x - pointB.x) < ZERO_TOLERANCE)
		{
			float lerpValue = (x - pointA.x) / (pointB.x - pointA.x);
			float y = Mathf.Lerp(pointA.y, pointB.y, lerpValue);

			result = new Vector2(x, y);
			return lerpValue >= 0.0f && lerpValue <= 1.0f;
		}
		else if (Mathf.Abs(v0.x) < ZERO_TOLERANCE)
		{
			float t = x / v0.x;
			float y = EvaluateParabola(gravity * 0.5f, v0.y, 0.0f, t);
			float yLerp = (y - pointA.y) / (pointB.y - pointA.y);

			result = new Vector2(x, y);
			return yLerp >= 0.0f && yLerp <= 0.0f;
		}
		else
		{
			result = new Vector2(x, 0.0f);
			return false;
		}
	}

	public static bool PathagareanTheorem(float a, float b, float c, float sign, out float result)
	{
		if (Mathf.Abs(a) < ZERO_TOLERANCE)
		{
			result = 0.0f;
			return false;
		}

		float sqrtCheck = b * b - 4.0f * a * c;

		if (sqrtCheck < 0.0f)
		{
			result = 0.0f;
			return false;
		}

		result = (-b + Mathf.Sign(sign) * Mathf.Sqrt(sqrtCheck)) / (2.0f * a);
		return true;
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
