using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ConcaveColliderGroup : ISerializationCallbackReceiver {
	[SerializeField]
	private ConcaveCollider[] colliders;
	private bool isSetup = false;
	
	private static readonly int maxIterations = 10;
	
	public ConcaveColliderGroup(ConcaveCollider[] colliders)
	{
		this.colliders = colliders;
		ExtendBorders();
	}
	
	public void DebugDraw(Transform transform, bool showInteralEdges, Color edgeColor)
	{
		foreach (ConcaveCollider collider in colliders)
		{
			collider.DebugDraw(transform, showInteralEdges, edgeColor);
		}
	}
	
	private bool OverlapCorrectionPass(OverlapShape shape)
	{
		bool result = false;
		
		foreach (ConcaveCollider collider in colliders)
		{
			result = collider.OverlapCorrectionPass(shape) || result;
		}
		
		return result;
	}
	
	public void OverlapCorrection(OverlapShape shape)
	{
		int i = 0;
		
		while (OverlapCorrectionPass(shape) && i < maxIterations)
			++i;
		
		if (i == maxIterations)
		{
			Debug.LogWarning("Could not correct overlap");
		}
	}
	
	public ConcaveCollider GetCollider(int index)
	{
		return colliders[index];
	}
	
	public int ColliderCount
	{
		get
		{
			return colliders.Length;
		}
	}
	
	public void OnBeforeSerialize()
	{
		
	}
	
	public void OnAfterDeserialize()
	{
		if (!isSetup)
		{
			ReconnectSections();
			ExtendBorders();
		}
	}

	public void ReconnectSections()
	{
		foreach (ConcaveCollider collider in colliders)
		{
			collider.ReconnectSections();
		}
	}

	public void ExtendBorders()
	{
		if (colliders.Length > 0)
		{
			BoundingBox bb = colliders[0].BB;

			foreach (ConcaveCollider collider in colliders)
			{
				bb = bb.Union(collider.BB);
			}

			foreach (ConcaveCollider collider in colliders)
			{
				collider.ExtendBorders(bb);
			}
		}

		isSetup = true;
	}
	
	public List<ShapeOutline> GetOutline()
	{
		return colliders.Select(collider => collider.GetOutline()).ToList();
	}
}