using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class PlaceObjectsTool : VoxelMapTool {
	
	private VoxelSide selectedSide = VoxelSide.Right;
	private Vector3 selectedPoint;

	private Voxel hoverVoxel = null;
	private PlaceableObject previewObject;
	private PlaceableObject previewInstance;
	
	private PlaceableObject selectedObject;
	
	private int rotationStep = 0;

	private TileSelector tileSelector;

	public override void BecomeActive(VoxelMap voxelMap)
	{
		tileSelector = EditorWindow.GetWindow<TileSelector>();

		tileSelector.SelectMode = TileSelector.Mode.PlaceableObjects;
		tileSelector.SelectedTileset = voxelMap.currentTileset;
		
		voxelMap.RefreshOccupiedObjects();
		
		AddUndoCreationCallbacks(voxelMap, "Placed object");
	}

	private Vector3 GetPlacementLocation(Voxel voxel, VoxelSide side, PlaceableObject placeObject)
	{
		if (placeObject == null || side == VoxelSide.Bottom)
		{
			return -Vector3.one;
		}
		else if (VoxelFace.IsWall(side))
		{
			Vector3 size = RotatedSize(placeObject);
			Vector3 sideNormal = Voxel.GetSideDirection(side);

			size.x *= sideNormal.x;
			size.y *= sideNormal.y;
			size.z *= sideNormal.z;

			return voxel.Center + (size + sideNormal - Vector3.up)* 0.5f;
		}
		else
		{
			Vector3 size = RotatedSize(placeObject);

			size.y = 0.0f;

			return voxel.Center + Voxel.GetSideDirection(side) * 0.5f + (size - new Vector3(1.0f, 0.0f, 1.0f)) * 0.5f;
		}
	}
	
	private bool CanPlaceObject(Voxel voxel, VoxelSide side, PlaceableObject placeObject)
	{
		if (VoxelFace.IsWall(side) && !placeObject.PlaceOnWall ||
		    !VoxelFace.IsWall(side) && !placeObject.PlaceOnFloor)
		{
			return false;
		}

		Vector3 placeLocation = GetPlacementLocation(voxel, side, placeObject);

		if (placeLocation == -Vector3.one)
		{
			return false;
		}
		else
		{
			Vector3 size = RotatedSize(placeObject);
			Vector3 placeOrigin = placeLocation - new Vector3(size.x - 1.0f, 0.0f, size.z - 1.0f) * 0.5f;

			return voxel.Map.IsEmpty(
				Mathf.RoundToInt(placeOrigin.x), 
				Mathf.RoundToInt(placeOrigin.y), 
				Mathf.RoundToInt(placeOrigin.z),
				Mathf.RoundToInt(size.x),
				Mathf.RoundToInt(size.y),
				Mathf.RoundToInt(size.z));
		}
	}
	
	private void SetPreviewObject(PlaceableObject value)
	{
		if (previewObject != value)
		{
			if (previewInstance != null)
			{
				GameObjectHelper.DestroySafe(previewInstance.gameObject);
			}
			
			previewObject = value;
			
			if (value != null)
			{
				previewInstance = (PlaceableObject)PrefabUtility.InstantiatePrefab(value);
			}
		}
	}
	
	private void SetPreviewVisible(bool value)
	{
		if (previewInstance != null)
		{
			previewInstance.enabled = value;
		}
	}
	
	private void SetPreviewLocation(Vector3 objectLocation)
	{
		if (previewInstance != null)
		{
			previewInstance.transform.position = objectLocation;
			previewInstance.transform.rotation = CurrentRotation();
		}
	}
	
	private Quaternion CurrentRotation()
	{
		return Quaternion.AngleAxis(rotationStep * 90.0f, Vector3.up);
	}
	
	private Vector3 RotatedSize(PlaceableObject placeObject)
	{
		Vector3 size = CurrentRotation() * placeObject.Size;
		size.x = Mathf.Floor(Mathf.Abs(size.x) + 0.5f);
		size.y = Mathf.Floor(Mathf.Abs(size.y) + 0.5f);
		size.z = Mathf.Floor(Mathf.Abs(size.z) + 0.5f);
		return size;
	}
	
	private void UpdatePreviewLocation(VoxelMap voxelMap)
	{
		if (previewInstance != null && hoverVoxel != null)
		{
			Vector3 objectLocation = voxelMap.transform.TransformPoint(GetPlacementLocation(hoverVoxel, selectedSide, previewObject));
			SetPreviewLocation(objectLocation);
			
			SetPreviewVisible(CanPlaceObject(hoverVoxel, selectedSide, previewObject));
		}
		else
		{
			SetPreviewVisible(false);
		}
	}


	public override void UseTool (VoxelMap voxelMap)
	{
		Event currentEvent = Event.current;
		
		HandleDefaultEvents(currentEvent, voxelMap);
		
		if (currentEvent.type == EventType.ScrollWheel && currentEvent.shift)
		{
			if (currentEvent.delta.x > 0.0f)
			{
				++rotationStep;
			}
			else
			{
				--rotationStep;
			}
			
			currentEvent.Use();
			UpdatePreviewLocation(voxelMap);
		}
		
		if (currentEvent.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			return;
		}
		
		PlaceableObject placeableObject = tileSelector.SelectedPlacableObject;
		SetPreviewObject(placeableObject);
		
		if (currentEvent.type == EventType.Repaint)
		{
			if (hoverVoxel != null && placeableObject != null)
			{
				if (selectedSide == VoxelSide.Top && placeableObject.PlaceOnFloor)
				{
					Vector3 placementLocation = GetPlacementLocation(hoverVoxel, selectedSide, placeableObject);
					
					Vector3 objectLocation = voxelMap.transform.TransformPoint(placementLocation);
					
					Handles.ConeCap(0, objectLocation, voxelMap.transform.rotation * CurrentRotation(), 1.0f);
					/*
					for (int x = 0; x < (int)size.x; ++x)
					{
						for (int z = 0; z < (int)size.z; ++z)
						{
							Voxel adjacentVoxel = voxelMap.GetVoxel(hoverVoxel.Center + new Vector3(x, 0.0f, z));
							
							if (adjacentVoxel != null)
							{
								DrawVoxelSelection(voxelMap, adjacentVoxel, selectedSide, hoverColor);
							}
						}
					}*/
				}
			}
		}
		else if (currentEvent.type == EventType.MouseMove)
		{
			Ray localRay = GetLocalRay(voxelMap, currentEvent);
			
			VoxelRaycastCastHit raycastHit;
			if (voxelMap.LocalRaycast(localRay, out raycastHit, voxel => voxel.IsSolid || voxel.IsOccupied))
			{
				if (hoverVoxel != raycastHit.Voxel || selectedSide != raycastHit.Side)
				{
					HandleUtility.Repaint();
					hoverVoxel = raycastHit.Voxel;
					selectedSide = raycastHit.Side;
					UpdatePreviewLocation(voxelMap);
				}
			}
			else 
			{
				if (hoverVoxel != null)
				{
					HandleUtility.Repaint();
					hoverVoxel = null;
				}
				
				SetPreviewVisible(false);
			}
		}
		else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
		{
			if (placeableObject != null && hoverVoxel != null && CanPlaceObject(hoverVoxel, selectedSide, placeableObject))
			{
				PlaceableObject instance = (PlaceableObject)PrefabUtility.InstantiatePrefab(placeableObject);

				Undo.RegisterCreatedObjectUndo(instance.gameObject, "Added " + instance.name);

				Vector3 placementLocation = GetPlacementLocation(hoverVoxel, selectedSide, instance);
				
				instance.transform.parent = voxelMap.GetDynamicTransform();
				instance.transform.localPosition = placementLocation;
				instance.transform.localRotation = CurrentRotation();
				
				voxelMap.SetOccupySpace(instance, true);
			}
		}
	}
	
	public override void BecomeInactive(VoxelMap voxelMap)
	{
		SetPreviewObject(null);
		voxelMap.InstantiateCallback = null;
		voxelMap.DestroyCallback = null;
	}
}
