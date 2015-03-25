using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MeshBaking
{
	private static void PopulateStaticTiles(VoxelMap map, List<MeshRenderer> meshRenderer, List<MeshCollider> meshCollider)
	{
		Tile[] tiles = map.GetStaticTransform().GetComponentsInChildren<Tile>();

		foreach (Tile tile in tiles)
		{
			if (tile.isStatic)
			{
				meshRenderer.AddRange(tile.GetComponentsInChildren<MeshRenderer>());
				meshCollider.AddRange(tile.GetComponentsInChildren<MeshCollider>());
			}
		}
	}

	private static void GetStaticMeshes(VoxelMap voxelMap, List<MeshRenderer> meshRenderer, List<MeshCollider> meshCollider)
	{
		PopulateStaticTiles(voxelMap, meshRenderer, meshCollider);
	}

	private static bool IsCeilingTile(Transform transform)
	{
		return Vector3.Dot(Vector3.down, transform.TransformDirection(Vector3.up)) > 0.5f;
	}

	private static void SortInstance(List<CombineInstance> instances)
	{
		instances.Sort((CombineInstance a, CombineInstance b) => {
			Vector4 aPosition = a.transform.GetColumn(3);
			Vector4 bPosition = b.transform.GetColumn(3);

			aPosition.x = Mathf.Floor(aPosition.x / 10.0f);
			aPosition.y = Mathf.Floor(aPosition.y / 10.0f);
			aPosition.z = Mathf.Floor(aPosition.z / 10.0f);

			bPosition.x = Mathf.Floor(bPosition.x / 10.0f);
			bPosition.y = Mathf.Floor(bPosition.y / 10.0f);
			bPosition.z = Mathf.Floor(bPosition.z / 10.0f);

			if (aPosition.x != bPosition.x)
			{
				return aPosition.x.CompareTo(bPosition.x);
			}
			else if (aPosition.z != bPosition.z)
			{
				return aPosition.z.CompareTo(bPosition.z);
			}
			else
			{
				return aPosition.y.CompareTo(bPosition.y);
			}
		});
	}

	public static void BakeStaticMeshes(VoxelMap voxelMap, bool bakeCielingTiles, bool simplifyColliderMesh, bool simplifyVisualMesh)
	{
		List<MeshRenderer> renderers = new List<MeshRenderer>();
		List<MeshCollider> colliderMeshes = new List<MeshCollider>();
		GetStaticMeshes(voxelMap, renderers, colliderMeshes);
		List<MeshFilter> filters = new List<MeshFilter>(renderers.Count);
		HashSet<Material> usedMaterials = new HashSet<Material>();
		HashSet<PhysicMaterial> usedPhysicsMaterials = new HashSet<PhysicMaterial>();

		for (int i = 0; i < renderers.Count; ++i)
		{
			filters.Add(renderers[i].GetComponent<MeshFilter>());

			foreach (Material material in renderers[i].sharedMaterials)
			{
				if (material != null)
				{
					usedMaterials.Add(material);
				}
			}
		}

		Transform bakedTransform = voxelMap.GetBakedTransform();

		foreach (Material material in usedMaterials)
		{
			List<CombineInstance> combineInstances = new List<CombineInstance>();
			
			Matrix4x4 relativeMatrix = voxelMap.transform.worldToLocalMatrix;

			for (int i = 0; i < renderers.Count; ++i)
			{
				if (bakeCielingTiles || !IsCeilingTile(renderers[i].transform) && filters[i].sharedMesh != null && renderers[i].enabled)
				{
					Material[] meshMaterials = renderers[i].sharedMaterials;

					Matrix4x4 fullTransform = relativeMatrix * renderers[i].transform.localToWorldMatrix;

					for (int matIndex = 0; matIndex < meshMaterials.Length; ++matIndex)
					{
						if (meshMaterials[matIndex] == material)
						{
							CombineInstance combineInstance = new CombineInstance();
							combineInstance.mesh = filters[i].sharedMesh;
							combineInstance.subMeshIndex = matIndex;
							combineInstance.transform = fullTransform;
							combineInstances.Add(combineInstance);
						}
					}
				}
			}

			SortInstance(combineInstances);
			
			while (combineInstances.Count > 0)
			{
				GameObject batch = new GameObject(material.name);
				batch.transform.parent = bakedTransform;
				batch.transform.localPosition = Vector3.zero;
				batch.transform.localScale = Vector3.one;
				batch.transform.localRotation = Quaternion.identity;

				MeshFilter batchFilter = batch.AddComponent<MeshFilter>();

				int instaceUseCount = MeshTools.GetMaxInstanceCount(combineInstances);
				List<CombineInstance> subarray = combineInstances.GetRange(0, Mathf.Min(combineInstances.Count, instaceUseCount));

				Mesh combinedMesh = new Mesh();
				combinedMesh.name = material.name;
				combinedMesh.CombineMeshes(subarray.ToArray());
				
				if (simplifyVisualMesh)
				{
					Mesh simplifiedMesh = combinedMesh.Simplify(false);
					GameObject.DestroyImmediate(combinedMesh);
					combinedMesh = simplifiedMesh;
				}

				combinedMesh.Optimize();
				combinedMesh.RecalculateBounds();

				batchFilter.sharedMesh = combinedMesh;

				MeshRenderer renderer = batch.AddComponent<MeshRenderer>();
				renderer.sharedMaterials = new Material[]{material};
				
				combineInstances.RemoveRange(0, subarray.Count);
			}
		}
		
		for (int i = 0; i < renderers.Count; ++i)
		{
			GameObjectHelper.DestroySafe(renderers[i]);
			GameObjectHelper.DestroySafe(filters[i]);
		}

		for (int i = 0; i < colliderMeshes.Count; ++i)
		{
			usedPhysicsMaterials.Add(colliderMeshes[i].sharedMaterial);
		}

		foreach (PhysicMaterial material in usedPhysicsMaterials)
		{
			List<CombineInstance> combineInstances = new List<CombineInstance>();
			Matrix4x4 relativeMatrix = voxelMap.transform.worldToLocalMatrix;

			for (int i = 0; i < colliderMeshes.Count; ++i)
			{
				MeshCollider colliderMesh = colliderMeshes[i];

				if (colliderMesh.sharedMaterial == material && (bakeCielingTiles || !IsCeilingTile(colliderMesh.transform)) && colliderMesh.enabled)
				{
					Mesh mesh = colliderMesh.sharedMesh;
					Matrix4x4 fullTransform = relativeMatrix * colliderMesh.transform.localToWorldMatrix;

					for (int subMeshIndex = 0; mesh != null && subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
					{
						CombineInstance instance = new CombineInstance();
						instance.mesh = mesh;
						instance.subMeshIndex = subMeshIndex;
						instance.transform = fullTransform;
						combineInstances.Add(instance);
					}
				}
			}
			
			SortInstance(combineInstances);

			while (combineInstances.Count > 0)
			{
				string materialName = material == null ? "Default Physics" : material.name;
				GameObject batch = new GameObject(materialName);
				batch.transform.parent = bakedTransform;
				batch.transform.localPosition = Vector3.zero;
				batch.transform.localScale = Vector3.one;
				batch.transform.localRotation = Quaternion.identity;
				
				MeshCollider batchCollider = batch.AddComponent<MeshCollider>();

				int instaceUseCount = MeshTools.GetMaxInstanceCount(combineInstances);
				List<CombineInstance> subarray = combineInstances.GetRange(0, Mathf.Min(combineInstances.Count, instaceUseCount));
				
				Mesh combinedMesh = new Mesh();
				combinedMesh.name = materialName;
				combinedMesh.CombineMeshes(subarray.ToArray());
				
				if (simplifyColliderMesh)
				{
					Mesh simplifiedMesh = combinedMesh.Simplify(true);
					GameObject.DestroyImmediate(combinedMesh);
					combinedMesh = simplifiedMesh;
					combinedMesh.RecalculateNormals();
					combinedMesh.AddEmptyTextureCoordinates();
				}

				combinedMesh.Optimize();
				combinedMesh.RecalculateBounds();

				batchCollider.sharedMesh = combinedMesh;
				batchCollider.sharedMaterial = material;

				combineInstances.RemoveRange(0, subarray.Count);
			}
		}

		for (int i = 0; i < colliderMeshes.Count; ++i)
		{
			GameObjectHelper.DestroySafe(colliderMeshes[i]);
		}

		foreach (TileCorner corner in voxelMap.GetStaticTransform().GetComponentsInChildren<TileCorner>())
		{
			GameObjectHelper.DestroySafe(corner);
		}
		
		foreach (TileEdge edge in voxelMap.GetStaticTransform().GetComponentsInChildren<TileEdge>())
		{
			GameObjectHelper.DestroySafe(edge);
		}
	
		foreach (Tile tile in voxelMap.GetStaticTransform().GetComponentsInChildren<Tile>())
		{
			GameObject tileGameObject = tile.gameObject;

			if (tile.isStatic)
			{
				GameObjectHelper.DestroySafe(tile);

				// if there is only a transform, delete the game object
				if (IsTileEmpty(tileGameObject))
				{
					GameObjectHelper.DestroySafe(tileGameObject);
				}
			}
		}
	}

	private static bool IsTileEmpty(GameObject gameObject)
	{
		foreach (Component component in gameObject.GetComponents<Component>())
		{
			if (!(component is Transform || component is TileEdge))
			{
				return false;
			}
		}

		for (int i = 0; i < gameObject.transform.childCount; ++i)
		{
			if (!IsTileEmpty(gameObject.transform.GetChild(i).gameObject))
			{
				return false;
			}
		}

		return true;
	}
}
