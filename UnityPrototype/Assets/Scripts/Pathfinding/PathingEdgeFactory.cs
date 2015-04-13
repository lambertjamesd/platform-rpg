using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathingEdgeFactory {
	private NodeEdgeFinder jumpEdgeFinder;
	private CharacterSize characterSize;

	public PathingEdgeFactory(List<ShapeOutline> environment, CharacterSize characterSize)
	{
		jumpEdgeFinder = new NodeEdgeFinder(environment, characterSize);
		this.characterSize = characterSize;
	}

	public JumpPathingEdge CreateJumpEdge(PlatformPathingNode startNode, PlatformPathingNode endNode)
	{
		if (startNode.PointB.x < endNode.PointA.x && startNode.IsCliffEdgeB && endNode.IsCliffEdgeA || 
		    endNode.PointB.x < startNode.PointA.x && endNode.IsCliffEdgeB && startNode.IsCliffEdgeA)
		{
			Vector2 startNodePoint;
			Vector2 endNodePoint;
			
			if (startNode.PointB.x < endNode.PointA.x)
			{
				startNodePoint = startNode.PointB;
				endNodePoint = endNode.PointA;
			}
			else
			{
				startNodePoint = startNode.PointA;
				endNodePoint = endNode.PointB;
			}

			List<NodeEdgeFinder.Range> clearJumpHeights = jumpEdgeFinder.FindClearRanges(startNodePoint, endNodePoint);

			if (clearJumpHeights.Count > 0)
			{

				return new JumpPathingEdge(startNodePoint, startNode, endNodePoint, endNode, clearJumpHeights, characterSize);
			}
		}
		
		return null;
	}
}
