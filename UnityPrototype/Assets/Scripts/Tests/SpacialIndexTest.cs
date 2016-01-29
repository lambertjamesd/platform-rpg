using UnityEngine;
using System.Collections;

public class SpacialIndexTest : UnitTest
{
	protected override void Run()
	{
		SpacialIndex index = new SpacialIndex(new BoundingBox(0.0f, 0.0f, 10.0f, 10.0f));
	}
}