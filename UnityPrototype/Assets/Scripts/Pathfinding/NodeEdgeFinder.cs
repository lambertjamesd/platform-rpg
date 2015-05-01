using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NodeEdgeFinder {

	private ConcaveColliderGroup colliders;
	private CharacterSize characterSize;
	private Vector2 lowerCastOffset;
	private Vector2 upperCastOffset;

	public NodeEdgeFinder(ConcaveColliderGroup colliders, CharacterSize characterSize)
	{
		this.colliders = colliders;
		this.characterSize = characterSize;
		lowerCastOffset = Vector2.up * characterSize.radius;
		upperCastOffset = Vector2.up * (characterSize.height - characterSize.radius);
	}

	private bool IsSourcePoint(Vector2 testPoint, Vector2 nodePoint)
	{
		float offset = (testPoint - nodePoint).sqrMagnitude;

		return offset < characterSize.radius * characterSize.radius * 2.0f;
	}

	private Range BlockedHorizontalVelocities(ConvexSection section, Vector2 startPoint, Vector2 direction, Vector2 platformA, Vector2 platformB)
	{
		float upperJumpSpeed = float.MinValue;
		float lowerJumpSpeed = float.MaxValue;
		float xDirection = Mathf.Sign(direction.x);

		Vector2 normal = direction.x > 0.0f ? ColliderMath.RotateCCW(direction) : ColliderMath.RotateCW(direction);
		
		Vector2 lowerOrigin = startPoint + characterSize.radius * normal;
		Vector2 upperOrigin = lowerOrigin + Vector2.up * (characterSize.height - 2.0f * characterSize.radius);

		float edgeSlope = direction.y / direction.x;

		Vector2 platformNormal = ColliderMath.RotateCW(platformB - platformA);

		if (platformNormal.y < 0.0f)
		{
			platformNormal *= -1.0f;
		}

		for (int i = 0; i < section.PointCount; ++i)
		{
			Vector2 currentPoint = section.GetPoint(i);
			Vector2 nextPoint = section.GetPoint(i + 1);

			if (!IsSourcePoint(currentPoint, platformA) && !IsSourcePoint(nextPoint, platformB))
			{
				float speed;

				if ((currentPoint.x - lowerOrigin.x) * xDirection > 0.0f && 
				    ColliderMath.InFrontOf(currentPoint, platformNormal, platformA) &&
				    !IsSourcePoint(currentPoint, startPoint))
				{
					if (PathingMath.FreefallSpeedToPoint(edgeSlope, currentPoint - lowerOrigin, Physics.gravity.y, out speed))
					{
						upperJumpSpeed = Mathf.Max(speed, upperJumpSpeed);
					}

					if (PathingMath.FreefallSpeedToPoint(edgeSlope, currentPoint - upperOrigin, Physics.gravity.y, out speed))
					{
						lowerJumpSpeed = Mathf.Min(speed, lowerJumpSpeed);
					}
				}

				if (((currentPoint.x - lowerOrigin.x) * xDirection > 0.0f || (nextPoint.x - lowerOrigin.x) * xDirection > 0.0f) &&
				    Mathf.Abs(currentPoint.x - nextPoint.x) > ColliderMath.ZERO_TOLERANCE)
				{
					float xPosition;
					if (PathingMath.FreefallSpeedToLine(edgeSlope, currentPoint - upperOrigin, nextPoint - upperOrigin, Physics.gravity.y, out speed, out xPosition))
					{
						float worldX = xPosition + lowerOrigin.x;
						float lerpX = (worldX - currentPoint.x) / (nextPoint.x - currentPoint.x);

						if ((xPosition - lowerOrigin.x) * xDirection > 0.0f && lerpX >= 0.0f && lerpX <= 1.0f &&
						    ColliderMath.InFrontOf(new Vector2(worldX, Mathf.Lerp(currentPoint.x, nextPoint.x, lerpX)), platformNormal, platformA))
						{
							lowerJumpSpeed = Mathf.Min(speed, lowerJumpSpeed);
						}
					}

					if ((currentPoint.x - lowerOrigin.x) * xDirection < 0.0f || (nextPoint.x - lowerOrigin.x) * xDirection < 0.0f)
					{
						float xCrossing = (lowerOrigin.x - currentPoint.x) / (nextPoint.x - currentPoint.x);
						float yCrossing = Mathf.Lerp(currentPoint.y, nextPoint.y, xCrossing);

						if (xCrossing >= 0.0f && xCrossing <= 1.0f && yCrossing < lowerOrigin.y && 
						    ColliderMath.InFrontOf(new Vector2(lowerOrigin.x, yCrossing), platformNormal, platformA))
						{
							lowerJumpSpeed = float.MinValue;
						}
					}
				}
			}
		}

		return new Range(lowerJumpSpeed, upperJumpSpeed);
	}
	
	
	private List<Range> BlockedHorizontalRanges(Vector2 startPoint, Vector2 direction, Vector2 platformA, Vector2 platformB, BoundingBox jumpRange)
	{
		List<Range> result = new List<Range>();
		
		for (int i = 0; i < colliders.ColliderCount; ++i)
		{
			ConcaveCollider collider = colliders.GetCollider(i);
			if (collider.BB.Overlaps(jumpRange))
			{
				for (int j = 0; j < collider.SectionCount; ++j)
				{
					ConvexSection section = collider.GetSection(j);
					
					if (section.BB.Overlaps(jumpRange))
					{
						Range blockedRange = BlockedHorizontalVelocities(section, startPoint, direction, platformA, platformB);
						
						if (blockedRange.min < blockedRange.max)
						{
							result.Add(blockedRange);
						}
					}
				}
			}
		}
		
		return result;
	}
	
	private Range BlockedRange(ConvexSection section, Vector2 startPoint, Vector2 endPoint)
	{
		float upperJumpHeight = float.MinValue;
		float lowerJumpHeight = float.MaxValue;

		for (int i = 0; i < section.PointCount; ++i)
		{
			Vector2 currentPoint = section.GetPoint(i);
			Vector2 nextPoint = section.GetPoint(i + 1);
			
			float a;
			float b;

			float minX = Mathf.Min(startPoint.x, endPoint.x);
			float maxX = Mathf.Max(startPoint.x, endPoint.x);

			if (minX < currentPoint.x && maxX > currentPoint.x && 
			    !IsSourcePoint(currentPoint, startPoint) && 
			    !IsSourcePoint(currentPoint, endPoint))
			{
				Vector2 origin = startPoint + upperCastOffset;

				if (PathingMath.ParabolaForPoints(currentPoint - origin, endPoint - origin, out a, out b))
				{
					float peakX = PathingMath.PeakPosition(a, b) + startPoint.x;

					if (peakX >= minX && peakX <= maxX)
					{
						lowerJumpHeight = Mathf.Min(PathingMath.HeightForParabola(a, b), lowerJumpHeight);
					}
					else
					{
						// the peak of the jump is not between the start and end point horizontally
						// this means its impossible to go under
						lowerJumpHeight = float.MinValue;
					}
				}

				origin = startPoint + lowerCastOffset;

				if (PathingMath.ParabolaForPoints(currentPoint - origin, endPoint - origin, out a, out b))
				{
					float peakX = PathingMath.PeakPosition(a, b) + startPoint.x;

					if (peakX >= minX && peakX <= maxX)
					{
						upperJumpHeight = Mathf.Max(PathingMath.HeightForParabola(a, b), upperJumpHeight);
					}
					else
					{
						// the peak of the jump is not between the start and end point horizontally
						// this means it is not in the way at all
						upperJumpHeight = Mathf.Max(endPoint.y - startPoint.y, upperJumpHeight);
					}
				}
			}

			if (section.GetNormal(i).y < 0.0f)
			{
				Vector2 origin = startPoint + upperCastOffset;

				if (PathingMath.ParabolaForLine(currentPoint - origin, nextPoint - origin, endPoint - origin, out a, out b))
				{
					lowerJumpHeight = Mathf.Min(PathingMath.HeightForParabola(a, b), lowerJumpHeight);
				}

				if (Mathf.Abs(currentPoint.x - nextPoint.x) > PathingMath.ZERO_TOLERANCE)
				{
					for (int wallIndex = 0; wallIndex < 2; ++wallIndex)
					{
						Vector2 wallPoint = wallIndex == 1 ? startPoint : endPoint;

						float wallIntersection = (wallPoint.x - currentPoint.x) / (nextPoint.x - currentPoint.x);

						if (wallIntersection >= 0.0 && wallIntersection <= 1.0f)
						{
							float intersectionY = Mathf.Lerp(currentPoint.y, nextPoint.y, wallIntersection);

							if (intersectionY > wallPoint.y)
							{
								// top blocked, cant jump over it
								upperJumpHeight = float.MaxValue;
							}
							else 
							{
								// bottom block, can't go under it
								lowerJumpHeight = float.MinValue;
							}
						}
					}
				}
			}
		}

		return new Range(lowerJumpHeight, upperJumpHeight);
	}

	public struct Range
	{
		public Range(float min, float max)
		{
			this.min = min;
			this.max = max;
		}

		public float min;
		public float max;
	}

	private struct Boundary
	{
		public Boundary(float position, bool isStart)
		{
			this.position = position;
			this.isStart = isStart;
		}

		public float position;
		public bool isStart;
	}

	private List<Range> BlockedRanges(Vector2 startPoint, Vector2 endPoint, BoundingBox jumpRange)
	{
		List<Range> result = new List<Range>();

		for (int i = 0; i < colliders.ColliderCount; ++i)
		{
			ConcaveCollider collider = colliders.GetCollider(i);
			if (collider.BB.Overlaps(jumpRange))
			{
				for (int j = 0; j < collider.SectionCount; ++j)
				{
					ConvexSection section = collider.GetSection(j);

					if (section.BB.Overlaps(jumpRange))
					{
						Range blockedRange = BlockedRange(section, startPoint, endPoint);

						if (blockedRange.min < blockedRange.max)
						{
							result.Add(blockedRange);
						}
					}
				}
			}
		}

		return result;
	}

	private static List<Range> InvertRange(List<Range> ranges, float minValue, float maxValue)
	{
		List<Boundary> boundaries = new List<Boundary>();
		
		boundaries.Add(new Boundary(maxValue, true));
		boundaries.Add(new Boundary(minValue, false));
		
		foreach (Range range in ranges)
		{
			boundaries.Add(new Boundary(range.min, true));
			boundaries.Add(new Boundary(range.max, false));
		}
		
		boundaries.Sort((a, b) => {
			if (a.position > b.position)
			{
				return 1;
			}
			else if (a.position < b.position)
			{
				return -1;
			}
			else
			{
				return 0;
			}
		});
		
		float clearRangeStart = 0.0f;
		int blockedCount = 1;
		
		List<Range> result = new List<Range>();
		
		for (int i = 0; i < boundaries.Count; ++i)
		{
			if (boundaries[i].isStart)
			{
				if (blockedCount == 0 && clearRangeStart != boundaries[i].position)
				{
					result.Add(new Range(clearRangeStart, boundaries[i].position));
				}
				
				++blockedCount;
			}
			else
			{
				--blockedCount;
				
				if (blockedCount == 0)
				{
					clearRangeStart = boundaries[i].position;
				}
			}
		}
		
		return result;
	}

	public List<Range> FindClearRanges(Vector2 startPoint, Vector2 endPoint)
	{
		BoundingBox jumpRange = new BoundingBox(startPoint, endPoint);
		jumpRange.max.y = Mathf.Infinity;
		List<Range> blockedRanges = BlockedRanges(startPoint, endPoint, jumpRange);
		return InvertRange(blockedRanges, Mathf.Max(0.0f, endPoint.y - startPoint.y), float.MaxValue);
	}
	
	public List<Range> FindClearHorizontalRanges(Vector2 startPoint, Vector2 direction, Vector2 platformA, Vector2 platformB)
	{
		BoundingBox jumpRange = new BoundingBox(platformA, platformB);
		jumpRange.Extend(startPoint);
		jumpRange.max.y = Mathf.Infinity;
		List<Range> blockedRanges = BlockedHorizontalRanges(startPoint, direction, platformA, platformB, jumpRange);
		
		Vector2 normal = direction.x > 0.0f ? ColliderMath.RotateCCW(direction) : ColliderMath.RotateCW(direction);
		Vector2 lowerOrigin = startPoint + characterSize.radius * normal;

		Vector2 platformNormal = ColliderMath.RotateCW(platformB - platformA).normalized;
		
		if (platformNormal.y < 0.0f)
		{
			platformNormal *= -1.0f;
		}

		
		float edgeSlope = direction.y / direction.x;

		float xDirection = Mathf.Sign(direction.x);

		float minSpeed = 0.0f;
		float maxSpeed = float.MaxValue;
		float edgeSpeed;

		Vector2 closerPoint;
		Vector2 furtherPoint;

		if ((platformB - platformA).x * xDirection > 0.0f)
		{
			closerPoint = platformA + platformNormal * characterSize.radius;
			furtherPoint = platformB + platformNormal * characterSize.radius;
		}
		else
		{
			closerPoint = platformB + platformNormal * characterSize.radius;
			furtherPoint = platformA + platformNormal * characterSize.radius;
		}

		if (PathingMath.FreefallSpeedToPoint(edgeSlope, closerPoint - lowerOrigin, Physics.gravity.y, out edgeSpeed))
		{
			minSpeed = Mathf.Max(minSpeed, edgeSpeed);
		}
		
		if (PathingMath.FreefallSpeedToPoint(edgeSlope, furtherPoint - lowerOrigin, Physics.gravity.y, out edgeSpeed))
		{
			maxSpeed = edgeSpeed;
		}


		return InvertRange(blockedRanges, minSpeed, maxSpeed);
	}
}
