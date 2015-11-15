using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using System.IO;

public abstract class VoxelMapTool
{
	public abstract void BecomeActive(VoxelMap voxelMap);
	public abstract void UseTool(VoxelMap voxelMap);
	public abstract void BecomeInactive(VoxelMap voxelMap);

	public virtual void OnInspectorGUI(VoxelMap voxelMap)
	{

	}

	public static void AddUndoCreationCallbacks(VoxelMap voxelMap, string undoMessage)
	{
		voxelMap.InstantiateCallback = (UnityEngine.Object template) => {
			UnityEngine.Object result = PrefabUtility.InstantiatePrefab(template);
			RegisterUndoCreation(result, undoMessage);
			return result;
		};
		
		voxelMap.DestroyCallback = (UnityEngine.Object toDestroy) => {
			Undo.DestroyObjectImmediate(toDestroy);
		};
	}

	protected static HashSet<Voxel> ExpandSelection(HashSet<Voxel> voxels)
	{
		HashSet<Voxel> result = new HashSet<Voxel>();

		foreach (Voxel voxel in voxels)
		{
			voxel.AppendAdjacentVoxel(result);
			result.Add(voxel);
		}

		return result;
	}

	protected static void RegisterUndoCreation(UnityEngine.Object newObject, string undoName)
	{
		GameObject gameObject = null;

		if (newObject is Component)
		{
			gameObject = ((Component)newObject).gameObject;
		}
		else if (newObject is GameObject)
		{
			gameObject = (GameObject)newObject;
		}

		if (gameObject != null)
		{
			Undo.RegisterCreatedObjectUndo(gameObject, undoName);
		}
	}

	protected void HandleDefaultEvents(Event currentEvent, VoxelMap voxelMap)
	{
		if (Camera.current == null || currentEvent.isKey)
		{
			return;
		}
		
		if (currentEvent.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			return;
		}
	}

	protected Ray GetLocalRay(VoxelMap map, Event mouseEvent)
	{
		Ray worldRay = Camera.current.ScreenPointToRay(new Vector3(mouseEvent.mousePosition.x, Camera.current.pixelHeight - mouseEvent.mousePosition.y, 0.0f));
		Vector3 localOrigin = map.transform.InverseTransformPoint(worldRay.origin);
		Vector3 localTarget = map.transform.InverseTransformPoint(worldRay.origin + worldRay.direction);
		return new Ray(localOrigin, localTarget - localOrigin);
	}

	protected void DrawVoxelSelection(VoxelMap map, Voxel voxel, VoxelSide side, Color color)
	{
		Quaternion faceRotation = Voxel.GetFaceOrientation(side);
		
		Vector3[] vertices = new Vector3[4];
		vertices[0] = map.transform.TransformPoint(voxel.Center + faceRotation * new Vector3(0.5f, 0.5f, -0.5f));
		vertices[1] = map.transform.TransformPoint(voxel.Center + faceRotation * new Vector3(-0.5f, 0.5f, -0.5f));
		vertices[2] = map.transform.TransformPoint(voxel.Center + faceRotation * new Vector3(-0.5f, 0.5f, 0.5f));
		vertices[3] = map.transform.TransformPoint(voxel.Center + faceRotation * new Vector3(0.5f, 0.5f, 0.5f));
		
		Handles.DrawSolidRectangleWithOutline(vertices, color, Color.white);
	}
}

[CustomEditor(typeof(VoxelMap))]
[CanEditMultipleObjects]
public class VoxelMapEditor : Editor
{
	private int width;
	private int height;
	private int depth;

	private VoxelMapTool currentTool = null;

	public enum BakeState
	{
		Idle,
		MakingCopy,
		BakingStaticGeometry,
		SavingPrefab,
		CleaningUp,
		StateCount
	}

	private bool isBakeRepaint = false;
	private BakeState bakeState = BakeState.Idle;
	private VoxelMap bakeCopy = null;

	public enum EditMode
	{
		None,
		Draw,
		Shape,
		Raise,
		Place,
		BuildCorner,
		ToolCount
	}

	private EditMode editMode = EditMode.None;

	private void Reinitialize()
	{
		VoxelMap voxelMap = (VoxelMap)target;
		width = voxelMap.Width;
		height = voxelMap.Height;
		depth = voxelMap.Depth;
		
		voxelMap.RefreshVoxels();
	}

	public void OnEnable()
	{
		Reinitialize();
	}

	public void OnDisable()
	{
		SetCurrentTool(null);
	}

	private void SetCurrentTool(VoxelMapTool tool)
	{
		VoxelMap map = (VoxelMap)target;

		if (currentTool != null)
		{
			currentTool.BecomeInactive(map);
		}

		currentTool = tool;

		if (tool != null)
		{
			currentTool.BecomeActive(map);
		}
	}

	private string GetReadableBakeState()
	{
		switch (bakeState)
		{
		case BakeState.Idle:
			return "idle";
		case BakeState.MakingCopy:
			return "making copy";
		case BakeState.BakingStaticGeometry:
			return "baking static geometry";
		case BakeState.SavingPrefab:
			return "saving prefab";
		case BakeState.CleaningUp:
			return "cleaning up";
		default:
			return "invalid state";
		}
	}

	private static string GetPrefabPath(UnityEngine.Object existingPrefab)
	{
		if (existingPrefab == null)
		{
			string currentScene = EditorApplication.currentScene;

			if (currentScene == null || currentScene.Length == 0)
			{
				return "Assets/BakedLevel.prefab";
			}
			else
			{
				return Path.ChangeExtension(currentScene, "prefab");
			}
		}
		else
		{
			return AssetDatabase.GetAssetPath(existingPrefab);
		}
	}

	private static void RebakePrefabs(string[] sceneNames)
	{
		SceneTour.TakeTour(sceneNames, () => {
			VoxelMap[] toBake = Object.FindObjectsOfType<VoxelMap>();
			
			foreach (VoxelMap map in toBake)
			{
				InlineBake(map);
			}
		}, false);
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		if (serializedObject.isEditingMultipleObjects)
		{
			SerializedProperty isBaked = serializedObject.FindProperty("isBaked");

			if (!isBaked.hasMultipleDifferentValues && isBaked.boolValue)
			{
				if (GUILayout.Button("Rebake all from source"))
				{
					Object[] voxelMaps = serializedObject.targetObjects;
					string[] sceneNames = new string[voxelMaps.Length];

					for (int i = 0; i < voxelMaps.Length; ++i)
					{
						sceneNames[i] = ((VoxelMap)voxelMaps[i]).SourceScene;
					}

					RebakePrefabs(sceneNames);
				}

				EditorGUILayout.LabelField("Warning: This will take a long time when used on many levels");
			}

			return;
		}

		if (bakeState != BakeState.Idle)
		{
			GUI.enabled = false;
		}

		SerializedProperty tilesetProperty = serializedObject.FindProperty("currentTileset");
		Tileset currentTileset = (Tileset)tilesetProperty.objectReferenceValue;
		Tileset tileset = (Tileset)EditorGUILayout.ObjectField("Current Tileset", currentTileset, typeof(Tileset), false);

		if (tileset != currentTileset)
		{
			EditorWindow.GetWindow<TileSelector>().SelectedTileset = tileset;
			tilesetProperty.objectReferenceValue = tileset;
		}

		width = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("width"), width));
		height = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("height"), height));
		depth = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("depth"), depth));

		if (GUILayout.Button(new GUIContent("Resize")))
		{
			VoxelMap voxelMap = (VoxelMap)target;

			Undo.RecordObject(voxelMap.gameObject, "Resize voxel map");

			VoxelMap.InstantiateCallbackDelegate previousInstantiate = voxelMap.InstantiateCallback;
			VoxelMap.DestoryCallbackDelegate previousDestroy = voxelMap.DestroyCallback;

			VoxelMapTool.AddUndoCreationCallbacks(voxelMap, "Resize voxel map");

			voxelMap.Resize(width, height, depth);
			serializedObject.SetIsDifferentCacheDirty();
			SceneView.RepaintAll();

			voxelMap.InstantiateCallback = previousInstantiate;
			voxelMap.DestroyCallback = previousDestroy;
		}

		if (GUILayout.Button (new GUIContent ("Make Navmesh"))) 
		{
			//NavmeshMaker.makeNavMesh((VoxelMap)target);
		}

		EditorGUILayout.Space();

		GUIContent[] guiContent = new GUIContent[(int)EditMode.ToolCount];

		guiContent[(int)EditMode.None] = new GUIContent("None");
		guiContent[(int)EditMode.Draw] = new GUIContent("Draw");
		guiContent[(int)EditMode.Shape] = new GUIContent("Shape");
		guiContent[(int)EditMode.Raise] = new GUIContent("Raise");
		guiContent[(int)EditMode.Place] = new GUIContent("Place");
		guiContent[(int)EditMode.BuildCorner] = new GUIContent("Build Corner");

		EditMode newEditMode = (EditMode)GUILayout.SelectionGrid((int)editMode, guiContent, 3);

		if (newEditMode != editMode)
		{
			SceneView.RepaintAll();
			editMode = newEditMode;

			SetCurrentTool(null);

			switch (editMode)
			{
			case EditMode.Draw:
				SetCurrentTool(new DrawTilesTool());
				break;
			case EditMode.Shape:
				SetCurrentTool(new ShapeTilesTool());
				break;
			case EditMode.Raise:
				SetCurrentTool(new RaiseTool());
				break;
			case EditMode.Place:
				SetCurrentTool(new PlaceObjectsTool());
				break;
			case EditMode.BuildCorner:
				SetCurrentTool(new CornerMappingTool());
				break;
			}
		}

		if (currentTool != null)
		{
			serializedObject.ApplyModifiedProperties();
			currentTool.OnInspectorGUI((VoxelMap)target);
			serializedObject.Update();
		}
		
		EditorGUILayout.Separator();

		if (!serializedObject.FindProperty("isBaked").boolValue)
		{
			EditorGUILayout.Space();
			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("tileOrigin"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("tileOriginRelative"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("faceRotation"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("bakeCeilings"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("simplifyColliderMesh"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("simplifyVisualMesh"));

			BakeLevelStep();

			if (GUILayout.Button(new GUIContent("Bake Prefab")))
			{
				SetNextBakeState(BakeState.MakingCopy);
			}

			SerializedProperty prefabSaveTarget = serializedObject.FindProperty("prefabSaveTarget");
			UnityEngine.Object prefabTarget = prefabSaveTarget.objectReferenceValue;
			if (prefabTarget != null)
			{
				EditorGUILayout.SelectableLabel("Filename: " + GetPrefabPath(prefabTarget));
			}

			GUI.enabled = true;

			Rect progressRect = EditorGUILayout.BeginVertical(GUILayout.Height(20.0f));
			EditorGUI.ProgressBar(progressRect, (float)bakeState / (float)BakeState.StateCount, "Bake state: " + GetReadableBakeState());
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
		}
		else
		{
			VoxelMap voxelMap = (VoxelMap)target;

			if (voxelMap.SourceScene != null && voxelMap.SourceScene.Length > 0)
			{
				if (GUILayout.Button(new GUIContent("Rebake from source")))
				{
					RebakePrefabs(new string[]{voxelMap.SourceScene});

					return;
				}

				if (GUILayout.Button(new GUIContent("Open containing scene")))
				{
					EditorApplication.OpenScene(voxelMap.SourceScene);
				}

				EditorGUILayout.SelectableLabel("Scene Source: " + voxelMap.SourceScene);
			}

			GUI.enabled = true;
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void SetNextBakeState(BakeState value)
	{
		bakeState = value;
		EditorUtility.SetDirty(target);
		isBakeRepaint = true;
	}

	public static void InlineBake(VoxelMap map)
	{
		VoxelMap bakeCopy = null;
		BakeState currentState = BakeState.MakingCopy;

		while (currentState != BakeState.Idle)
		{
			currentState = BakeStep(currentState, map, ref bakeCopy);
		}
	}

	public static BakeState BakeStep(BakeState currentState, VoxelMap map, ref VoxelMap bakeCopy)
	{
		switch (currentState)
		{
		case BakeState.Idle:
			return BakeState.Idle;
		case BakeState.MakingCopy:
			bakeCopy = (VoxelMap)Instantiate(map);
			return BakeState.BakingStaticGeometry;
		case BakeState.BakingStaticGeometry:
			bakeCopy.BakeStaticGeometry(EditorApplication.currentScene);

			map.gameObject.SendMessage("VoxelMapBaked", SendMessageOptions.DontRequireReceiver);

			return BakeState.SavingPrefab;
		case BakeState.SavingPrefab:
		{
			UnityEngine.Object prefab = map.PrefabSaveTarget;
			
			if (prefab == null)
			{
				prefab = PrefabUtility.CreateEmptyPrefab(GetPrefabPath(null));
			}
			else
			{
				// clear the prefab
				string preafabPath = AssetDatabase.GetAssetPath(prefab);
				UnityEngine.Object[] existingObjects = AssetDatabase.LoadAllAssetsAtPath(preafabPath);
				foreach (UnityEngine.Object existingObject in existingObjects)
				{
					if (existingObject is Mesh)
					{
						GameObject.DestroyImmediate(existingObject, true);
					}
				}
			}
			
			foreach (MeshFilter filter in bakeCopy.GetBakedTransform().GetComponentsInChildren<MeshFilter>())
			{
				AssetDatabase.AddObjectToAsset(filter.sharedMesh, prefab);
			}
			
			foreach (MeshCollider collider in bakeCopy.GetBakedTransform().GetComponentsInChildren<MeshCollider>())
			{
				AssetDatabase.AddObjectToAsset(collider.sharedMesh, prefab);
			}
			
			PrefabUtility.ReplacePrefab(bakeCopy.gameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
			
			map.PrefabSaveTarget = prefab;
			return BakeState.CleaningUp;
		}
		case BakeState.CleaningUp:
			DestroyImmediate(bakeCopy.gameObject);
			return BakeState.Idle;
		default:
			return BakeState.Idle;
		}
	}

	private void BakeLevelStep()
	{
		try 
		{
			if (Event.current.type == EventType.Layout)
			{
				if (isBakeRepaint)
				{
					Repaint();
					isBakeRepaint = false;
					return;
				}

				SetNextBakeState(BakeStep(bakeState, (VoxelMap)target, ref bakeCopy));
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError(e);
			SetNextBakeState(BakeState.Idle);
		}
	}

	public void OnSceneGUI()
	{
		Event currentEvent = Event.current;
		
		if (currentEvent.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
		{
			Reinitialize();
		}

		if (currentTool != null)
		{
			currentTool.UseTool((VoxelMap)target);
		}
	}
}
