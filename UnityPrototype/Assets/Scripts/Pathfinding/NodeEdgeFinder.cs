using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NodeEdgeFinder {

	private List<ShapeOutline> colliders;
	private float characterRadius;
	private Vector2 lowerCastOffset;
	private Vector2 upperCastOffset;

	public NodeEdgeFinder(List<ShapeOutline> colliders, CharacterSize characterSize)
	{
		this.colliders = colliders;
		this.characterRadius = characterSize.radius;
		lowerCastOffset = Vector2.up * characterSize.radius;
		upperCastOffset = Vector2.up * (characterSize.height - characterSize.radius);
	}

	private bool IsSourcePoint(Vector2 testPoint, Vector2 nodePoint)
	{
		float offset = (testPoint - nodePoint).sqrMagnitude;

		return offset < characterRadius * characterRadius * 2.0f;
	}
	
	private Range BlockedRange(ShapeOutline section, Vector2 startPoint, Vector2 endPoint)
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

	private List<Range> BlockedRanges(List<ShapeOutline> colliderGroup, Vector2 startPoint, Vector2 endPoint, BoundingBox jumpRange)
	{
		List<Range> result = new List<Range>();

		for (int i = 0; i < colliderGroup.Count; ++i)
		{
			ShapeOutline collider = colliderGroup[i];
			if (collider.BB.Overlaps(jumpRange))
			{
				Range blockedRange = BlockedRange(collider, startPoint, endPoint);

				if (blockedRange.min < blockedRange.max)
				{
					result.Add(blockedRange);
				}
			}
		}

		return result;
	}

	public List<Range> FindClearRanges(Vector2 startPoint, Vector2 endPoint)
	{
		BoundingBox jumpRange = new BoundingBox(startPoint, endPoint);
		jumpRange.max.y = Mathf.Infinity;

		List<Range> blockedRanges = BlockedRanges(colliders, startPoint, endPoint, jumpRange);
		List<Boundary> boundaries = new List<Boundary>();

		boundaries.Add(new Boundary(float.MaxValue, true));
		boundaries.Add(new Boundary(Mathf.Max(0.0f, endPoint.y - startPoint.y), false));

		foreach (Range blockedRange in blockedRanges)
		{
			boundaries.Add(new Boundary(blockedRange.min, true));
			boundaries.Add(new Boundary(blockedRange.max, false));
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
}
