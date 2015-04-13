using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class PathingEdge
{
	public abstract void DebugDraw(Transform parentTransform);
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

	private static void DrawParabola(Transform parentTransform, Vector2 origin, float a, float b, float dx)
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
		}
		else
		{
			this.pointA = pointB;
			this.pointB = pointA;
			this.isCliffEdgeB = isCliffEdgeB;
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

	public override void ConnectToPlatformNode(PlatformPathingNode target, PathingEdgeFactory edgeFactory)
	{
		JumpPathingEdge edgeToTarget = edgeFactory.CreateJumpEdge(this, target);

		if (edgeToTarget != null)
		{
			this.edges.Add(edgeToTarget);
			target.edges.Add(edgeToTarget.Inverse());
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
}

[System.Serializable]
public class CharacterSize {
	public float height;
	public float radius;
}

public class PathingNetwork {

	private List<PathingNode> nodes = new List<PathingNode>();
	private List<ShapeOutline> extrudedColliders;

	public PathingNetwork(List<ShapeOutline> environment, CharacterSize characterSize)
	{
		extrudedColliders = environment.Select(outline => outline.Extrude(characterSize.radius)).ToList();
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

	public void DebugDraw(Transform parentTransform)
	{
		foreach (PathingNode node in nodes)
		{
			node.DebugDraw(parentTransform);
		}

		foreach (ShapeOutline outline in extrudedColliders)
		{
			outline.DebugDraw(parentTransform, Color.white);
		}
	}
}
