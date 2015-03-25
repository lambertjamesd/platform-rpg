using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class CornerMappingTool : VoxelMapTool {

	private class FaceInformation
	{
		private VoxelFace face;
		private TileSide side;
		private EdgeAngle edgeAngle;

		public FaceInformation(VoxelFace face, TileSide side, EdgeAngle edgeAngle)
		{
			this.face = face;
			this.side = side;
			this.edgeAngle = edgeAngle;
		}

		public VoxelFace Face
		{
			get
			{
				return face;
			}
		}

		public TileSide Side
		{
			get
			{
				return side;
			}
		}

		public EdgeAngle Angle
		{
			get
			{
				return edgeAngle;
			}
		}

		public bool AnyTileType
		{
			get;
			set;
		}

		public bool AnyTileRotation
		{
			get;
			set;
		}

		public string TileType
		{
			get
			{
				if (AnyTileType || face.TileInstance == null)
				{
					return "[Any]";
				}
				else
				{
					return face.TileInstance.tileType;
				}
			}
		}
	}
		
	private List<FaceInformation> selectedFaces;
	private GameObject newTileCorner;
	
	public override void BecomeActive(VoxelMap voxelMap)
	{

	}

	private void DrawArrow(Vector3 position, Quaternion rotation)
	{
		Vector3 arrowTip = position + rotation * Vector3.forward * 0.5f;
		Handles.DrawLine(position, arrowTip);
		Handles.DrawLine(arrowTip, position + rotation * new Vector3(0.125f, 0.0f, 0.375f));
		Handles.DrawLine(arrowTip, position + rotation * new Vector3(-0.125f, 0.0f, 0.375f));
	}

	public override void UseTool(VoxelMap voxelMap)
	{
		Event currentEvent = Event.current;

		HandleDefaultEvents(currentEvent, voxelMap);

		if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
		{
			Ray localRay = GetLocalRay(voxelMap, currentEvent);
			
			VoxelRaycastCastHit raycastHit;
			if (voxelMap.LocalRaycast(localRay, out raycastHit))
			{
				Vector3 selectedCornerLocation = new Vector3(Mathf.Round(raycastHit.Position.x), Mathf.Round(raycastHit.Position.y), Mathf.Round(raycastHit.Position.z));

				List<VoxelFace> faces;
				List<TileSide> sides;
				List<EdgeAngle> edgeAngles;

				if (voxelMap.GetCornerTiles(raycastHit.Face, selectedCornerLocation, out faces, out sides, out edgeAngles))
				{
					selectedFaces = new List<FaceInformation>(faces.Count);

					for (int i = 0; i < faces.Count; ++i)
					{
						selectedFaces.Add(new FaceInformation(faces[i], sides[i], edgeAngles[i]));
					}
				}
				else
				{
					selectedFaces = null;
				}
				
				HandleUtility.Repaint();
			}
			else
			{
				selectedFaces = null;
			}
		}
		else if (currentEvent.type == EventType.Repaint)
		{
			if (selectedFaces != null)
			{
				bool isFirst = true;

				Color selectionColor = new Color(0.0f, 1.0f, 0.0f, 0.2f);
				Color anyTypeColor = new Color(1.0f, 0.0f, 1.0f, 0.2f);

				int index = 0;
				foreach (FaceInformation face in selectedFaces)
				{
					Color faceColor = face.AnyTileType ? anyTypeColor : selectionColor;

					DrawVoxelSelection(voxelMap, face.Face.ParentVoxel, face.Face.Side, isFirst ? faceColor : (faceColor * 0.5f));
					Handles.Label(voxelMap.transform.TransformPoint(face.Face.Center), index.ToString() + ": " + face.TileType);

					Tile tileInstance = face.Face.TileInstance;

					if (tileInstance != null)
					{
						if (face.AnyTileRotation)
						{
							for (int i = 0; i < 4; ++i)
							{
								DrawArrow(tileInstance.transform.position, tileInstance.transform.rotation * Quaternion.AngleAxis(90.0f * i, Vector3.up));
							}
						}
						else
						{
							DrawArrow(tileInstance.transform.position, tileInstance.transform.rotation);
						}
					}

					isFirst = false;
					++index;
				}
			}
		}
	}

	public override void BecomeInactive(VoxelMap voxelMap)
	{

	}

	private void RotateBack()
	{
		if (selectedFaces != null && selectedFaces.Count > 0)
		{
			selectedFaces.Add(selectedFaces[0]);
			selectedFaces.RemoveAt(0);
		}
	}

	private void RotateForward()
	{
		if (selectedFaces != null && selectedFaces.Count > 0)
		{
			selectedFaces.Insert(0, selectedFaces[selectedFaces.Count - 1]);
			selectedFaces.RemoveAt(selectedFaces.Count - 1);
		}
	}

	public void UpdateNewTilePosition()
	{
		if (newTileCorner != null && selectedFaces != null)
		{
			selectedFaces[0].Face.ApplyCornerTransform(selectedFaces[0].Side, newTileCorner.transform);
		}
	}
	
	private static string GetPrefabPath(string prefabName)
	{
		string currentScene = EditorApplication.currentScene;
		
		if (currentScene == null || currentScene.Length == 0)
		{
			return "Assets/" + prefabName + ".prefab";
		}
		else
		{
			return Path.Combine(Path.GetDirectoryName(currentScene) , prefabName + ".prefab");
		}
	}

	public void CreateNewTile(UnityEngine.Object targetPrefab, Tileset tileset)
	{
		GameObject objectCopy = (GameObject)Editor.Instantiate(newTileCorner);
		objectCopy.name = newTileCorner.name;

		TileCorner corner = objectCopy.GetComponent<TileCorner>();

		if (corner == null)
		{
			corner = objectCopy.AddComponent<TileCorner>();
		}

		corner.TargetTileset = tileset;
		corner.EdgeAngleCount = selectedFaces.Count - 1;

		for (int i = 0; i < selectedFaces.Count; ++i)
		{
			FaceInformation face = selectedFaces[i];

			corner.SetEdgeAngle(i, face.Angle);
			TileInfoType tileInfo = corner.GetTileType(i);

			tileInfo.TileType = face.AnyTileType ? null : face.TileType;

			for (int side = 0; side < 4; ++side)
			{
				tileInfo.SetDoesMatchSide((TileSide)side, face.AnyTileRotation);
			}

			tileInfo.SetDoesMatchSide(face.Side, true);
		}

		PrefabUtility.ReplacePrefab(objectCopy, targetPrefab, ReplacePrefabOptions.ReplaceNameBased);
		
		string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
		GameObject prefabGameObject = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
		tileset.AddCorner(prefabGameObject.GetComponent<TileCorner>());
		EditorUtility.SetDirty(tileset);

		Editor.DestroyImmediate(objectCopy);

		EditorGUIUtility.PingObject(targetPrefab);
	}
	
	public override void OnInspectorGUI(VoxelMap voxelMap)
	{
		EditorGUILayout.Separator();

		if (selectedFaces != null)
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("<"))
			{
				RotateBack();
				UpdateNewTilePosition();
			}

			if (GUILayout.Button(">"))
			{
				RotateForward();
				UpdateNewTilePosition();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.LabelField("Faces");
			for (int i = 0; i < selectedFaces.Count; ++i)
			{
				FaceInformation faceInfo = selectedFaces[i];

				EditorGUILayout.Foldout(true, "Face " + i + ": " + faceInfo.TileType);
				faceInfo.AnyTileType = EditorGUILayout.Toggle("Match any tile", faceInfo.AnyTileType);
				faceInfo.AnyTileRotation = EditorGUILayout.Toggle("Match any rotation", faceInfo.AnyTileRotation);
			}

			GameObject newTileCornerReplacement = (GameObject)EditorGUILayout.ObjectField(newTileCorner, typeof(GameObject), true);

			if (newTileCornerReplacement != newTileCorner)
			{
				newTileCorner = newTileCornerReplacement;
				UpdateNewTilePosition();
			}

			bool wasEnabled = GUI.enabled;
			GUI.enabled = newTileCorner != null;

			if (GUILayout.Button("Create New Corner Piece"))
			{
				string prefabPath = AssetDatabase.GenerateUniqueAssetPath(GetPrefabPath(newTileCorner.name));
				UnityEngine.Object targetPrefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
				CreateNewTile(targetPrefab, voxelMap.currentTileset);
			}

			VoxelFace firstFace = selectedFaces[0].Face;
			TileCorner corner = firstFace.GetCorner(selectedFaces[0].Side);

			GUI.enabled = newTileCorner != null && corner != null && PrefabUtility.GetPrefabType(corner) == PrefabType.PrefabInstance;
			
			if (GUILayout.Button("Replace Existing Corner"))
			{
				UnityEngine.Object targetPrefab = PrefabUtility.GetPrefabParent(corner);
				CreateNewTile(targetPrefab, voxelMap.currentTileset);
			}

			GUI.enabled = wasEnabled;
		}
	}
}
