using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class RaiseTool : VoxelMapTool {
	
	private VoxelSide selectedSide = VoxelSide.Right;
	private Voxel hoverVoxel;
	private Vector3 startDrawVoxelLocation;

	private HashSet<Voxel> placedVoxels = new HashSet<Voxel>();

	public override void BecomeActive(VoxelMap voxelMap)
	{
		TileSelector tileSelector = EditorWindow.GetWindow<TileSelector>();
		tileSelector.SelectMode = TileSelector.Mode.Tiles;
		tileSelector.SelectedTileset = voxelMap.currentTileset;
		
		AddUndoCreationCallbacks(voxelMap, "Rasied voxles");
	}
	
	public override void UseTool(VoxelMap voxelMap)
	{
		Event currentEvent = Event.current;
		
		HandleDefaultEvents(currentEvent, voxelMap);
		
		if (currentEvent.type == EventType.Repaint)
		{
			if (hoverVoxel != null)
			{
				DrawVoxelSelection(voxelMap, hoverVoxel, selectedSide, new Color(0.0f, 0.3f, 0.7f, 0.25f));
			}
		}
		else if (currentEvent.type == EventType.MouseMove)
		{
			Ray localRay = GetLocalRay(voxelMap, currentEvent);
			
			VoxelRaycastCastHit raycastHit;
			if (voxelMap.LocalRaycast(localRay, out raycastHit))
			{
				if (hoverVoxel != raycastHit.Voxel || selectedSide != raycastHit.Side)
				{
					HandleUtility.Repaint();
					hoverVoxel = raycastHit.Voxel;
					selectedSide = raycastHit.Side;
				}
			}
			else 
			{
				if (hoverVoxel != null)
				{
					HandleUtility.Repaint();
					hoverVoxel = null;
				}
			}
		}
		else if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.button == 0)
		{
			if (currentEvent.type == EventType.MouseDown)
			{
				placedVoxels.Clear();
			}

			hoverVoxel = null;
			
			Ray localRay = GetLocalRay(voxelMap, currentEvent);
			
			VoxelRaycastCastHit raycastHit;
			Vector3 position;
			Vector3 normal;
			
			TileSelector tileSelector = EditorWindow.GetWindow<TileSelector>();
			TileDefinition selectedTileDefinition = tileSelector.SelectedTileDefinition;
			
			if (!selectedTileDefinition.IsNullTile)
			{
				Voxel voxelToDraw = null;

				if (voxelMap.LocalRaycast(localRay, out raycastHit, voxel => voxel.IsSolid && !placedVoxels.Contains(voxel)))
				{
					if (currentEvent.type == EventType.MouseDown)
					{
						startDrawVoxelLocation = raycastHit.Voxel.Center;
						
						VoxelFace face = raycastHit.Voxel.GetFace(raycastHit.Side);
						
						for (int i = 0; i < VoxelFace.FaceSideCount; ++i)
						{
							EdgeAngle edgeAngle;
							VoxelFace adjacentFace = face.GetConnectedFace((TileSide)i, out edgeAngle);
							
							if (adjacentFace.TileInstance != null && edgeAngle == EdgeAngle.FlatAngle)
							{
								TileDefinition tileDef = adjacentFace.TileInstance.GetTileDefinition();
								
								if (tileDef.typeName == selectedTileDefinition.typeName && tileDef.groupName == selectedTileDefinition.groupName)
								{
									startDrawVoxelLocation = tileDef.groupOrigin;
									break;
								}
							}
						}
					}
					
					selectedTileDefinition.SetGroupOrigin(startDrawVoxelLocation);

					voxelToDraw = voxelMap.GetVoxel(raycastHit.Position + raycastHit.Normal * 0.5f);
					selectedSide = raycastHit.Side;
				}
				else if (voxelMap.LocalRaycastWithBoundary(localRay, out position, out normal))
				{
					voxelToDraw = voxelMap.GetVoxel(position + normal * 0.5f);
					selectedSide = Voxel.GetSide(normal);
				}

				if (voxelToDraw != null)
				{
					placedVoxels.Add(voxelToDraw);

					voxelToDraw.SetTile(selectedSide, selectedTileDefinition);
					voxelMap.ResolveVoxel(voxelToDraw, selectedTileDefinition);
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
