using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public enum VoxelSide
{
	Right,
	Left,
	Top,
	Bottom,
	Back,
	Front
}

public static class GameObjectHelper
{
	public static void DestroySafe(UnityEngine.Object deadObject)
	{
		if (Application.isPlaying)
		{
			GameObject.Destroy(deadObject);
		}
		else
		{
			GameObject.DestroyImmediate(deadObject);
		}
	}
}

public class VoxelFace
{
	public static readonly int FaceSideCount = 4;
	public static readonly int CornerCount = 4;

	private TileEdge[] edges = new TileEdge[FaceSideCount];
	private TileCorner[] corners = new TileCorner[CornerCount];
	private Tile tile;
	private Voxel parentVoxel;
	private VoxelSide side;
	
	private Transform parentTransform;
	
	private Quaternion rotation;
	private Vector3 center;

	private int replaceTileCount = 0;
	
	public static Quaternion SideSpin(TileSide side)
	{
		return Quaternion.AngleAxis(90.0f * (int)side, Vector3.up);
	}
	
	public Vector3 SideCenter(TileSide side)
	{
		return GetEdgeOrientation(side) * new Vector3(0.5f, 0.0f, 0.0f) + center;
	}

	public Vector3 CornerCenter(TileSide side)
	{
		return GetEdgeOrientation(side) * new Vector3(0.5f, 0.0f, 0.5f) + center;
	}
	
	public TileSide GetTileSide(Vector3 voxelMapPosition)
	{
		Vector3 offset = voxelMapPosition - center;
		return GetTileSideLocal(Quaternion.Inverse(rotation) * offset);
	}

	public static TileSide GetTileSideLocal(Vector3 localOffset)
	{
		if (Mathf.Abs(localOffset.z) > Mathf.Abs(localOffset.x))
		{
			return (localOffset.z > 0.0f) ? TileSide.Top : TileSide.Bottom;
		}
		else
		{
			return (localOffset.x > 0.0f) ? TileSide.Right : TileSide.Left;
		}
	}

	public TileSide GetCornerSide(Vector3 offset)
	{
		Vector3 faceSpaceDirection = (Quaternion.AngleAxis(45.0f, Vector3.up) * Quaternion.Inverse(GetEdgeOrientation(TileSide.Right))) * offset;
		return VoxelFace.GetTileSideLocal(faceSpaceDirection);
	}

	public VoxelFace GetConnectedFace(TileSide side, out EdgeAngle edgeAngle)
	{
		Quaternion sideRotation = GetEdgeOrientation(side);
		Vector3 sideCenter = SideCenter(side);

		// check the voxel over the edge from this side
		Vector3 cliffPosition = sideRotation * new Vector3(0.5f, -0.5f, 0.0f) + sideCenter;
		Voxel cliffVoxel = parentVoxel.Map.GetVoxel(cliffPosition);

		if (cliffVoxel == null || !cliffVoxel.IsSolid)
		{
			edgeAngle = EdgeAngle.CliffAngle;
			return parentVoxel.GetFace(sideRotation * new Vector3(1.0f, 0.0f, 0.0f));
		}

		Vector3 wallPosition = sideRotation * new Vector3(0.5f, 0.5f, 0.0f) + sideCenter;
		Voxel wallVoxel = parentVoxel.Map.GetVoxel(wallPosition);

		if (wallVoxel == null || !wallVoxel.IsSolid)
		{
			edgeAngle = EdgeAngle.FlatAngle;
			return cliffVoxel.GetFace(sideRotation * new Vector3(0.0f, 1.0f, 0.0f));
		}
		else
		{
			edgeAngle = EdgeAngle.WallAngle;
			return wallVoxel.GetFace(sideRotation * new Vector3(-1.0f, 0.0f, 0.0f));
		}
	}

	public TileCorner GetCorner(TileSide side)
	{
		return corners[(int)side];
	}

	public void SetTileCorner(TileSide side, TileCorner newValue)
	{
		if (corners[(int)side] != null)
		{
			// use callback to allow editor to add undo hooks
			ParentVoxel.Map.DestroyCallback(corners[(int)side].gameObject);
		}

		corners[(int)side] = newValue;

		if (newValue != null && tile != null)
		{
			newValue.transform.parent = tile.transform;
			Quaternion sideSpin = SideSpin(side);
			newValue.transform.localPosition = sideSpin * new Vector3(0.5f, 0.0f, 0.5f);
			newValue.transform.localRotation = sideSpin;
			newValue.transform.localScale = Vector3.one;
		}
	}

	public void ApplyCornerTransform(TileSide side, Transform target)
	{
		if (tile != null)
		{
			Quaternion sideSpin = SideSpin(side);
			target.position = tile.transform.TransformPoint(sideSpin * new Vector3(0.5f, 0.0f, 0.5f));
			target.rotation = tile.transform.rotation * sideSpin;
		}
	}

	public TileEdge GetEdge(TileSide side)
	{
		return edges[(int)side];
	}

	public Quaternion GetEdgeOrientation(TileSide side)
	{
		return rotation * SideSpin(side);
	}

	public void SetTileEdge(TileSide side, TileEdge newValue)
	{
		int sideIndex = (int)side;

		if (edges[sideIndex] != null)
		{
			// use callback to allow editor to add undo hooks
			ParentVoxel.Map.DestroyCallback(edges[sideIndex].gameObject);

			if (edges[sideIndex].TileA.ReplaceTile)
			{
				RemoveReplaceTile();
			}

			if (edges[sideIndex].TileB.ReplaceTile)
			{
				EdgeAngle edgeAngle;
				VoxelFace connectedFace = GetConnectedFace(side, out edgeAngle);
				
				if (connectedFace != null)
				{
					connectedFace.RemoveReplaceTile();
				}
			}
		}
		
		edges[(int)side] = newValue;
		
		if (newValue != null && tile != null)
		{
			newValue.transform.parent = tile.transform;
			Quaternion sideSpin = SideSpin(side);
			newValue.transform.localPosition = sideSpin * new Vector3(0.5f, 0.0f, 0.0f);
			newValue.transform.localRotation = sideSpin;
			newValue.transform.localScale = Vector3.one;

			if (newValue.TileA.ReplaceTile)
			{
				AddReplaceTile();
			}
			
			if (newValue.TileB.ReplaceTile)
			{
				EdgeAngle edgeAngle;
				VoxelFace connectedFace = GetConnectedFace(side, out edgeAngle);
				
				if (connectedFace != null)
				{
					connectedFace.AddReplaceTile();
				}
			}
		}
	}
	
	public VoxelFace(Voxel parentVoxel, VoxelSide side, Transform parentTransform)
	{
		this.parentVoxel = parentVoxel;
		this.side = side;
		this.parentTransform = parentTransform;
		
		rotation = Voxel.GetFaceOrientation(side);
		center = parentVoxel.Center + rotation * Vector3.up * 0.5f;
	}
	
	public void Destroy()
	{
		if (tile != null)
		{
			ParentVoxel.Map.DestroyCallback(tile.gameObject);
		}
		
		for (int i = 0; i < edges.Length; ++i)
		{
			if (edges[i] != null)
			{
				ParentVoxel.Map.DestroyCallback(edges[i].gameObject);

				edges[i] = null;
			}
		}
	}

	private void SetTileEnabled(bool value)
	{
		tile.enabled = value;
		
		MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
		
		if (renderer != null)
		{
			renderer.enabled = value;
		}

		MeshCollider meshCollider = tile.GetComponent<MeshCollider>();
		
		if (meshCollider != null)
		{
			meshCollider.enabled = value;
		}
	}

	public void AddReplaceTile()
	{
		++replaceTileCount;

		if (tile != null && tile.enabled)
		{
			SetTileEnabled(false);
		}
	}

	public void RemoveReplaceTile()
	{
		--replaceTileCount;

		if (replaceTileCount <= 0 && tile != null && !tile.enabled)
		{
			SetTileEnabled(true);
			replaceTileCount = 0;
		}
	}
	
	public Tile TileInstance
	{
		get
		{
			return tile;
		}
	}
	
	public string TileType
	{
		get
		{
			return tile == null ? null : tile.tileType;
		}
	}
	
	public Voxel ParentVoxel
	{
		get
		{
			return parentVoxel;
		}
	}
	
	public VoxelSide Side
	{
		get
		{
			return side;
		}
	}
	
	public Vector3 Center
	{
		get
		{
			return center;
		}
	}
	
	public void SetTileDirect(Tile newValue)
	{
		tile = newValue;

		if (tile != null)
		{
			TileEdge[] unboundEdges = newValue.GetComponentsInChildren<TileEdge>();

			foreach (TileEdge edge in unboundEdges)
			{
				Vector3 localPosition = tile.transform.InverseTransformPoint(edge.transform.position);
				edges[(int)GetTileSideLocal(localPosition)] = edge;
			}

			TileCorner[] unboundCorners = newValue.GetComponentsInChildren<TileCorner>();

			foreach (TileCorner corner in unboundCorners)
			{
				Vector3 localPosition = corner.transform.localPosition + corner.transform.localRotation * Vector3.right * 0.5f;
				corners[(int)GetTileSideLocal(localPosition)] = corner;
			}
		}
	}

	public void SetTile(string tileType)
	{
		SetTile(new TileDefinition(tileType));
	}
	
	public void SetTile(TileDefinition tileDef)
	{
		bool isVariableTile = !tileDef.IsNullTile && parentVoxel.Map.currentTileset.GetNumberOfType(tileDef.typeName) > 1;
		
		if (tile == null || tile.tileType != tileDef.typeName || isVariableTile)
		{
			if (tile != null)
			{
				Destroy();
			}
			
			if (tileDef.IsNullTile)
			{
				tile = null;
			}
			else
			{
				Tile tileTemplate = parentVoxel.Map.currentTileset.FindTile(tileDef, parentVoxel.Center, rotation);

				if (tileTemplate != null)
				{
					tile = (Tile)parentVoxel.Map.InstantiateCallback(tileTemplate);
					tile.transform.parent = parentTransform;
					tile.transform.localPosition = center;
					tile.transform.localRotation = rotation;
					tile.transform.localScale = Vector3.one;
				}
			}
		}
	}
	
	public static bool IsWall(VoxelSide side)
	{
		return side == VoxelSide.Right || side == VoxelSide.Left ||
			side == VoxelSide.Back || side == VoxelSide.Front;
	}
}
public class Voxel
{
	public static readonly int SideCount = 6;
	
	private bool isSolid = false;
	private bool isStatic = false;
	private PlaceableObject occupiedBy;
	private VoxelFace[] faces = new VoxelFace[SideCount];
	
	private VoxelMap map;
	private Vector3 localCenter;
	
	public Voxel(VoxelMap map, Vector3 localCenter, Transform parentTransform)
	{
		this.map = map;
		this.localCenter = localCenter;
		
		for (int i = 0; i < SideCount; ++i)
		{
			faces[i] = new VoxelFace(this, (VoxelSide)i, parentTransform);
		}
	}
	
	public int X
	{
		get
		{
			return Mathf.FloorToInt(localCenter.x);
		}
	}
	
	public int Y
	{
		get
		{
			return Mathf.FloorToInt(localCenter.y);
		}
	}
	
	public int Z
	{
		get
		{
			return Mathf.FloorToInt(localCenter.z);
		}
	}
	
	public VoxelMap Map
	{
		get
		{
			return map;
		}
	}
	
	public Vector3 Center
	{
		get
		{
			return localCenter;
		}
	}
	
	public bool IsSolid
	{
		get
		{
			return isSolid;
		}
		
		set
		{
			isSolid = value;
		}
	}

	public bool IsOccupied
	{
		get
		{
			return occupiedBy != null;
		}
	}

	public PlaceableObject OccupiedBy
	{
		get
		{
			return occupiedBy;
		}

		set
		{
			occupiedBy = value;
		}
	}

	public bool IsStatic
	{
		get
		{
			return isStatic;
		}
	}

	// faceindex
	// 0 = right or left face
	// 1 = top or bottom face
	// 2 = front or back face
	public VoxelFace GetFaceOnCorner(Vector3 cornerDirection, int faceIndex)
	{
		Vector3 faceDirection = Vector3.up;

		switch (faceIndex)
		{
		case 0:
			faceDirection = new Vector3(cornerDirection.x, 0.0f, 0.0f);
			break;
		case 1:
			faceDirection = new Vector3(0.0f, cornerDirection.y, 0.0f);
			break;
		case 2:
			faceDirection = new Vector3(0.0f, 0.0f, cornerDirection.z);
			break;
		}

		return GetFace(faceDirection);
	}

	public VoxelFace GetFilledFaceOnCorner(Vector3 cornerDirection)
	{
		for (int i = 0; i < 3; ++i)
		{
			VoxelFace currentFace = GetFaceOnCorner(cornerDirection, i);

			if (currentFace.TileInstance != null)
			{
				return currentFace;
			}
		}
	
		return null;
	}
	
	public static VoxelSide GetSide(Vector3 direction)
	{
		if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) &&
		    Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
		{
			return direction.x > 0 ? VoxelSide.Right : VoxelSide.Left;
		}
		else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
		{
			return direction.y > 0 ? VoxelSide.Top : VoxelSide.Bottom;
		}
		else
		{
			return direction.z > 0 ? VoxelSide.Back : VoxelSide.Front;
		}
	}
	
	public static VoxelSide GetOpposite(VoxelSide side)
	{
		switch (side)
		{
		case VoxelSide.Right:
			return VoxelSide.Left;
		case VoxelSide.Left:
			return VoxelSide.Right;
		case VoxelSide.Top:
			return VoxelSide.Bottom;
		case VoxelSide.Bottom:
			return VoxelSide.Top;
		case VoxelSide.Back:
			return VoxelSide.Front;
		case VoxelSide.Front:
			return VoxelSide.Back;
		default:
			return VoxelSide.Left;
		}
	}
	
	public static Quaternion GetFaceOrientation(VoxelSide side)
	{
		switch (side)
		{
		case VoxelSide.Right:
			return Quaternion.AngleAxis(-90.0f, Vector3.up) * Quaternion.AngleAxis(-90.0f, Vector3.right);
		case VoxelSide.Left:
			return Quaternion.AngleAxis(90.0f, Vector3.up) * Quaternion.AngleAxis(-90.0f, Vector3.right);
		case VoxelSide.Top:
			return Quaternion.identity;
		case VoxelSide.Bottom:
			return Quaternion.AngleAxis(180.0f, Vector3.forward);
		case VoxelSide.Back:
			return Quaternion.AngleAxis(180.0f, Vector3.up) * Quaternion.AngleAxis(-90.0f, Vector3.right);
		case VoxelSide.Front:
			return Quaternion.AngleAxis(-90.0f, Vector3.right);
		default:
			return Quaternion.identity;
		}
	}
	
	public static Vector3 GetSideDirection(VoxelSide side)
	{
		return GetFaceOrientation(side) * Vector3.up;
	}
	
	public System.Collections.Generic.IEnumerable<VoxelFace> Faces
	{
		get
		{
			return faces;
		}
	}
	
	public void Destroy()
	{
		foreach (VoxelFace face in Faces)
		{
			if (face != null)
			{
				face.Destroy();
			}
		}
	}
	
	public VoxelFace GetFace(Vector3 direction)
	{
		return GetFace(GetSide(direction));
	}
	
	public VoxelFace GetFace(VoxelSide side)
	{
		return faces[(int)side];
	}

	public void AppendAdjacentVoxel(HashSet<Voxel> target)
	{
		foreach (VoxelFace face in faces)
		{
			foreach (TileSide side in (TileSide[])Enum.GetValues(typeof(TileSide)))
			{
				EdgeAngle edgeAngle;
				VoxelFace adjacentFace = face.GetConnectedFace(side, out edgeAngle);

				if (adjacentFace != null && adjacentFace.TileInstance != null)
				{
					target.Add(adjacentFace.ParentVoxel);
				}
			}
		}
	}
	
	public void SetTile(VoxelSide side, string tileType)
	{
		faces[(int)side].SetTile(tileType);
		
		if (tileType != null)
		{
			isSolid = true;
		}
	}

	public void SetTile(VoxelSide side, TileDefinition tileDef)
	{
		faces[(int)side].SetTile(tileDef);

		if (!tileDef.IsNullTile)
		{
			isSolid = true;
		}
	}
}