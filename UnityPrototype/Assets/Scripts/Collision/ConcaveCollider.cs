using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ConcaveCollider {
	[SerializeField]
	private ConvexSection[] sections;
	private BoundingBox boundingBox;

	public ConcaveCollider(ConvexSection[] sections)
	{
		this.sections = sections;

		if (sections.Length > 0)
		{
			boundingBox = sections[0].BB;
		}

		for (int i = 0; i < sections.Length; ++i)
		{
			boundingBox = boundingBox.Union(sections[i].BB);

			sections[i].SaveAdjacentIndices(this);
		}
	}

	public int SectionCount
	{
		get
		{
			return sections.Length;
		}
	}

	public ConvexSection GetSection(int index)
	{
		if (index == -1)
		{
			return null;
		}
		else
		{
			return sections[index];
		}
	}

	public int GetIndex(ConvexSection section)
	{
		for (int i = 0; i < sections.Length; ++i)
		{
			if (section == sections[i])
			{
				return i;
			}
		}

		return -1;
	}
	
	public void ReconnectSections()
	{
		foreach (ConvexSection section in sections)
		{
			section.RecalcBoundingBox();
		}

		if (sections.Length > 0)
		{
			boundingBox = sections[0].BB;
		}


		foreach (ConvexSection section in sections)
		{
			boundingBox = boundingBox.Union(section.BB);
			section.ReconnectSections(this);
		}
	}

	public void ExtendBorders(BoundingBox levelBB)
	{
		foreach (ConvexSection section in sections)
		{
			section.MarkBorder(levelBB);
			boundingBox = boundingBox.Union(section.BB);
		}
	}
	
	public BoundingBox BB
	{
		get
		{
			return boundingBox;
		}
	}

	public void DebugDraw(Transform transform, bool showInteralEdges, Color edgeColor)
	{
		foreach (ConvexSection section in sections)
		{
			section.DebugDraw(transform, showInteralEdges, edgeColor);
		}
	}
	
	public bool OverlapCorrectionPass(OverlapShape shape)
	{
		if (!shape.BB.Overlaps(boundingBox))
		{
			return false;
		}

		int i = 0;

		for (i = 0; i < sections.Length; ++i)
		{
			if (sections[i].OverlapsConvex(shape))
			{
				break;
			}
		}

		if (i == sections.Length)
		{
			return false;
		}

		HashSet<ConvexSection> checkedShapes = new HashSet<ConvexSection>();
		SortedList<float, ConvexSection> sectionsToCheck = new SortedList<float, ConvexSection>();
		sectionsToCheck.Add(0, sections[i]);

		OverlapShape.Overlap currentOverlap = new OverlapShape.Overlap();
		currentOverlap.distance = float.MaxValue;

		while (sectionsToCheck.Count > 0 &&
		       sectionsToCheck.Keys[0] < currentOverlap.distance)
		{
			checkedShapes.Add(sectionsToCheck.Values[0]);

			OverlapShape.Overlap overlapCheck = sectionsToCheck.Values[0].Collide(shape, sectionsToCheck);

			if (overlapCheck.distance < currentOverlap.distance)
			{
				currentOverlap = overlapCheck;
			}

			while (sectionsToCheck.Count > 0 && checkedShapes.Contains(sectionsToCheck.Values[0]))
			{
				sectionsToCheck.RemoveAt(0);
			}
		}

		shape.MoveShape(currentOverlap.normal * (currentOverlap.distance + 0.001f));

		return true;
	}

	public void NextOutlinePoint(ref ConvexSection section, ref int index)
	{
		index = (index + 1) % section.PointCount;

		while (section.HasAdjacentSection(index))
		{
			int nextIndex = section.GetAdjacentSectionIndex(index);
			section = section.GetAdjacentSection(index);

			index = (nextIndex + 1) % section.PointCount;
		}
	}

	public ShapeOutline GetOutline()
	{
		List<Vector2> result = new List<Vector2>();

		ConvexSection startSection = sections[0];
		int startIndex = 0;

		NextOutlinePoint(ref startSection, ref startIndex);

		ConvexSection currentSection = startSection;
		int currentIndex = startIndex;

		do
		{
			result.Add(currentSection.GetPoint(currentIndex));
			NextOutlinePoint(ref currentSection, ref currentIndex);
		} while (startSection != currentSection || startIndex != currentIndex);

		return new ShapeOutline(result);
	}
	
	public void BuildShapes(List<LineListShape> output)
	{
		foreach (ConvexSection section in sections)
		{
			section.BuildShapes(output);
		}
	}
}