using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class PathingEdge
{
	public abstract void DebugDraw(Transform parentTransform);
}

public class FreefallPathingEdge : PathingEdge
{
	private struct PlatformRange
	{
		public float minInput;
		public Vector2 minPos;
		public float maxInput;
		public Vector2 maxPos;
	}

	private Vector2 freefallPoint;
	private Vector2 freefallDirection;
	private PlatformPathingNode source;
	private PlatformPathingNode target;
	private List<PlatformRange> movespeedRanges;
	private CharacterSize characterSize;

	public FreefallPathingEdge(Vector2 sourcePoint, Vector2 sourceDirection, PlatformPathingNode source, PlatformPathingNode target, List<NodeEdgeFinder.Range> movespeedRanges, CharacterSize characterSize)
	{
		this.freefallPoint = sourcePoint;
		this.freefallDirection = sourceDirection;
		this.source = source;
		this.target = target;

		Vector2 relativeA = target.PointA - sourcePoint;
		Vector2 relativeB = target.PointB - sourcePoint;

		this.movespeedRanges = movespeedRanges.Select(range => {
			PlatformRange result;
			result.minInput = range.min;
			PathingMath.PathCrossing(sourceDirection * range.min, Physics.gravity.y, relativeA, relativeB, out result.minPos);
			result.minPos += sourcePoint;
			result.maxInput = range.max;
			PathingMath.PathCrossing(sourceDirection * range.max, Physics.gravity.y, relativeA, relativeB, out result.maxPos);
			result.maxPos += sourcePoint;
			return result;
		}).ToList();

		this.characterSize = characterSize;
	}

	public override void DebugDraw(Transform parentTransform)
	{
		float slope = freefallDirection.y / freefallDirection.x;
		Vector2 normal = freefallDirection.x > 0.0f ? ColliderMath.RotateCCW(freefallDirection) : ColliderMath.RotateCW(freefallDirection);
		Vector2 lowerOrigin = freefallPoint + normal * characterSize.radius;
		Vector2 upperOrigin = lowerOrigin + Vector2.up * (characterSize.height - characterSize.radius * 2.0f);

		foreach (PlatformRange range in movespeedRanges)
		{
			float a;
			float b;
			
			Gizmos.color = Color.cyan;
			PathingMath.ParabolaForProjectile(slope, range.minInput * freefallDirection.x, Physics.gravity.y, out a, out b);
			JumpPathingEdge.DrawParabola(parentTransform, lowerOrigin, a, b, range.minPos.x - lowerOrigin.x);

			Gizmos.color = Color.Lerp(Color.yellow, Color.red, 0.5f);
			PathingMath.ParabolaForProjectile(slope, range.maxInput * freefallDirection.x, Physics.gravity.y, out a, out b);
			JumpPathingEdge.DrawParabola(parentTransform, upperOrigin, a, b, range.maxPos.x - lowerOrigin.x);
		}
	}
}

public class JumpPathingEdge : PathingEdge
{	
	private Vector2 sourcePoint;
	private Vector2 targetPoint;
	private PlatformPathingNode source;
	private PlatformPathingNode target;
	private List<NodeEdgeFinder.Range> jumpHeightRanges;
	private CharacterSize characterSize;

	public JumpPathingEdge(Vector2 sourcePoint, PlatformPathingNode source, Vector2 targetPoint, PlatformPathingNode target, List<NodeEdgeFinder.Range> jumpHeightRanges, CharacterSize characterSize)
	{
		this.sourcePoint = sourcePoint;
		this.source = source;
		this.targetPoint = targetPoint;
		this.target = target;
		this.jumpHeightRanges = jumpHeightRanges;
		this.characterSize = characterSize;
	}

	public static void DrawParabola(Transform parentTransform, Vector2 origin, float a, float b, float dx)
	{
		Vector3 lastPoint = parentTransform.TransformPoint(origin.x, origin.y, 0.0f);

		for (int i = 1; i <= 10; ++i)
		{
			float x = i * dx / 10;
			Vector3 currentPoint = parentTransform.TransformPoint(origin.x + x, origin.y + b * x + a * x * x, 0.0f);
			Gizmos.DrawLine(lastPoint, currentPoint);
			lastPoint = currentPoint;
		}
	}
	
	public override void DebugDraw(Transform parentTransform)
	{
		foreach (NodeEdgeFinder.Range range in jumpHeightRanges)
		{
			float a;
			float b;

			Gizmos.color = Color.green;
			PathingMath.ParabolaForPeak(range.min, targetPoint - sourcePoint, out a, out b);
			DrawParabola(parentTransform, sourcePoint + Vector2.up * characterSize.radius, a, b, targetPoint.x - sourcePoint.x);

			if (range.max != float.MaxValue)
			{
				Gizmos.color = Color.red;
				PathingMath.ParabolaForPeak(range.max, targetPoint - sourcePoint, out a, out b);
				DrawParabola(parentTransform, sourcePoint + Vector2.up * (characterSize.height - characterSize.radius), a, b, targetPoint.x - sourcePoint.x);
			}
		}
	}

	public JumpPathingEdge Inverse()
	{
		List<NodeEdgeFinder.Range> resultRanges = new List<NodeEdgeFinder.Range>();

		foreach (NodeEdgeFinder.Range range in jumpHeightRanges)
		{
			resultRanges.Add(new NodeEdgeFinder.Range(range.min - targetPoint.y + sourcePoint.y, range.max - targetPoint.y + sourcePoint.y));
		}

		return new JumpPathingEdge(targetPoint, target, sourcePoint, source, resultRanges, characterSize);
	}
}

public abstract class PathingNode
{
	protected List<PathingEdge> edges = new List<PathingEdge>();

	public abstract void ConnectTo(PathingNode target, PathingEdgeFactory edgeFactory);
	public abstract void ConnectToPlatformNode(PlatformPathingNode target, PathingEdgeFactory edgeFactory);
	public abstract bool Collides(Ray pickerTest);

	public virtual void DebugDraw(Transform parentTransform)
	{
		foreach (PathingEdge edge in edges)
		{
			edge.DebugDraw(parentTransform);
		}
	}
}

public class PlatformPathingNode : PathingNode
{
	private Vector2 pointA;
	private bool isCliffEdgeA;
	private Vector2 pointB;
	private bool isCliffEdgeB;

	public PlatformPathingNode(Vector2 pointA, bool isCliffEdgeA, Vector2 pointB, bool isCliffEdgeB)
	{
		if (pointA.x < pointB.x)
		{
			this.pointA = pointA;
			this.pointB = pointB;
			
			this.isCliffEdgeA = isCliffEdgeA;
			this.isCliffEdgeB = isCliffEdgeB;
		}
		else
		{
			this.pointA = pointB;
			this.pointB = pointA;
			
			this.isCliffEdgeA = isCliffEdgeB;
			this.isCliffEdgeB = isCliffEdgeA;
		}
	}

	public Vector2 PointA
	{
		get
		{
			return pointA;
		}
	}

	public bool IsCliffEdgeA
	{
		get
		{
			return isCliffEdgeA;
		}
	}
	
	public Vector2 PointB
	{
		get
		{
			return pointB;
		}
	}

	public bool IsCliffEdgeB
	{
		get
		{
			return isCliffEdgeB;
		}
	}
	
	public override void ConnectTo(PathingNode target, PathingEdgeFactory edgeFactory)
	{
		target.ConnectToPlatformNode(this, edgeFactory);
	}

	private static void CheckForFreefallEdge(PlatformPathingNode source, Vector2 sourcePos, Vector2 sourceDir, PlatformPathingNode target, PathingEdgeFactory edgeFactory)
	{
		FreefallPathingEdge freefallToTarget = edgeFactory.CreateFreefallEdge(source, sourcePos, sourceDir, target);
		
		if (freefallToTarget != null)
		{
			source.edges.Add(freefallToTarget);
		}
	}

	public override void ConnectToPlatformNode(PlatformPathingNode target, PathingEdgeFactory edgeFactory)
	{
		JumpPathingEdge edgeToTarget = edgeFactory.CreateJumpEdge(this, target);

		if (edgeToTarget != null)
		{
			this.edges.Add(edgeToTarget);
			target.edges.Add(edgeToTarget.Inverse());
		}

		if (isCliffEdgeA)
		{
			CheckForFreefallEdge(this, pointA, (pointA - pointB).normalized, target, edgeFactory);
		}

		if (isCliffEdgeB)
		{
			CheckForFreefallEdge(this, pointB, (pointB - pointA).normalized, target, edgeFactory);
		}

		if (target.isCliffEdgeA)
		{
			CheckForFreefallEdge(target, target.pointA, (target.pointA - target.pointB).normalized, this, edgeFactory);
		}

		if (target.isCliffEdgeB)
		{
			CheckForFreefallEdge(target, target.pointB, (target.pointB - target.pointA).normalized, this, edgeFactory);
		}
	}

	public override void DebugDraw(Transform parentTransform)
	{
		base.DebugDraw(parentTransform);
		Gizmos.color = Color.green;

		if (isCliffEdgeA)
		{
			Gizmos.DrawSphere(parentTransform.TransformPoint(pointA.x, pointA.y, 0.0f), 0.25f);
		}

		if (isCliffEdgeB)
		{
			Gizmos.DrawSphere(parentTransform.TransformPoint(pointB.x, pointB.y, 0.0f), 0.25f);
		}
	}
	
	public override bool Collides(Ray pickerTest)
	{
		return ColliderMath.DoesCollide(pickerTest, pointA, 0.25f) || ColliderMath.DoesCollide(pickerTest, pointB, 0.25f);
	}
}

[System.Serializable]
public class CharacterSize {
	public float height;
	public float radius;
}

public class PathingNetwork {

	private List<PathingNode> nodes = new List<PathingNode>();
	private ConcaveColliderGroup extrudedColliders;

	public PathingNetwork(List<ShapeOutline> environment, CharacterSize characterSize)
	{
		List<ShapeOutline> extrudedOutlines = environment.Select(outline => outline.Extrude(characterSize.radius)).ToList();
		extrudedColliders = new MeshOutlineGenerator(extrudedOutlines).Result;
		PathingNodeFactory nodeFactory = new PathingNodeFactory(environment);
		PathingEdgeFactory edgeFactory = new PathingEdgeFactory(extrudedColliders, characterSize);
		nodes = nodeFactory.GenerateNodes();

		for (int nodeAIndex = 0; nodeAIndex < nodes.Count; ++nodeAIndex)
		{
			for (int nodeBIndex = nodeAIndex + 1; nodeBIndex < nodes.Count; ++nodeBIndex)
			{
				nodes[nodeAIndex].ConnectTo(nodes[nodeBIndex], edgeFactory);
			}
		}
	}

	public void DebugDraw(Transform parentTransform, int onlyNode)
	{
		for (int i = 0; i < nodes.Count; ++i)
		{
			if (onlyNode == -1 || onlyNode == i)
			{
				nodes[i].DebugDraw(parentTransform);
			}
		}

		if (extrudedColliders != null)
		{
			extrudedColliders.DebugDraw(parentTransform, false, Color.white);
		}
	}

	public int SelectNode(Ray localRay)
	{
		for (int i = 0; i < nodes.Count; ++i)
		{
			if (nodes[i].Collides(localRay))
			{
				return i;
			}
		}

		return -1;
	}
}
