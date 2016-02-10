using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpacialIndexNode
{
	private BoundingBox boundary;
	private BoundingBoxShape boundaryShape;
	private Vector2 midpoint;
	private SpacialIndexNode parent;
	private int depth;
	private SpacialIndexNode[] childrenNodes;
	private int totalContained = 0;
	private List<ICollisionShape> shapes = new List<ICollisionShape>();
	private Dictionary<ICollisionShape, SpacialIndexNode> shapeToNode;

	public SpacialIndexNode(BoundingBox boundary, SpacialIndexNode parent, int depth, Dictionary<ICollisionShape, SpacialIndexNode> shapeToNode)
	{
		this.boundary = boundary;
		this.boundaryShape = new BoundingBoxShape(boundary);
		this.midpoint = boundary.Lerp(new Vector2(0.5f, 0.5f));
		this.parent = parent;
		this.depth = depth;
		this.shapeToNode = shapeToNode;
	}

	private int IndexOfChild(Vector2 point)
	{
		if (point.x == midpoint.x || point.y == midpoint.y)
		{
			return -1;
		}
		else
		{
			return ((point.x < midpoint.x) ? 0 : 1) + ((point.y < midpoint.y) ? 0 : 2);
		}
	}

	public SpacialIndexNode InsertIntoChildren(ICollisionShape shape, BoundingBox shapeBB, SpacialIndexNode currentNode)
	{
		int minLocation = IndexOfChild(shapeBB.min);
		int maxLocation = IndexOfChild(shapeBB.max);

		if (minLocation == maxLocation && minLocation != -1)
		{
			SpacialIndexNode child = childrenNodes[minLocation];

			return child.Insert(shape, shapeBB, currentNode);
		}
		else
		{
			return null;
		}
	}

	private void Subdivide()
	{
		childrenNodes = new SpacialIndexNode[4];
		BoundingBox[] subBoundaries = boundary.Subdivide(new Vector2(0.5f, 0.5f));
		
		for (int i = 0; i < 4; ++i)
		{
			childrenNodes[i] = new SpacialIndexNode(subBoundaries[i], this, depth + 1, shapeToNode);
		}

		for (int i = shapes.Count - 1; i >= 0; --i)
		{
			BoundingBox shapeBB = shapes[i].GetBoundingBox();
			int minChild = IndexOfChild(shapeBB.min);
			int maxChild = IndexOfChild(shapeBB.max);

			if (minChild == maxChild && minChild != -1)
			{
				childrenNodes[minChild].Insert(shapes[i], shapeBB, this);
				shapes.RemoveAt(i);
			}
		}
	}

	public SpacialIndexNode Insert(ICollisionShape shape, BoundingBox shapeBB, SpacialIndexNode currentNode)
	{
		if (shapes.Count >= SpacialIndex.MAX_CHILDREN && depth <= SpacialIndex.MAX_DEPTH)
		{
			if (childrenNodes == null)
			{
				Subdivide();
			}

			SpacialIndexNode result = InsertIntoChildren(shape, shapeBB, currentNode);

			if (result != null)
			{
				if (result != currentNode)
				{
					++totalContained;
				}

				return result;
			}
		}

		if (currentNode != this)
		{
			++totalContained;
			shapeToNode[shape] = this;
			shapes.Add(shape);
		}

		return this;
	}

	private void Combine()
	{
		if (childrenNodes != null)
		{
			foreach (SpacialIndexNode child in childrenNodes)
			{
				child.Combine();

				foreach (ICollisionShape shape in child.shapes)
				{
					shapeToNode[shape] = this;
				}

				shapes.AddRange(child.shapes);
			}

			childrenNodes = null;
		}
	}

	public void Remove(ICollisionShape shape)
	{
		shapes.Remove(shape);

		SpacialIndexNode chain = this;

		while (chain != null)
		{
			--chain.totalContained;

			if (chain.totalContained <= SpacialIndex.MIN_CHILDREN_SPLIT)
			{
				Combine();
			}

			chain = chain.parent;
		}
	}
	
	public void CollideBB(BoundingBox bb, SpacialIndex.ShapeCallback callback)
	{
		if (bb.Overlaps(boundary))
		{
			foreach (ICollisionShape shape in shapes)
			{
				if (bb.Overlaps(shape.GetBoundingBox()))
				{
					callback(shape);
				}
			}

			if (childrenNodes != null)
			{
				foreach (SpacialIndexNode child in childrenNodes)
				{
					child.CollideBB(bb, callback);
				}
			}
		}
	}

	private class ChildDistance
	{
		public SpacialIndexNode node;
		public float distance;

		public ChildDistance(SpacialIndexNode node, float distance)
		{
			this.node = node;
			this.distance = distance;
		}
	}
	
	public void Raycast(SpacialIndex.RaycastDelegate caster, BoundingBox startBB, SpacialIndex.RaycastState state, int collisionGroup, int collisionLayers)
	{
		foreach (ICollisionShape shape in shapes)
		{
			if (CollisionShape.Collides(shape, collisionGroup, collisionLayers))
			{
				SimpleRaycastHit maybeHit = caster(shape);

				if (maybeHit != null)
				{
					state.Update(new ShapeRaycastHit(shape, maybeHit));
				}
			}
		}

		if (childrenNodes != null)
		{
			foreach (SpacialIndexNode child in childrenNodes)
			{
				if (child.boundary.Overlaps(startBB))
				{
					child.Raycast(caster, startBB, state, collisionGroup, collisionLayers);
				}
			}

			childrenNodes
				.Select(
					child => {
						if (child.boundary.Overlaps(startBB))
						{
							// if the bb overlaps, this child has already been handled
							return null;
						}
						else
						{
							SimpleRaycastHit childHit = caster(child.boundaryShape);

							if (childHit != null && childHit.Distance <= state.nearestDistance)
							{
								return new ChildDistance(child, childHit.Distance);
							}
							else
							{
								return null;
							}
						}
					}
				)
				.Where(x => x != null)
				.OrderBy( a => a.distance)
				.SkipWhile( child => {
					if (child.distance <= state.nearestDistance)
					{
						child.node.Raycast(caster, startBB, state, collisionGroup, collisionLayers);
						return true;
					}
					else
					{
						return false;
					}
				});
		}
	}
	
	public void DrawGizmos()
	{
		Vector2 center = boundary.Lerp(new Vector2(0.5f, 0.5f));
		Vector2 size = boundary.Size;
		Gizmos.DrawWireCube(new Vector3(center.x, center.y), new Vector3(size.x, size.y, 1.0f));

		if (childrenNodes != null)
		{
			foreach (SpacialIndexNode child in childrenNodes)
			{
				child.DrawGizmos();
			}
		}
	}
}

public class SpacialIndex
{
	public static readonly int MAX_DEPTH = 6;
	public static readonly int MAX_CHILDREN = 10;
	public static readonly int MIN_CHILDREN_SPLIT = 5;

	private BoundingBox boundary;
	private SpacialIndexNode rootNode;
	private Dictionary<ICollisionShape, SpacialIndexNode> shapeMapping = new Dictionary<ICollisionShape, SpacialIndexNode>();

	public SpacialIndex (BoundingBox boundary)
	{
		rootNode = new SpacialIndexNode(boundary, null, 1, shapeMapping);
	}

	public void IndexShape(ICollisionShape shape)
	{
		BoundingBox shapeBB = shape.GetBoundingBox();
		SpacialIndexNode lastNode = shapeMapping.ContainsKey(shape) ? shapeMapping[shape] : null;
		SpacialIndexNode nextNode = rootNode.Insert(shape, shapeBB, lastNode);

		if (lastNode != nextNode && lastNode != null)
		{
			lastNode.Remove(shape);
		}
	}

	public void RemoveShape(ICollisionShape shape)
	{
		if (shapeMapping.ContainsKey(shape))
		{
			shapeMapping[shape].Remove(shape);
			shapeMapping.Remove(shape);
		}
	}

	public delegate void ShapeCallback(ICollisionShape shape);

	public void CollideBB(BoundingBox bb, ShapeCallback callback)
	{
		rootNode.CollideBB(bb, callback);
	}
	
	public List<ICollisionShape> OverlapShape(ICollisionShape shape)
	{
		List<ICollisionShape> result = new List<ICollisionShape>();
		
		CollideBB(shape.GetBoundingBox(), x => {
			if (CollisionShape.Collides(x, shape.CollisionGroup, shape.CollisionLayers) &&
			    x.Overlap(shape) != null) {
				result.Add(x);
			}
		});
		
		return result;
	}

	public List<ICollisionShape> CircleOverlap(Vector2 center, float radius, int collisionLayers, int collisionGroup)
	{
		CircleShape circle = new CircleShape(radius);
		circle.Center = center;
		circle.CollisionLayers = collisionLayers;
		circle.CollisionGroup = collisionGroup;

		return OverlapShape(circle);
	}

	public class RaycastState
	{
		public ShapeRaycastHit nearestHit;
		public float nearestDistance;
		private bool multi;
		private List<ShapeRaycastHit> allHits;
		
		public RaycastState(float maxDistance, bool multi)
		{
			nearestHit = null;
			nearestDistance = maxDistance;
			this.multi = multi;

			if (multi)
			{
				allHits = new List<ShapeRaycastHit>();
			}
		}

		public void Update(ShapeRaycastHit hit)
		{
			if (hit.Distance <= nearestDistance)
			{
				if (multi)
				{
					allHits.Add(hit);
				}
				else
				{
					// this handles the case where the distances are
					// equal while still being deterministic
					nearestHit = ShapeRaycastHit.NearestHit(nearestHit, hit);
					nearestDistance = nearestHit.Distance;
				}
			}
		}

		public IEnumerable<ShapeRaycastHit> AllHits
		{
			get
			{
				return allHits;
			}
		}
	}

	public delegate SimpleRaycastHit RaycastDelegate(ICollisionShape shape);

	public RaycastState Raycast(RaycastDelegate caster, BoundingBox startBB, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0, bool multi = false)
	{
		RaycastState state = new RaycastState(maxDistance, multi);
		rootNode.Raycast(caster, startBB, state, collisionGroup, collisionLayers);
		return state;
	}

	private RaycastState RaycastInteral(Ray2D ray, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0, bool multi = false)
	{
		return Raycast(
			shape => shape.Raycast(ray), 
			new BoundingBox(
				ray.origin - Vector2.one * Raycasting.ERROR_TOLERANCE, 
				ray.origin + Vector2.one * Raycasting.ERROR_TOLERANCE
			),
			maxDistance,
			collisionGroup,
			collisionLayers,
			multi
		);
	}

	private RaycastState SpherecastInternal(Ray2D ray, float radius, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0, bool multi = false)
	{
		return Raycast(
			shape => shape.Spherecast(ray, radius),
			new BoundingBox(
				ray.origin - Vector2.one * (Raycasting.ERROR_TOLERANCE + radius),
				ray.origin + Vector2.one * (Raycasting.ERROR_TOLERANCE + radius)
			),
			maxDistance,
			collisionGroup,
			collisionLayers,
			multi
		);
	}

	private RaycastState CapsulecastInternal(Ray2D ray, float radius, float innerHeight, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0, bool multi = false)
	{
		Vector2 halfSize = new Vector2(radius + Raycasting.ERROR_TOLERANCE, radius + Raycasting.ERROR_TOLERANCE + innerHeight * 0.5f);

		return Raycast(
			shape => shape.CapsuleCast(ray, radius, innerHeight),
			new BoundingBox(
				ray.origin - halfSize,
				ray.origin + halfSize
			),
			maxDistance,
			collisionGroup,
			collisionLayers,
			multi
		);
	}

	public ShapeRaycastHit Raycast(Ray2D ray, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0)
	{
		return RaycastInteral(ray, maxDistance, collisionGroup, collisionLayers, false).nearestHit;
	}

	public ShapeRaycastHit Spherecast(Ray2D ray, float radius, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0)
	{
		return SpherecastInternal(ray, radius, maxDistance, collisionGroup, collisionLayers, false).nearestHit;
	}
	
	public ShapeRaycastHit Capsulecast(Ray2D ray, float radius, float innerHeight, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0)
	{
		return CapsulecastInternal(ray, radius, innerHeight, maxDistance, collisionGroup, collisionLayers, false).nearestHit;
	}
	
	public IEnumerable<ShapeRaycastHit> RaycastMulti(Ray2D ray, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0)
	{
		return RaycastInteral(ray, maxDistance, collisionGroup, collisionLayers, true).AllHits;
	}
	
	public IEnumerable<ShapeRaycastHit> SpherecastMulti(Ray2D ray, float radius, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0)
	{
		return SpherecastInternal(ray, radius, maxDistance, collisionGroup, collisionLayers, true).AllHits;
	}
	
	public IEnumerable<ShapeRaycastHit> CapsulecastMulti(Ray2D ray, float radius, float innerHeight, float maxDistance = float.PositiveInfinity, int collisionGroup = -1, int collisionLayers = ~0)
	{
		return CapsulecastInternal(ray, radius, innerHeight, maxDistance, collisionGroup, collisionLayers, true).AllHits;
	}

	public void DrawGizmos()
	{
		rootNode.DrawGizmos();
	}
}
