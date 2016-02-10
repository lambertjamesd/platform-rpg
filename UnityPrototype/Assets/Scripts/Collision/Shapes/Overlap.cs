using UnityEngine;

public class SimpleOverlap
{
	private Vector2 from;
	private Vector2 to;

	public SimpleOverlap(Vector2 from, Vector2 to)
	{
		this.from = from;
		this.to = to;
	}

	public SimpleOverlap Reverse()
	{
		return new SimpleOverlap(to, from);
	}

	public static SimpleOverlap Reverse(SimpleOverlap overlap)
	{
		if (overlap == null)
		{
			return null;
		}
		else
		{
			return overlap.Reverse();
		}
	}
	
	public Vector2 From
	{
		get
		{
			return from;
		}
	}

	public Vector2 To
	{
		get
		{
			return to;
		}
	}
}