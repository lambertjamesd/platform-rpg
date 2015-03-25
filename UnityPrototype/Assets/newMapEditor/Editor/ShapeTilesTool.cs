using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class ShapeTilesTool : VoxelMapTool {
	
	private VoxelSide selectedSide = VoxelSide.Right;
	private Voxel hoverVoxel;
	private HashSet<Voxel> selectedVoxels = new HashSet<Voxel>();
	private Vector3 selectedPoint;
	private int currentOffset = 0;

	public override void BecomeActive(VoxelMap voxelMap)
	{
		TileSelector tileSelector = EditorWindow.GetWindow<TileSelector>();
		tileSelector.SelectMode = TileSelector.Mode.Tiles;
		tileSelector.SelectedTileset = voxelMap.currentTileset;
		
		AddUndoCreationCallbacks(voxelMap, "Shaped tilemap");
	}

	public override void UseTool(VoxelMap voxelMap)
	{
		Event currentEvent = Event.current;

		HandleDefaultEvents(currentEvent, voxelMap);

		if (currentEvent.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
		{
			selectedVoxels.Clear();
		}
		
		if (currentEvent.type == EventType.Repaint)
		{
			Color selectedColor = new Color(0.7f, 0.3f, 0.0f, 0.25f);
			
			if (hoverVoxel != null)
			{
				if (selectedVoxels.Contains(hoverVoxel))
				{
					selectedColor = new Color(0.0f, 0.7f, 0.3f, 0.25f);
				}
				else
				{
					DrawVoxelSelection(voxelMap, hoverVoxel, selectedSide, new Color(0.0f, 0.7f, 0.3f, 0.25f));
				}
			}
			
			
			foreach (Voxel voxel in selectedVoxels)
			{
				DrawVoxelSelection(voxelMap, voxel, selectedSide, selectedColor);
			}
		}
		
		if ((currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseMove) && currentEvent.button == 0)
		{
			Ray localRay = GetLocalRay(voxelMap, currentEvent);
			
			if (currentEvent.type == EventType.MouseDown || (currentEvent.shift && currentEvent.type == EventType.MouseDrag))
			{
				VoxelRaycastCastHit raycastHit;
				
				if (voxelMap.LocalRaycast(localRay, out raycastHit))
				{
					if (!currentEvent.shift || selectedVoxels.Count == 0 || selectedSide == raycastHit.Side)
					{
						if (!currentEvent.shift && (!selectedVoxels.Contains(raycastHit.Voxel) || selectedSide != raycastHit.Side))
						{
							selectedVoxels.Clear();
						}
						
						if (currentEvent.type == EventType.MouseDown && currentEvent.shift && selectedVoxels.Contains(raycastHit.Voxel))
						{
							selectedVoxels.Remove(raycastHit.Voxel);
						}
						else
						{
							selectedSide = raycastHit.Side;
							selectedVoxels.Add(raycastHit.Voxel);
							selectedPoint = raycastHit.Position;
							currentOffset = 0;
						}
						
						if (currentEvent.control)
						{
							selectedVoxels.UnionWith(voxelMap.GetEntireFace(raycastHit.Voxel, selectedSide, true));
						}
					}
					
				}
				else
				{
					selectedVoxels.Clear();
				}
			}
			else if (currentEvent.type == EventType.MouseDrag)
			{
				if (selectedVoxels.Count > 0)
				{
					Vector3 direction = Voxel.GetSideDirection(selectedSide);
					Ray faceRay = new Ray(selectedPoint, direction);
					
					float selectDistance = MeshTools.GetDistanceOfNearsestPoint(faceRay, localRay);
					int nextOffset = Mathf.FloorToInt(selectDistance + 0.5f);
					
					
					if (nextOffset != currentOffset)
					{
						TileSelector tileSelector = EditorWindow.GetWindow<TileSelector>();
						TileDefinition selectedTile = tileSelector.SelectedTileDefinition;
						
						if (nextOffset > currentOffset)
						{
							HashSet<Voxel> nextVoxels = new HashSet<Voxel>();
							List<Voxel> toResolve = new List<Voxel>();
							
							foreach (Voxel selectedVoxel in selectedVoxels)
							{
								Voxel adjacentVoxel = voxelMap.GetVoxel(selectedVoxel.Center + direction);
								
								if (adjacentVoxel != null)
								{
									Tile drawTile = selectedVoxel.GetFace(selectedSide).TileInstance;
									adjacentVoxel.SetTile(selectedSide, drawTile == null ? selectedTile : drawTile.GetTileDefinition());
									toResolve.Add(adjacentVoxel);
									
									nextVoxels.Add(adjacentVoxel);
								}
								else
								{
									nextVoxels.Add(selectedVoxel);
								}
							}
							
							foreach (Voxel voxel in toResolve)
							{
								voxelMap.ResolveVoxel(voxel, -Voxel.GetSideDirection(selectedSide), selectedTile);
							}
							
							selectedVoxels = nextVoxels;
							
							++currentOffset;
						}
						else if (nextOffset < currentOffset)
						{
							HashSet<Voxel> nextVoxels = new HashSet<Voxel>();
							
							List<Voxel> toResolve = new List<Voxel>();
							
							foreach (Voxel selectedVoxel in selectedVoxels)
							{
								Voxel adjacentVoxel = voxelMap.GetVoxel(selectedVoxel.Center - direction);
								
								if (adjacentVoxel != null && adjacentVoxel.IsSolid)
								{
									Tile drawTile = selectedVoxel.GetFace(selectedSide).TileInstance;
									adjacentVoxel.SetTile(selectedSide, drawTile == null ? selectedTile : drawTile.GetTileDefinition());
									nextVoxels.Add(adjacentVoxel);
									toResolve.Add(adjacentVoxel);
								}
								
								selectedVoxel.IsSolid = false;
								toResolve.Add(selectedVoxel);
							}
							
							
							selectedVoxels = nextVoxels;
							
							
							foreach (Voxel voxel in toResolve)
							{
								voxelMap.ResolveVoxel(voxel, Voxel.GetSideDirection(selectedSide), selectedTile);
							}
							
							--currentOffset;
						}
					}
				}
			}
			else if (currentEvent.type == EventType.MouseMove)
			{	
				VoxelRaycastCastHit raycastHit;
				
				if (voxelMap.LocalRaycast(localRay, out raycastHit))
				{
					if (selectedVoxels.Count == 0 || selectedSide == raycastHit.Side)
					{
						hoverVoxel = raycastHit.Voxel;
						selectedSide = raycastHit.Side;
					}
					else
					{
						hoverVoxel = null;
					}
				}
				else
				{
					hoverVoxel = null;
				}
			}
			
			currentEvent.Use();
		}
	}
	
	public override void BecomeInactive(VoxelMap voxelMap)
	{
		voxelMap.InstantiateCallback = null;
		voxelMap.DestroyCallback = null;
	}
}
