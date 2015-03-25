using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public struct VoxelRaycastCastHit
{
	private Voxel voxel;
	private VoxelSide side;
	private Vector3 position;
	private Vector3 normal;

	public VoxelRaycastCastHit(Voxel voxel, VoxelSide side, Vector3 position, Vector3 normal)
	{
		this.voxel = voxel;
		this.side = side;
		this.position = position;
		this.normal = normal;
	}

	public Voxel Voxel
	{
		get { return voxel; }
	}

	public VoxelSide Side
	{
		get { return side; }
	}

	public VoxelFace Face
	{
		get { return voxel == null ? null : voxel.GetFace(side); }
	}

	public Vector3 Position
	{
		get { return position; }
	}
	
	public Vector3 Normal
	{
		get { return normal; }
	}
}

public class VoxelMap : MonoBehaviour
{
	public delegate UnityEngine.Object InstantiateCallbackDelegate(UnityEngine.Object template);
	public delegate void DestoryCallbackDelegate(UnityEngine.Object deadObject);

	private Voxel[] voxels;

	public Tileset currentTileset;
	
	[SerializeField]
	private int width = 0;
	[SerializeField]
	private int height = 0;
	[SerializeField]
	private int depth = 0;
	
	[SerializeField]
	private UnityEngine.Object prefabSaveTarget = null;
	
	[SerializeField]
	private bool isBaked = false;
	[SerializeField]
	private string sourceScene;

	[SerializeField]
	private bool bakeCeilings = false;
	[SerializeField]
	private bool simplifyColliderMesh = true;
	[SerializeField]
	private bool simplifyVisualMesh = false;

	public int Width{get{return width;}}
	public int Height{get{return height;}}
	public int Depth{get{return depth;}}

	private static readonly string StaticObjectName = "Static";
	private static readonly string DynamicObjectName = "Dynamic";
	private static readonly string BakedObjectName = "Baked";

	private Transform staticTransform;
	private Transform dynamicTransform;
	private Transform bakedTransform;

	private InstantiateCallbackDelegate instantiateCallback;

	public UnityEngine.Object PrefabSaveTarget
	{
		get
		{
			return prefabSaveTarget;
		}

		set
		{
			prefabSaveTarget = value;
		}
	}

	public InstantiateCallbackDelegate InstantiateCallback
	{
		get
		{
			if (instantiateCallback != null)
			{
				return instantiateCallback;
			}
			else
			{
#if UNITY_EDITOR
				return PrefabUtility.InstantiatePrefab;
#else
				return GameObject.Instantiate;
#endif
			}
		}

		set
		{
			instantiateCallback = value;
		}
	}
	
	private DestoryCallbackDelegate destroyCallback;

	public DestoryCallbackDelegate DestroyCallback
	{
		get
		{
			if (destroyCallback != null)
			{
				return destroyCallback;
			}
			else
			{
				return GameObjectHelper.DestroySafe;
			}
		}

		set
		{
			destroyCallback = value;
		}
	}

	public void BakeStaticGeometry(string sourceScene)
	{
		MeshBaking.BakeStaticMeshes(this, bakeCeilings, simplifyColliderMesh, simplifyVisualMesh);
		this.sourceScene = sourceScene;
		isBaked = true;
	}

	public void Start()
	{
#if UNITY_EDITOR
		if (prefabSaveTarget != null && !isBaked)
		{
			string prefabPath = AssetDatabase.GetAssetPath(prefabSaveTarget);
			Instantiate(AssetDatabase.LoadAssetAtPath(prefabPath, typeof(VoxelMap)), transform.position, transform.rotation);
			Destroy(gameObject);
		}
		else
		{
			CheckRebuildVoxels();
		}
#else
			CheckRebuildVoxels();		
#endif
	}

	public bool IsBaked
	{
		get
		{
			return isBaked;
		}
	}

	public bool SimplifyColliderMesh
	{
		get
		{
			return simplifyColliderMesh;
		}
	}
	
	public bool SimplifyVisualMesh
	{
		get
		{
			return simplifyVisualMesh;
		}
	}

	public string SourceScene
	{
		get
		{
			return sourceScene;
		}
	}

	public Transform GetStaticTransform()
	{
		if (staticTransform == null)
		{
			staticTransform = transform.FindChild(StaticObjectName);

			if (staticTransform == null)
			{
				GameObject newGameObject = new GameObject(StaticObjectName);
				staticTransform = newGameObject.transform;
				staticTransform.parent = transform;
				staticTransform.localPosition = Vector3.zero;
				staticTransform.localScale = Vector3.one;
				staticTransform.localRotation = Quaternion.identity;
			}
		}

		return staticTransform;
	}
	
	public Transform GetDynamicTransform()
	{
		if (dynamicTransform == null)
		{
			dynamicTransform = transform.FindChild(DynamicObjectName);
			
			if (dynamicTransform == null)
			{
				GameObject newGameObject = new GameObject(DynamicObjectName);
				dynamicTransform = newGameObject.transform;
				dynamicTransform.parent = transform;
				dynamicTransform.localPosition = Vector3.zero;
				dynamicTransform.localScale = Vector3.one;
				dynamicTransform.localRotation = Quaternion.identity;
				dynamicTransform.tag = "DynamicMap";
			}
		}
		
		return dynamicTransform;
	}
	
	public Transform GetBakedTransform()
	{
		if (bakedTransform == null)
		{
			bakedTransform = transform.FindChild(BakedObjectName);
			
			if (bakedTransform == null)
			{
				GameObject newGameObject = new GameObject(BakedObjectName);
				bakedTransform = newGameObject.transform;
				bakedTransform.parent = transform;
				bakedTransform.localPosition = Vector3.zero;
				bakedTransform.localScale = Vector3.one;
				bakedTransform.localRotation = Quaternion.identity;
			}
		}
		
		return bakedTransform;
	}
	
	private static bool RaycastPlane(Ray ray, Plane plane, out Vector3 position)
	{
		float distance;
		if (plane.Raycast(ray, out distance))
		{
			position = ray.GetPoint(distance);
			return true;
		}
		else
		{
			position = Vector3.zero;
			return false;
		}
	}

	// raycasts collides against the back of the boundary
	public bool LocalRaycastWithBoundary(Ray localRay, out Vector3 position, out Vector3 normal)
	{
		Vector3 signDirection = new Vector3(Mathf.Sign(localRay.direction.x), Mathf.Sign(localRay.direction.y), Mathf.Sign(localRay.direction.z));
		
		Plane[] planes = new Plane[3];

		planes[0] = new Plane(new Vector3(-signDirection.x, 0.0f, 0.0f), width * (signDirection.x + 1.0f) * 0.5f);
		planes[1] = new Plane(new Vector3(0.0f, -signDirection.y, 0.0f), height * (signDirection.y + 1.0f) * 0.5f);
		planes[2] = new Plane(new Vector3(0.0f, 0.0f, -signDirection.z), depth * (signDirection.z + 1.0f) * 0.5f);

		foreach (Plane plane in planes)
		{
			Vector3 raycastHit;

			if (RaycastPlane(localRay, plane, out raycastHit))
			{
				if (GetVoxel(raycastHit + plane.normal * 0.5f) != null)
				{
					position = raycastHit;
					normal = plane.normal;
					return true;
				}
			}
		}

		position = Vector3.zero;
		normal = Vector3.zero;
		return false;
	}

	private struct PlaneDistance
	{
		public Plane plane;
		public float distance;

		private Ray ray;
		private float distanceStep;

		public PlaneDistance(Plane plane, Ray ray, float distanceStep)
		{
			this.plane = plane;
			this.ray = ray;
			this.distanceStep = distanceStep;

			if (!plane.Raycast(ray, out distance))
			{
				distance = float.PositiveInfinity;
			}
		}

		public void Step(float distance)
		{
			plane.distance -= distanceStep * distance;
			this.distance += distance / Vector3.Dot(ray.direction, plane.normal);
		}
	}

	public delegate bool IsSolidVoxelCallback(Voxel voxel);

	public bool LocalRaycast(Ray localRay, out VoxelRaycastCastHit hit, IsSolidVoxelCallback solidVoxelCallback = null)
	{
		solidVoxelCallback = solidVoxelCallback ?? (voxel => voxel.IsSolid);

		CheckRebuildVoxels();

		Vector3 boundaryHit;
		Vector3 boundaryNormal;

		if (LocalRaycastWithBoundary(localRay, out boundaryHit, out boundaryNormal))
		{
			Vector3 signDirection = new Vector3(Mathf.Sign(localRay.direction.x), Mathf.Sign(localRay.direction.y), Mathf.Sign(localRay.direction.z));

			float xDistance = Mathf.Floor(localRay.origin.x) + (1.0f + signDirection.x) * 0.5f;
			float yDistance = Mathf.Floor(localRay.origin.y) + (1.0f + signDirection.y) * 0.5f;
			float zDistance = Mathf.Floor(localRay.origin.z) + (1.0f + signDirection.z) * 0.5f;

			xDistance = Mathf.Clamp(xDistance, 0.0f, width);
			yDistance = Mathf.Clamp(yDistance, 0.0f, height);
			zDistance = Mathf.Clamp(zDistance, 0.0f, depth);

			PlaneDistance[] planes = new PlaneDistance[3];

			planes[0] = new PlaneDistance(new Plane(new Vector3(signDirection.x, 0.0f, 0.0f), xDistance * -signDirection.x), localRay, signDirection.x);
			planes[1] = new PlaneDistance(new Plane(new Vector3(0.0f, signDirection.y, 0.0f), yDistance * -signDirection.y), localRay, signDirection.y);
			planes[2] = new PlaneDistance(new Plane(new Vector3(0.0f, 0.0f, signDirection.z), zDistance * -signDirection.z), localRay, signDirection.z);

			bool insideArea = true;

			float boundaryDistance = Vector3.Dot(boundaryHit - localRay.origin, localRay.direction);

			while (insideArea)
			{
				System.Array.Sort<PlaneDistance>(planes, (PlaneDistance a, PlaneDistance b) => {
					return a.distance.CompareTo(b.distance);
				});

				if (planes[0].distance > boundaryDistance)
				{
					insideArea = false;
				}
				else
				{
					Vector3 collision = localRay.GetPoint(planes[0].distance);

					Voxel voxel = GetVoxel(collision + planes[0].plane.normal * 0.5f);

					if (voxel != null && solidVoxelCallback(voxel))
					{
						Vector3 normal = -planes[0].plane.normal;
						hit = new VoxelRaycastCastHit(voxel, Voxel.GetSide(normal), collision, normal);
						return true;
					}

					planes[0].Step(1.0f);
				}
			}


			hit = new VoxelRaycastCastHit();
			return false;
		}
		else
		{
			hit = new VoxelRaycastCastHit();
			return false;
		}

	}
	
	private static Voxel GetVoxel(Voxel[] voxelData, int width, int height, int depth, int x, int y, int z)
	{
		if (x < 0 || y < 0 || z < 0 ||
		    x >= width || y >= height || z >= depth)
		{
			return null;
		}
		else
		{
			return voxelData[z + (y + x * height) * depth];
		}
	}

	private static void SetVoxel(Voxel[] voxelData, int width, int height, int depth, int x, int y, int z, Voxel value)
	{
		if (x >= 0 && y >= 0 && z >= 0 &&
		    x < width && y < height && z < depth)
		{
			voxelData[z + (y + x * height) * depth] = value;
		}
	}

	public Voxel GetVoxel(int x, int y, int z)
	{
		CheckRebuildVoxels();
		return GetVoxel(voxels, width, height, depth, x, y, z);
	}

	private void SetVoxel(int x, int y, int z, Voxel value)
	{
		SetVoxel(voxels, width, height, depth, x, y, z, value);
	}

	public Voxel GetVoxel(Vector3 localPosition)
	{
		return GetVoxel(Mathf.FloorToInt(localPosition.x), Mathf.FloorToInt(localPosition.y), Mathf.FloorToInt(localPosition.z));
	}

	public bool IsEmpty(int x, int y, int z, int width, int height, int depth)
	{
		for (int checkX = x; checkX < x + width; ++checkX)
		{
			for (int checkY = y; checkY < y + height; ++checkY)
			{
				for (int checkZ = z; checkZ < z + depth; ++checkZ)
				{
					Voxel voxel = GetVoxel(checkX, checkY, checkZ);

					if (voxel != null && (voxel.IsSolid || voxel.IsOccupied))
					{
						return false;
					}
				}
			}
		}

		return true;
	}

	public bool IsEmpty(PlaceableObject occupyWith)
	{
		Vector3 position = occupyWith.MinCorner;
		Vector3 size = occupyWith.RotatedSize;

		return IsEmpty((int)position.x, (int)position.y, (int)position.z, (int)size.x, (int)size.y, (int)size.z);
	}

	public void SetOccupySpace(PlaceableObject occupyWith, bool value)
	{
		Vector3 size = occupyWith.RotatedSize;
		Vector3 startPos = occupyWith.MinCorner;

		for (int checkX = (int)startPos.x; checkX < (int)(startPos.x + size.x); ++checkX)
		{
			for (int checkY = (int)startPos.y; checkY < (int)(startPos.y + size.y); ++checkY)
			{
				for (int checkZ = (int)startPos.z; checkZ < (int)(startPos.z + size.z); ++checkZ)
				{
					Voxel voxel = GetVoxel (checkX, checkY, checkZ);

					if (voxel != null)
					{
						voxel.OccupiedBy = value ? occupyWith : null;
					}
				}
			}
		}
	}

	public void AssignTileCorners(List<VoxelFace> faces, EdgeAngle[] edgeAngles, TileSide[] tileRotations)
	{
		string[] tileTypes = new string[faces.Count];

		for (int i = 0; i < tileTypes.Length; ++i)
		{
			tileTypes[i] = faces[i].TileInstance.tileType;
		}

		for (int i = 0; i < faces.Count; ++i)
		{
			TileCorner corner = currentTileset.FindCorner(edgeAngles, tileTypes, tileRotations, i);

			if (corner != null)
			{
				corner = (TileCorner)instantiateCallback(corner);
			}

			faces[i].SetTileCorner(tileRotations[i], corner);
		}
	}
	
	public bool GetCornerTiles(VoxelFace startFace, Vector3 worldCornerPos, out List<VoxelFace> faces, out List<TileSide> tileSides, out List<EdgeAngle> edgeAngles)
	{
		VoxelFace currentFace = startFace;
		TileSide edgeStep = startFace.GetCornerSide(worldCornerPos - startFace.Center);
		
		faces = new List<VoxelFace>();
		tileSides = new List<TileSide>();
		edgeAngles = new List<EdgeAngle>();
		
		do
		{
			EdgeAngle edgeAngle;
			VoxelFace nextFace = currentFace.GetConnectedFace(edgeStep, out edgeAngle);
			
			if (nextFace.TileInstance == null)
			{
				nextFace = null;
			}
			
			if (nextFace != null)
			{
				faces.Add(currentFace);
				tileSides.Add(edgeStep);
				edgeAngles.Add(edgeAngle);
				
				edgeStep = nextFace.GetCornerSide(worldCornerPos - nextFace.ParentVoxel.Center);
				currentFace = nextFace;
			}
			else
			{
				faces = null;
				tileSides = null;
				edgeAngles = null;
				
				return false;
			}
		} while (currentFace != null && currentFace != startFace);
		
		return true;
	}

	public bool GetCornerTiles(Voxel voxel, Vector3 cornerDirection, out List<VoxelFace> faces, out List<TileSide> tileSides, out List<EdgeAngle> edgeAngles)
	{
		VoxelFace startFace = voxel.GetFilledFaceOnCorner(cornerDirection);
		
		if (startFace != null)
		{
			Vector3 worldCornerPos = cornerDirection + voxel.Center;
			return GetCornerTiles(startFace, worldCornerPos, out faces, out tileSides, out edgeAngles);
		}
		else
		{
			faces = null;
			tileSides = null;
			edgeAngles = null;

			return false;
		}
	}

	public void ResolveVoxelCorner(Voxel voxel, Vector3 cornerDirection)
	{
		List<VoxelFace> faces;
		List<TileSide> tileSides;
		List<EdgeAngle> edgeAngles;

		if (GetCornerTiles(voxel, cornerDirection, out faces, out tileSides, out edgeAngles))
		{
			AssignTileCorners(faces, edgeAngles.ToArray(), tileSides.ToArray());
		}
		else
		{
			// default behavior is to clear the corner
			for (int i = 0; i < 3; ++i)
			{
				VoxelFace face = voxel.GetFaceOnCorner(cornerDirection, i);
				TileSide side = face.GetCornerSide(cornerDirection);
				face.SetTileCorner(side, null);
			}
		}
	}
	
	public void ResolveVoxelEdges(VoxelFace voxelFace, Vector3 tileOrigin)
	{
		if (voxelFace.TileInstance != null)
		{
			for (int i = 0; i < VoxelFace.FaceSideCount; ++i)
			{
				TileSide thisSide = (TileSide)i;
				EdgeAngle edgeAngle;
				VoxelFace adjacentFace = voxelFace.GetConnectedFace(thisSide, out edgeAngle);

				if (adjacentFace.TileInstance != null)
				{
					TileSide otherSide = adjacentFace.GetTileSide(voxelFace.SideCenter(thisSide));

					TileDefinition tileDef = voxelFace.TileInstance.GetTileDefinition();

					Vector3 edgeTileOrigin = tileDef.groupName == null ? tileOrigin : tileDef.groupOrigin;

					Vector3 tileOffset = voxelFace.ParentVoxel.Center - Vector3.one * 0.5f - edgeTileOrigin;
					Vector3 localOffset = Quaternion.Inverse(voxelFace.GetEdgeOrientation(TileSide.Right)) * tileOffset;

					int toOtherGroupOffset = Mathf.FloorToInt(((thisSide == TileSide.Right || thisSide == TileSide.Left) ? localOffset.z : localOffset.x) + 0.5f);
					TileEdge toOtherTemplate = currentTileset.RandomTileEdge(voxelFace.TileInstance.tileType, thisSide, adjacentFace.TileInstance.tileType, otherSide, edgeAngle, toOtherGroupOffset);

					TileEdge toOther = toOtherTemplate == null ? null : (TileEdge)voxelFace.ParentVoxel.Map.InstantiateCallback(toOtherTemplate);
					voxelFace.SetTileEdge(thisSide, toOther);
				}
			}
		}
	}

	private void ResolveVoxelSide(Voxel voxel, VoxelSide side, Voxel adjacentVoxel, TileDefinition fillType)
	{
		bool adjacentSideSolid = adjacentVoxel != null && adjacentVoxel.IsSolid;

		if (voxel.IsSolid == adjacentSideSolid)
		{
			voxel.SetTile(side, null);

			if (adjacentVoxel != null)
			{
				adjacentVoxel.SetTile(Voxel.GetOpposite(side), null);
			}
		}
		else if (voxel.IsSolid)
		{
			if (voxel.GetFace(side).TileInstance == null)
			{
				voxel.SetTile(side, fillType);
			}

			ResolveVoxelEdges(voxel.GetFace(side), fillType.groupOrigin);
		}
		else if (adjacentSideSolid)
		{
			if (adjacentVoxel.GetFace(Voxel.GetOpposite(side)).TileInstance == null)
			{
				adjacentVoxel.SetTile(Voxel.GetOpposite(side), fillType);
			}
			
			ResolveVoxelEdges(adjacentVoxel.GetFace(side), fillType.groupOrigin);
		}
	}

	public bool IsSolid(Vector3 position)
	{
		Voxel voxelAtPosition = GetVoxel(position);
		return voxelAtPosition != null && voxelAtPosition.IsSolid;
	}
	
	public void ResolveVoxel(Voxel voxel, string fillType)
	{
		ResolveVoxel(voxel, new TileDefinition(fillType));
	}

	public void ResolveVoxel(Voxel voxel, TileDefinition fillType)
	{
		CheckRebuildVoxels();

		for (int i = 0; i < Voxel.SideCount; ++i)
		{
			VoxelSide side = (VoxelSide)i;
			Vector3 direction = Voxel.GetSideDirection(side);
			ResolveVoxelSide(voxel, side, GetVoxel(voxel.Center + direction), fillType);
		}

		for (int i = 0; i < 8; ++i)
		{
			ResolveVoxelCorner(voxel, new Vector3(
				((i & 0x1) >> 0) - 0.5f,
				((i & 0x2) >> 1) - 0.5f,
				((i & 0x4) >> 2) - 0.5f
				));
		}
	}
	
	public void ResolveVoxel(Voxel voxel, Vector3 fillSampleDirection, string fillType)
	{
		ResolveVoxel(voxel, fillSampleDirection, new TileDefinition(fillType));
	}

	public void ResolveVoxel(Voxel voxel, Vector3 fillSampleDirection, TileDefinition fillType)
	{
		CheckRebuildVoxels();
		
		for (int i = 0; i < Voxel.SideCount; ++i)
		{
			VoxelSide side = (VoxelSide)i;
			Vector3 direction = Voxel.GetSideDirection(side);

			TileDefinition sideFillType = fillType;

			float directionDot = Vector3.Dot(direction, fillSampleDirection);

			VoxelFace adjacentFace = null;

			if (Mathf.Abs(directionDot) < 0.5f)
			{
				EdgeAngle edgeAngle;
				VoxelFace face = voxel.GetFace(side);
				adjacentFace = face.GetConnectedFace(face.GetTileSide(voxel.Center + fillSampleDirection), out edgeAngle);

			}
			else if (directionDot < -0.5f)
			{
				Voxel adjacentVoxel = GetVoxel(voxel.Center + fillSampleDirection);

				if (adjacentVoxel != null)
				{
					adjacentFace = adjacentVoxel.GetFace(side);
				}
			}

			if (adjacentFace != null && adjacentFace.TileInstance != null)
			{
				sideFillType = adjacentFace.TileInstance.GetTileDefinition();
			}

			ResolveVoxelSide(voxel, side, GetVoxel(voxel.Center + direction), sideFillType);
		}
	}

	private void GetEntireFace(Voxel voxel, VoxelSide side, string typeFilter, HashSet<Voxel> output)
	{
		if (voxel != null && voxel.IsSolid && !output.Contains(voxel))
		{
			Quaternion rotation = Voxel.GetFaceOrientation(side);
			Voxel faceVoxel = GetVoxel(voxel.Center + rotation * new Vector3(0.0f, 1.0f, 0.0f));
			Tile tile = null;

			if (typeFilter != null)
			{
				tile = voxel.GetFace(side).TileInstance;
			}

			if ((faceVoxel == null || !faceVoxel.IsSolid) && (tile == null || tile.tileType == typeFilter))
			{
				output.Add(voxel);

				GetEntireFace(GetVoxel(voxel.Center + rotation * new Vector3(1.0f, 0.0f, 0.0f)), side, typeFilter, output);
				GetEntireFace(GetVoxel(voxel.Center + rotation * new Vector3(-1.0f, 0.0f, 0.0f)), side, typeFilter, output);
				GetEntireFace(GetVoxel(voxel.Center + rotation * new Vector3(0.0f, 0.0f, 1.0f)), side, typeFilter, output);
				GetEntireFace(GetVoxel(voxel.Center + rotation * new Vector3(0.0f, 0.0f, -1.0f)), side, typeFilter, output);
			}
		}
	}
	
	public HashSet<Voxel> GetEntireFace(Voxel voxel, VoxelSide side, bool typeBoundary = false)
	{
		CheckRebuildVoxels();

		HashSet<Voxel> result = new HashSet<Voxel>();

		string typeFilter = null;

		if (typeBoundary)
		{
			Tile tile = voxel.GetFace(side).TileInstance;

			if (tile != null)
			{
				typeFilter = tile.tileType;
			}
		}

		GetEntireFace(voxel, side, typeFilter, result);

		return result;
	}

	private void RebuildVoxels()
	{
		voxels = new Voxel[width * height * depth];

		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				for (int z = 0; z < depth; ++z)
				{
					SetVoxel(x, y, z, new Voxel(this, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), GetStaticTransform()));
				}
			}
		}
	}
	
	private void ReconnectTiles()
	{
		Tile[] tiles = GetStaticTransform().GetComponentsInChildren<Tile>();

		Tile fallbackTile = currentTileset.AnyTile();
		
		foreach (Tile tile in tiles)
		{
			Vector3 up = tile.transform.localRotation * Vector3.up;
			Vector3 tileCenter = tile.transform.localPosition;

			Tile newTile = tile;

#if UNITY_EDITOR
			PrefabType prefabType = PrefabUtility.GetPrefabType(tile.gameObject);

			if (prefabType == PrefabType.MissingPrefabInstance && currentTileset != null)
			{
				Tile replacement = (Tile)PrefabUtility.InstantiatePrefab(fallbackTile);
				replacement.transform.parent = tile.transform.parent;
				replacement.transform.localPosition = tile.transform.localPosition;
				replacement.transform.localRotation = tile.transform.localRotation;
				replacement.transform.localScale = tile.transform.localScale;
				DestroyImmediate(tile);
				newTile = replacement;
			}
#endif

			Vector3 voxelCenter = tileCenter - up * 0.5f;
			
			Voxel voxel = GetVoxel(voxelCenter);

			if (voxel != null)
			{
				voxel.IsSolid = true;
				voxel.GetFace(up).SetTileDirect(newTile);
			}
		}
	}

	private void FillSolidVoxels()
	{
		for (int x = 0; x < width; ++x)
		{
			for (int z = 0; z < depth; ++z)
			{
				bool isSolid = false;

				for (int y = height - 1; y >= 0; --y)
				{
					Voxel voxel = GetVoxel(x, y, z);

					if (voxel.GetFace(VoxelSide.Top).TileInstance != null)
					{
						isSolid = true;
					}

					voxel.IsSolid = isSolid;

					if (voxel.GetFace(VoxelSide.Bottom).TileInstance != null)
					{
						isSolid = false;
					}
				}
			}
		}
	}

	private void OccupyDynamicObjects()
	{
		PlaceableObject[] placableObjects = GetDynamicTransform().GetComponentsInChildren<PlaceableObject>();

		foreach (PlaceableObject placableObject in placableObjects)
		{
			SetOccupySpace(placableObject, true);
		}
	}

	public void CheckRebuildVoxels()
	{
		if (voxels == null)
		{
			RebuildVoxels();
			ReconnectTiles();
			FillSolidVoxels();

			OccupyDynamicObjects();
		}
	}

	public void RefreshVoxels()
	{
		voxels = null;
	}

	public void RefreshOccupiedObjects()
	{
		if (voxels == null)
		{
			CheckRebuildVoxels();
		}
		else
		{
			foreach (Voxel voxel in voxels)
			{
				voxel.OccupiedBy = null;
			}

			OccupyDynamicObjects();
		}
	}

	public void DrillHole(Voxel voxel, Vector3 direction, int depth, string sideTile, string endTile)
	{
		int x = voxel.X;
		int y = voxel.Y;
		int z = voxel.Z;

		float maxComponent = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));

		int dx = (maxComponent == Mathf.Abs(direction.x)) ? (int)Mathf.Sign(direction.x) : 0;
		int dy = (maxComponent == Mathf.Abs(direction.y)) ? (int)Mathf.Sign(direction.y) : 0;
		int dz = (maxComponent == Mathf.Abs(direction.z)) ? (int)Mathf.Sign(direction.z) : 0;

		Voxel currentVoxel = voxel;
		VoxelSide floorFace = Voxel.GetSide(-direction);

		while (currentVoxel != null && !currentVoxel.IsSolid)
		{
			x += dx;
			y += dy;
			z += dz;

			currentVoxel = GetVoxel(x, y, z);
		}

		List<Voxel> toResolve = new List<Voxel>();

		for (int i = 0; currentVoxel != null && i < depth; ++i)
		{

			x += dx;
			y += dy;
			z += dz;
			
			Voxel nextVoxel = GetVoxel(x, y, z);
				
			currentVoxel.IsSolid = false;
			toResolve.Add(currentVoxel);

			// cap the last tile in the hole
			if (i == depth - 1 && nextVoxel != null && nextVoxel.IsSolid)
			{
				nextVoxel.SetTile(floorFace, endTile);
			}

			currentVoxel = nextVoxel;
		}

		foreach (Voxel modifiedVoxel in toResolve)
		{
			ResolveVoxel(modifiedVoxel, sideTile);
		}
	}

	public void Resize(int newWidth, int newHeight, int newDepth)
	{
		CheckRebuildVoxels();

		Voxel[] newData = new Voxel[newWidth * newHeight * newDepth];

		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				for (int z = 0; z < depth; ++z)
				{
					if (x < newWidth && 
					    y < newHeight && 
					    z < newDepth)
					{
						SetVoxel(newData, newWidth, newHeight, newDepth, x, y, z, GetVoxel(x, y, z));
					}
					else
					{
						GetVoxel(x, y, z).Destroy();
					}
				}
			}
		}
		
		for (int x = 0; x < newWidth; ++x)
		{
			for (int y = 0; y < newHeight; ++y)
			{
				for (int z = 0; z < newDepth; ++z)
				{
					if (x >= width ||
					    y >= height || 
					    z >= depth)
					{
						SetVoxel(newData, newWidth, newHeight, newDepth, x, y, z, new Voxel(this, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), GetStaticTransform()));
					}
				}
			}
		}

		voxels = newData;
		width = newWidth;
		height = newHeight;
		depth = newDepth;
	}
	
	public void OnDrawGizmos()
	{
		Vector3 size = new Vector3(width, height, depth);
		Gizmos.DrawWireCube(transform.TransformPoint(size * 0.5f) ,size);
	}
}
