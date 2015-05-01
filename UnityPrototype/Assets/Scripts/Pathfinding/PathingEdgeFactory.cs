using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathingEdgeFactory {
	private NodeEdgeFinder jumpEdgeFinder;
	private CharacterSize characterSize;

	public PathingEdgeFactory(ConcaveColliderGroup environment, CharacterSize characterSize)
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
	
	public FreefallPathingEdge CreateFreefallEdge(PlatformPathingNode startNode, Vector2 startNodePosition, Vector2 startNodeDirection, PlatformPathingNode endNode)
	{
		if ((startNodePosition.y > endNode.PointA.y || startNodePosition.y > endNode.PointB.y) &&
		    ((endNode.PointA.x - startNodePosition.x) * startNodeDirection.x > 0.0f || (endNode.PointB.x - startNodePosition.x) * startNodeDirection.x > 0.0f))
		{
			
			List<NodeEdgeFinder.Range> clearSpeeds = jumpEdgeFinder.FindClearHorizontalRanges(startNodePosition, startNodeDirection, endNode.PointA, endNode.PointB);
			
			if (clearSpeeds.Count > 0)
			{	
				return new FreefallPathingEdge(startNodePosition, startNodeDirection, startNode, endNode, clearSpeeds, characterSize);
			}
		}
		
		return null;
	}
}
