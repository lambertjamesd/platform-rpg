using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ConcaveColliderGroup {
	[SerializeField]
	private ConcaveCollider[] colliders;
	
	private static readonly int maxIterations = 10;
	
	public ConcaveColliderGroup(ConcaveCollider[] colliders)
	{
		this.colliders = colliders;
	}
	
	public void EnsureInitialized()
	{
		foreach (ConcaveCollider collider in colliders)
		{
			collider.EnsureInitialized();
		}
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
	
	public List<ShapeOutline> GetOutline()
	{
		return colliders.Select(collider => collider.GetOutline()).ToList();
	}
}