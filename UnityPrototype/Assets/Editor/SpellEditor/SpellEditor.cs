using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SpellEditor : EditorWindow {

	EffectAsset currentlyOpenSpell;

	Vector2 lastMousePosition;
	Vector2 lastDragPosition;
	Vector2 scrollPos = Vector2.zero;
	
	private const string settingsFileName = "EffectSettings";

	private SpellNode rootNode;
	private List<SpellNode> nodes = new List<SpellNode>();
	private List<SpellConnection> connections = new List<SpellConnection>();

	private SpellNodeConnector hoverNode;
	private SpellNodeConnector selectedNode;
	private Rect targetRect;

	private SpellNode selectedSpellNode;

	private const float scrollMargin = 200.0f;

	private Texture2D backgroundImage = null;

	private SpellTypeParser effectTypes;
	private SpellBuiltInNodes builtInTypes;

	public void New()
	{
		Init();
		currentlyOpenSpell = null;
		rootNode = null;
		nodes = new List<SpellNode>();
		connections = new List<SpellConnection>();
	}

	public void InitializeDefaultFile()
	{
		AddNode(effectTypes.SpellTypes["caster"], new Vector2(100.0f, 100.0f));
	}

	[MenuItem ("Assets/Create/Spell")]
	static void CreateSpell()
	{
		EffectAsset newSpell = ScriptableObject.CreateInstance<EffectAsset>();
		newSpell.xmlText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<caster />";

		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (path == "")
		{

		}
		else if (Path.GetExtension(path) != "")
		{
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}

		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New Spell.asset");

		AssetDatabase.CreateAsset(newSpell, assetPathAndName);
		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = newSpell;
	}

	[MenuItem("Window/Spell Editor")]
	static void ShowEditor() {
		SpellEditor editor = EditorWindow.GetWindow<SpellEditor>();
		editor.Init();
	}

	public void Reload() {
		effectTypes = null;
		if (currentlyOpenSpell != null)
		{
			LoadAsset(currentlyOpenSpell);
		}
	}

	public void Init() {
		if (effectTypes == null)
		{
			effectTypes = new SpellTypeParser(settingsFileName);
			builtInTypes = new SpellBuiltInNodes();
		}
	}

	public void LoadAsset(EffectAsset xmlDocument)
	{
		New();
		currentlyOpenSpell = xmlDocument;
		SpellXmlLoader loader = new SpellXmlLoader(xmlDocument, this);
		loader.Load();
	}

	public void Save()
	{
		if (currentlyOpenSpell != null && rootNode != null)
		{
			SpellXmlGenerator.WriteFile(currentlyOpenSpell, rootNode);
			EditorUtility.SetDirty(currentlyOpenSpell);
			AssetDatabase.SaveAssets();
		}
	}

	public EffectAsset CurretlyOpenSpell
	{
		get
		{
			return currentlyOpenSpell;
		}

		set
		{
			Save();
			LoadAsset(value);
		}
	}

	public SpellTypeParser EffectTypes
	{
		get
		{
			return effectTypes;
		}
	}

	public SpellBuiltInNodes BuiltInTypes
	{
		get
		{
			return builtInTypes;
		}
	}

	public void SetSelectedSpellNode(SpellNode node)
	{
		selectedSpellNode = node;
	}

	public void RemoveNode(SpellNode node)
	{
		if (rootNode == node || node == null)
		{
			return;
		}

		nodes.Remove(node);

		for (int i = 0; i < node.ConnectorCount; ++i)
		{
			SpellConnection connection = GetFirstConnection(node.GetConnector(i));

			if (connection != null)
			{
				RemoveConnection(connection);
			}
		}

		Repaint();
	}

	public SpellNode AddNode(SpellNodeType type, Vector2 position)
	{
		SpellNode newNode = new SpellNode(type, this);
		newNode.Position = position;
		nodes.Add(newNode);
		
		if (newNode.Type.IsRoot)
		{
			SpellNode lastRoot = rootNode;
			rootNode = newNode;
			RemoveNode(lastRoot);
		}

		return newNode;
	}
	
	public void RemoveConnection(SpellConnection connection)
	{
		connections.Remove(connection);
	}

	public void AddConnection(SpellNodeConnector source, SpellNodeConnector destination)
	{
		if (source.Type.IsInput)
		{
			SpellNodeConnector switchTmp = source;
			source = destination;
			destination = switchTmp;
		}


		if (!source.Type.SupportMultipleConnections)
		{
			SpellConnection previousSource = GetFirstConnection(source);

			if (previousSource != null)
			{
				RemoveConnection(previousSource);
			}
		}

		SpellConnection previousDest = GetFirstConnection(destination);
		
		if (previousDest != null)
		{
			RemoveConnection(previousDest);
		}

		connections.Add(new SpellConnection(source, destination));
	}

	public SpellConnection GetFirstConnection(SpellNodeConnector connector)
	{
		foreach (SpellConnection connection in connections)
		{
			if (connection.Contains(connector))
			{
				return connection;
			}
		}

		return null;
	}

	public List<SpellConnection> GetAllConnections(SpellNodeConnector connector)
	{
		List<SpellConnection> result = new List<SpellConnection>();

		foreach (SpellConnection connection in connections)
		{
			if (connection.Contains(connector))
			{
				result.Add(connection);
			}
		}
		
		return result;
	}

	private void SetHoverNode(SpellNodeConnector node)
	{
		if (hoverNode != null)
		{
			hoverNode.State = ConnectorState.Default;
		}

		hoverNode = node;

		if (hoverNode != null)
		{
			hoverNode.State = ConnectorState.Hover;
		}
	}
	
	private void SetSelectedNode(SpellNodeConnector node)
	{
		if (selectedNode != null)
		{
			selectedNode.State = ConnectorState.Default;
		}
		
		selectedNode = node;
		
		if (selectedNode != null)
		{
			selectedNode.State = ConnectorState.Selected;
			targetRect = selectedNode.BoundingRect;
		}
	}

	private SpellNodeConnector GetNode(Vector2 mousePos)
	{
		for (int i = nodes.Count - 1; i >= 0; --i)
		{
			SpellNodeConnector result = nodes[i].GetConnector(mousePos);

			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	void CheckMouseEvents()
	{
		Event currentEvent = Event.current;
		Vector2 mousePosition = currentEvent.mousePosition;
		
		if (currentEvent.isMouse)
		{
			SpellNodeConnector currentHover = GetNode(currentEvent.mousePosition);

			if (currentEvent.button == 2)
			{
				if (currentEvent.type == EventType.MouseDown)
				{
					lastDragPosition = mousePosition;
				}
				else if (currentEvent.type == EventType.MouseDrag)
				{
					scrollPos -= mousePosition - lastDragPosition;
					lastDragPosition = mousePosition;
					Repaint();
				}
			}

			if (selectedNode == null)
			{
				if (currentHover != hoverNode)
				{
					SetHoverNode(currentHover);
					Repaint();
				}
				
				if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
				{
					if (currentHover != null)
					{
						SpellConnection existingConnection = GetFirstConnection(currentHover);

						if (currentHover.Type.IsInput && existingConnection != null)
						{
							SetHoverNode(null);
							SetSelectedNode(existingConnection.Source);
							RemoveConnection(existingConnection);
						}
						else
						{
							SetHoverNode(null);
							SetSelectedNode(currentHover);
						}

						Repaint();
					}
				}
			}
			else
			{
				Vector2 windowCoordinates = mousePosition - scrollPos;

				if (windowCoordinates.x < 0)
				{
					scrollPos.x = mousePosition.x;
				}
				else if (windowCoordinates.x > position.width)
				{
					scrollPos.x = mousePosition.x - position.width;
				}
				
				if (windowCoordinates.y < 0)
				{
					scrollPos.y = mousePosition.y;
				}
				else if (windowCoordinates.y > position.height)
				{
					scrollPos.y = mousePosition.y - position.height;
				}

				targetRect = new Rect(mousePosition.x, mousePosition.y, 1.0f, 1.0f);

				if (currentHover != null && currentHover != selectedNode)
				{
					SetHoverNode(currentHover);

					if (hoverNode.CanConnectTo(selectedNode))
					{
						hoverNode.State = ConnectorState.Valid;
						targetRect = hoverNode.BoundingRect;
					}
					else
					{
						hoverNode.State = ConnectorState.Invalid;
					}
				}
				else if (hoverNode != null)
				{
					SetHoverNode(null);
				}

				if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
				{
					if (hoverNode != null && hoverNode.State == ConnectorState.Valid)
					{
						AddConnection(hoverNode, selectedNode);
					}

					SetSelectedNode(null);
					SetHoverNode(null);
				}

				Repaint();
			}
		}

		lastMousePosition = mousePosition;
	}

	public void ContextMenuCallback(object obj) {
		if (obj is SpellNodeType)
		{
			AddNode(obj as SpellNodeType, lastMousePosition);
		}
	}

	void OnGUI() {
		if (currentlyOpenSpell == null || rootNode == null)
		{
			return;
		}

		if (backgroundImage == null)
		{
			backgroundImage = Resources.Load<Texture2D>("SpellEditor/BoxBackground");
			backgroundImage.wrapMode = TextureWrapMode.Repeat;
		}

		float width = position.width;
		float height = position.height;
		
		foreach (SpellNode node in nodes)
		{
			Vector2 nodeMax = node.Position + node.Size;
			width = Mathf.Max(width, nodeMax.x + scrollMargin);
			height = Mathf.Max(height, nodeMax.y + scrollMargin);
		}


		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height));
		GUILayout.Box("Spell Editor", GUILayout.Width(width), GUILayout.Height(height));

		GUI.DrawTextureWithTexCoords(new Rect(0, 0, width, height), backgroundImage, new Rect(0, 0, width / backgroundImage.width, height / backgroundImage.height));

		wantsMouseMove = true;
		
		CheckMouseEvents();

		if (selectedNode != null)
		{
			if (selectedNode.Type.IsInput)
			{
				SpellConnection.DrawNodeCurve(targetRect, selectedNode.BoundingRect);
			}
			else
			{
				SpellConnection.DrawNodeCurve(selectedNode.BoundingRect, targetRect);
			}
		}

		foreach (SpellConnection connection in connections)
		{
			connection.Draw();
		}
		
		BeginWindows();
		int i = 1;
		foreach (SpellNode node in nodes)
		{
			node.DrawWindow(i, selectedNode == null);
			++i;
		}
		EndWindows();

		
		foreach (SpellNode node in nodes)
		{
			node.DrawNodes();
		}
		
		Event currentEvent = Event.current;

		if (currentEvent.type == EventType.ContextClick)
		{
			GenericMenu menu = new GenericMenu();

			foreach (string spellName in effectTypes.SpellTypes.Keys)
			{
				SpellNodeType spellType = effectTypes.SpellTypes[spellName];

				if (spellType.InConnection == null)
				{
					menu.AddItem(new GUIContent(spellType.Namespace + "/" + spellName, spellType.Description), false, ContextMenuCallback, spellType);
				}
				else 
				{
					menu.AddItem(new GUIContent(spellType.Namespace + "/" + spellName, spellType.Description), false, ContextMenuCallback, spellType);
				}
			}

			foreach (string spellName in effectTypes.Functions.Keys)
			{
				SpellNodeType spellType = effectTypes.Functions[spellName];
				
				if (spellType.InConnection == null)
				{
					menu.AddItem(new GUIContent(spellType.Namespace + "/" + spellName, spellType.Description), false, ContextMenuCallback, spellType);
				}
				else 
				{
					menu.AddItem(new GUIContent(spellType.Namespace + "/" + spellName, spellType.Description), false, ContextMenuCallback, spellType);
				}
			}

			foreach (string spellName in builtInTypes.BuiltInTypes.Keys)
			{
				SpellNodeType spellType = builtInTypes.BuiltInTypes[spellName];
				
				if (spellType.InConnection == null)
				{
					menu.AddItem(new GUIContent(spellType.Namespace + "/" + spellName, spellType.Description), false, ContextMenuCallback, spellType);
				}
				else 
				{
					menu.AddItem(new GUIContent(spellType.Namespace + "/" + spellName, spellType.Description), false, ContextMenuCallback, spellType);
				}
			}



			menu.ShowAsContext();
		}

		if (currentEvent.type == EventType.KeyUp)
		{
			if (currentEvent.keyCode == KeyCode.Delete && selectedSpellNode != null)
			{
				RemoveNode(selectedSpellNode);
			}
			else if (currentEvent.control && currentEvent.keyCode == KeyCode.S)
			{
				Save();
			}
		}

		EditorGUILayout.EndScrollView();

		if (GUI.Button(new Rect(0.0f, 0.0f, 100.0f, 20.0f), "Reload"))
		{
			Reload();
		}
	}
}
