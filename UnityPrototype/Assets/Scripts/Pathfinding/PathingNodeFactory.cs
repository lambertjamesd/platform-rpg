using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathingNodeFactory {
	private List<ShapeOutline> environment;

	private static readonly float TOP_SURFACE_Y_TOLERANCE = 0.5f;
	private int currentSurfaceId = 0;

	public PathingNodeFactory(List<ShapeOutline> environment)
	{
		this.environment = environment;
	}

	private void NodesFromOutline(ShapeOutline outline, List<PathingNode> result)
	{
		for (int i = 0; i < outline.PointCount; ++i)
		{
			if (outline.GetNormal(i).y > TOP_SURFACE_Y_TOLERANCE)
			{
				result.Add(new PlatformPathingNode(outline.GetPoint(i), outline.GetPoint(i + 1)));

				++currentSurfaceId;
			}
		}
	}

	public List<PathingNode> GenerateNodes()
	{
		List<PathingNode> result = new List<PathingNode>();
		
		for (int i = 0; i < environment.Count; ++i)
		{
			NodesFromOutline(environment[i], result);
		}

		return result;
	}
}
