using UnityEngine;
using UnityEditor;
using System.Collections;

public class SpellConnection {

	private SpellNodeConnector source;
	private SpellNodeConnector destination;

	public SpellConnection(SpellNodeConnector source, SpellNodeConnector destination)
	{
		this.source = source;
		this.destination = destination;
	}

	public bool Contains(SpellNodeConnector connector)
	{
		return connector == source || connector == destination;
	}

	public void Draw()
	{
		DrawNodeCurve(source.BoundingRect, destination.BoundingRect);
	}

	public SpellNodeConnector Source
	{
		get
		{
			return source;
		}
	}

	public SpellNodeConnector Destination
	{
		get
		{
			return destination;
		}
	}
	
	public static void DrawNodeCurve(Rect start, Rect end) {
		Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
		Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
		Vector3 startTan = startPos + Vector3.right * 50;
		Vector3 endTan = endPos + Vector3.left * 50;
		Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 2);
	}
}
