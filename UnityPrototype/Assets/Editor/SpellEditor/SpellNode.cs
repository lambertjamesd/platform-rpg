using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public enum ConnectorState
{
	Default,
	Hover,
	Selected,
	Valid,
	Invalid
}

public class SpellNodeConnector
{
	private SpellNodeConnectorType type;
	private Vector2 position;
	private SpellNode parent;
	private ConnectorState state = ConnectorState.Default;

	public const float SelectRadius = 8.0f;

	public SpellNodeConnector(SpellNodeConnectorType type, Vector2 position, SpellNode parent)
	{
		this.type = type;
		this.position = position;
		this.parent = parent;
	}

	public bool CanConnectTo(SpellNodeConnector other)
	{
		if (type.IsInput)
		{
			return !other.Type.IsInput && (type.Type == other.Type.Type || type.Type == "Any" || other.Type.Type == "Any");
		}
		else if (other.type.IsInput)
		{
			return type.Type == other.Type.Type || type.Type == "Any" || other.Type.Type == "Any";
		}
		else
		{
			return false;
		}
	}

	public void Draw()
	{
		parent.DrawConnectorNode(position, SpellNode.GetTypeColor(type.Type), state);
	}

	public SpellNodeConnectorType Type
	{
		get
		{
			return type;
		}
	}

	public ConnectorState State
	{
		get
		{
			return state;
		}
		set
		{
			state = value;
		}
	}

	public bool Collides(Vector2 nodeSpacePos)
	{
		return (nodeSpacePos - position).sqrMagnitude < SelectRadius * SelectRadius;
	}

	public Vector2 Position
	{
		get
		{
			return position + parent.Position;
		}
	}

	public Rect BoundingRect
	{
		get
		{
			Vector2 pos = Position;
			return new Rect(pos.x - SelectRadius, pos.y - SelectRadius, SelectRadius * 2.0f, SelectRadius * 2.0f);
		}
	}

	public SpellNode Parent
	{
		get
		{
			return parent;
		}
	}

	public SpellConnection Connection
	{
		get
		{
			return parent.ParentEditor.GetFirstConnection(this);
		}
	}

	// returns the first connected node
	public SpellNodeConnector ConnectedTo
	{
		get
		{
			SpellConnection connecton = parent.ParentEditor.GetFirstConnection(this);

			if (connecton == null)
			{
				return null;
			}

			if (type.IsInput)
			{
				return connecton.Source;
			}
			else
			{
				return connecton.Destination;
			}
		}
	}

	public string DefaultValue { get; set; }
}

public class SpellNode
{
	private SpellNodeType type;

	Rect position;

	private Texture nodeConnector;
	private Texture nodeConnectorOuter;

	private const float connectorSpacing = 16.0f;
	private const float nodeConnectorPosition = 8.0f;
	
	private const float firstConnectionPosition = 40.0f;

	private const float textMargin = 10.0f;
	private const float innerMargin = 10.0f;
	private const float bottomMargin = 10.0f;
	
	private const float minWidth = 100.0f;
	private const float minHeight = 60.0f;

	private const float minInputWidth = 100.0f;
	private float inputFieldWidth = 0.0f;
	private int inConnectorIndex = 0;
	private int outConnectorIndex = 0;
	private int inputConnectorStartIndex = 0;
	private int outputConnectorStartIndex = 0;

	private int[] eventConnectorStartIndex = null;

	private GUIStyle style = new GUIStyle();
	private GUIStyle descriptionLabelStyle = new GUIStyle();

	private List<SpellNodeConnector> connectors = new List<SpellNodeConnector>();
	private bool allowDrag = true;
	private SpellEditor parent;

	private bool DisableInteraction { get; set; }
	private bool IsOrphaned { get; set; }

	public SpellNode(SpellNodeType type, SpellEditor parent)
	{
		this.type = type;
		this.parent = parent;

		position = new Rect(10, 10, 100, 100);
		
		nodeConnector = Resources.Load<Texture>("SpellEditor/NodeConnectorInner");
		nodeConnectorOuter = Resources.Load<Texture>("SpellEditor/NodeConnectorOuter");

		RecalculateSize();
		AddConnectors();

		descriptionLabelStyle.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);
		descriptionLabelStyle.alignment = TextAnchor.MiddleCenter;
	}

	private Vector2 TextSize(string text)
	{
		return style.CalcSize(new GUIContent(text));
	}

	private void MeasureType(SpellNodeConnectorType type, ref float currentY, ref float width)
	{
		float lineWidth = textMargin * 2.0f + 
			TextSize(type.Name).x;

		width = Mathf.Max(lineWidth, width);
		currentY += connectorSpacing;

		if (type.IsInput && UseEditor(type.Type))
		{
			currentY += connectorSpacing;
			width = Mathf.Max(minInputWidth, width);
		}
	}
	
	private void RecalculateSize()
	{
		inputFieldWidth = 0.0f;

		float currentY = firstConnectionPosition;

		for (int i = 0; i < type.InputCount; ++i)
		{
			MeasureType(type.GetInput(i), ref currentY, ref inputFieldWidth);
		}
		
		float outputWidth = 0.0f;
		float inputYEnd = currentY;
		currentY = firstConnectionPosition;

		for (int i = 0; i < type.OutputCount; ++i)
		{
			MeasureType(type.GetOutput(i), ref currentY, ref outputWidth);
		}

		currentY = Mathf.Max(currentY, inputYEnd);
		float actualWidth = inputFieldWidth + outputWidth;

		if (inputFieldWidth != 0.0f  && outputWidth != 0.0f)
		{
			actualWidth += innerMargin;
		}

		actualWidth = Mathf.Max(minWidth, actualWidth);
		
		for (int i = 0; i < type.EventCount; ++i)
		{
			currentY += connectorSpacing;
			
			SpellNodeEventType nodeEvent = type.GetEvent(i);
			MeasureType(nodeEvent.Type, ref currentY, ref actualWidth);

			for (int j = 0; j < nodeEvent.OutputCount; ++j)
			{
				MeasureType(nodeEvent.GetOutput(i), ref currentY, ref actualWidth);
			}
		}

		currentY += bottomMargin;

		position.width = actualWidth;
		position.height = Mathf.Max(minHeight, currentY);
	}

	private void AddConnectors()
	{
		float currentY = firstConnectionPosition + connectorSpacing * 0.5f;

		if (type.InConnection != null)
		{
			inConnectorIndex = connectors.Count;
			connectors.Add(new SpellNodeConnector(type.InConnection, new Vector2(0.0f, nodeConnectorPosition), this));
		}
		
		if (type.OutConnection != null)
		{
			outConnectorIndex = connectors.Count;
			connectors.Add(new SpellNodeConnector(type.OutConnection, new Vector2(position.width, nodeConnectorPosition), this));
		}

		inputConnectorStartIndex = connectors.Count;

		for (int i = 0; i < type.InputCount; ++i)
		{
			connectors.Add(new SpellNodeConnector(type.GetInput(i), new Vector2(0.0f, currentY), this));

			if (UseEditor(type.GetInput(i).Type))
			{
				currentY += connectorSpacing;
			}

			currentY += connectorSpacing;
		}

		outputConnectorStartIndex = connectors.Count;
		
		float eventStart = currentY;
		currentY = firstConnectionPosition + connectorSpacing * 0.5f;
		
		for (int i = 0; i < type.OutputCount; ++i)
		{
			connectors.Add(new SpellNodeConnector(type.GetOutput(i), new Vector2(position.width, currentY), this));
			currentY += connectorSpacing;
		}

		currentY = Mathf.Max(currentY, eventStart);

		eventConnectorStartIndex = new int[type.EventCount];

		for (int i = 0; i < type.EventCount; ++i)
		{
			eventConnectorStartIndex[i] = connectors.Count;

			currentY += connectorSpacing;
			SpellNodeEventType nodeEvent = type.GetEvent(i);
			connectors.Add(new SpellNodeConnector(nodeEvent.Type, new Vector2(position.width, currentY), this));
			currentY += connectorSpacing;

			for (int j = 0; j < nodeEvent.OutputCount; ++j)
			{
				connectors.Add(new SpellNodeConnector(nodeEvent.GetOutput(j), new Vector2(position.width, currentY), this));
				currentY += connectorSpacing;
			}
		}
	}

	public Vector2 Position
	{
		get
		{
			return new Vector2(position.x, position.y);
		}

		set
		{
			position.x = value.x;
			position.y = value.y;
		}
	}

	public Vector2 Size
	{
		get
		{
			return new Vector2(position.width, position.height);
		}
	}

	public int ConnectorCount
	{
		get
		{
			return connectors.Count;
		}
	}

	public SpellNodeConnector GetConnector(int index)
	{
		return connectors[index];
	}

	public SpellNodeType Type
	{
		get
		{
			return type;
		}
	}

	public SpellEditor ParentEditor
	{
		get
		{
			return parent;
		}
	}

	public SpellNode Sibling
	{
		get
		{
			if (type.OutConnection == null)
			{
				return null;
			}

			SpellConnection connection = parent.GetFirstConnection(connectors[outConnectorIndex]);

			if (connection != null)
			{
				return connection.Destination.Parent;
			}
			else
			{
				return null;
			}
		}
	}

	public SpellNode GetEventTarget(int eventIndex)
	{
		SpellConnection connection = parent.GetFirstConnection(connectors[eventConnectorStartIndex[eventIndex]]);
		
		if (connection != null)
		{
			return connection.Destination.Parent;
		}
		else
		{
			return null;
		}
	}

	public SpellNodeConnector InConnector
	{
		get
		{
			if (type.InConnection != null)
			{
				return connectors[inConnectorIndex];
			}
			else
			{
				return null;
			}
		}
	}
	
	public SpellNodeConnector OutConnector
	{
		get
		{
			if (type.OutConnection != null)
			{
				return connectors[outConnectorIndex];
			}
			else
			{
				return null;
			}
		}
	}

	private SpellNodeConnector GetConnectorByName(string name, int startIndex, int endIndex)
	{
		for (int i = startIndex; i < endIndex; ++i)
		{
			if (connectors[i].Type.Name == name)
			{
				return connectors[i];
			}
		}

		return null;
	}
	
	public SpellNodeConnector GetInputConnector(int index)
	{
		return connectors[inputConnectorStartIndex + index];
	}

	public SpellNodeConnector GetInputConnector(string name)
	{
		return GetConnectorByName(name, inputConnectorStartIndex, inputConnectorStartIndex + type.InputCount);
	}

	public SpellNodeConnector GetOutputConnector(int index)
	{
		return connectors[outputConnectorStartIndex + index];
	}

	public SpellNodeConnector GetOutputConnector(string name)
	{
		return GetConnectorByName(name, outputConnectorStartIndex, outputConnectorStartIndex + type.OutputCount);
	}

	public SpellNodeConnector GetEventConnector(int index)
	{
		return connectors[eventConnectorStartIndex[index]];
	}

	public SpellNodeConnector GetEventConnector(string name)
	{
		for (int i = 0; i < eventConnectorStartIndex[i]; ++i)
		{
			if (connectors[eventConnectorStartIndex[i]].Type.Name == name)
			{
				return connectors[eventConnectorStartIndex[i]];
			}
		}

		return null;
	}

	public SpellNodeConnector GetEventOutputConnector(string eventName, string propertyName)
	{
		for (int i = 0; i < eventConnectorStartIndex[i]; ++i)
		{
			if (connectors[eventConnectorStartIndex[i]].Type.Name == eventName)
			{
				int propertyIndexStart = eventConnectorStartIndex[i] + 1;
				return GetConnectorByName(propertyName, propertyIndexStart, propertyIndexStart + type.GetEvent(i).OutputCount);
			}
		}
		
		return null;
	}

	public void DrawWindow(int windowID, bool allowDrag)
	{
		Color lastColor = GUI.color;

		Color color = IsOrphaned ? new Color(1.0f, 0.5f, 0.5f) : Color.white;

		if (DisableInteraction)
		{
			color.a = 0.5f;
		}

		GUI.color = color;

		this.allowDrag = allowDrag;
		position = GUI.Window(windowID, position, DrawNodeWindow, new GUIContent(type.Name, type.Description));   // Updates the Rect's when these are dragged

		GUI.color = lastColor;

		position.x = Mathf.Max(position.x, 0.0f);
		position.y = Mathf.Max(position.y, 0.0f);
	}

	public void DrawNodes()
	{
		foreach (SpellNodeConnector connector in connectors)
		{
			connector.Draw();
		}
	}
	
	public void DrawConnectorNode(Vector2 drawPosition, Color color, ConnectorState state)
	{
		Rect drawRect = new Rect(position.x + drawPosition.x - nodeConnector.width * 0.5f, position.y + drawPosition.y - nodeConnector.height * 0.5f,
	                         nodeConnector.width, nodeConnector.height);
		
		Color previousColor = GUI.color;
		GUI.color = color;
		
		GUI.DrawTexture(drawRect, nodeConnector);

		switch (state)
		{
		case ConnectorState.Default:
			GUI.color = Color.black;
			break;
		case ConnectorState.Hover:
			GUI.color = Color.white;
			break;
		case ConnectorState.Selected:
			GUI.color = Color.green;
			break;
		case ConnectorState.Valid:
			GUI.color = Color.green;
			break;
		case ConnectorState.Invalid:
			GUI.color = Color.red;
			break;
		}
		GUI.DrawTexture(drawRect, nodeConnectorOuter);
		
		GUI.color = previousColor;
	}

	public SpellNodeConnector GetConnector(Vector2 mousePosition)
	{
		if (mousePosition.x < position.x - SpellNodeConnector.SelectRadius ||
		    mousePosition.y < position.y - SpellNodeConnector.SelectRadius ||
		    mousePosition.x > position.xMax + SpellNodeConnector.SelectRadius ||
		    mousePosition.y > position.yMax + SpellNodeConnector.SelectRadius)
		{
			return null;
		}

		mousePosition -= new Vector2(position.x, position.y);

		foreach (SpellNodeConnector connector in connectors)
		{
			if (connector.Collides(mousePosition))
			{
				return connector;
			}
		}

		return null;
	}

	public SpellNodeConnector GetEventConnector(SpellNodeConnector connectorCheck)
	{
		for (int i = 0; i < eventConnectorStartIndex.Length; ++i)
		{
			int eventIndexStart = eventConnectorStartIndex[i];
			for (int j = 1; j <= Type.GetEvent(i).OutputCount; ++j)
			{
				if (connectors[j + eventIndexStart] == connectorCheck)
				{
					return connectors[eventIndexStart];
				}
			}
		}

		return null;
	}

	private void DrawRightAlignedText(float yPos, string text, string tooltip)
	{
		Vector2 textSize = TextSize(text);
		GUI.Label(new Rect(position.width - textSize.x - textMargin, yPos, textSize.x + 20.0f, connectorSpacing), new GUIContent(text, tooltip));
	}
	
	public void ContextMenuCallback(object obj) {
		if (obj is string && ((string)obj) == "delete")
		{
			parent.RemoveNode(this);
		}
	}
	
	private void DrawNodeWindow(int id) {
		Event currentEvent = Event.current;

		if (currentEvent.type == EventType.MouseDown)
		{
			parent.SetSelectedSpellNode(this);

			if (currentEvent.button == 1)
			{
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Delete"), false, ContextMenuCallback, "delete");
				menu.ShowAsContext();
			}
		}

		if (allowDrag)
		{
			GUI.DragWindow(new Rect(0, 0, position.width, connectorSpacing));
		}

		GUI.Label(new Rect(0, firstConnectionPosition - connectorSpacing * 1.5f, position.width, connectorSpacing), new GUIContent("description", type.Description), descriptionLabelStyle);
		
		float currentY = firstConnectionPosition;
		
		for (int i = 0; i < type.InputCount; ++i)
		{
			SpellNodeConnectorType connectorType = type.GetInput(i);

			GUI.Label(new Rect(textMargin, currentY, 100.0f, connectorSpacing), new GUIContent(connectorType.Name, connectorType.Description));

			if  (UseEditor(connectorType.Type))
			{
				currentY += connectorSpacing;

				SpellNodeConnector connector = connectors[inputConnectorStartIndex + i];
				Rect editorRect = new Rect(textMargin, currentY, inputFieldWidth, connectorSpacing);

				if (connectorType.Type == "bool")
				{
					bool boolValue = GUI.Toggle(editorRect,  (connector.DefaultValue ?? "") == "true", "default");
					connector.DefaultValue = boolValue ? "true" : "false";
				}
				else if (connectorType.Type == "Prefab")
				{
					int lastValue = -1;
					GameObject lastGameObject = null;

					if (connector.DefaultValue != null && int.TryParse(connector.DefaultValue, out lastValue))
					{
						lastGameObject = parent.CurretlyOpenSpell.GetPrefab(lastValue);
					}

					GameObject nextGameObject = EditorGUI.ObjectField(editorRect, lastGameObject, typeof(GameObject), false) as GameObject;

					int newValue = parent.CurretlyOpenSpell.GetPrefabIndex(nextGameObject);

					parent.CurretlyOpenSpell.AddReference(newValue);
					parent.CurretlyOpenSpell.RemoveReference(lastValue);

					connector.DefaultValue = newValue == -1 ? null : newValue.ToString();
				}
				else
				{
					connector.DefaultValue = GUI.TextField(editorRect, connector.DefaultValue ?? "");
				}
			}

			currentY += connectorSpacing;
		}

		float eventStartY = currentY;
		currentY = firstConnectionPosition;
		
		for (int i = 0; i < type.OutputCount; ++i)
		{
			DrawRightAlignedText(currentY, type.GetOutput(i).Name, type.GetOutput(i).Description);
			currentY += connectorSpacing;
		}

		currentY = Mathf.Max(eventStartY, currentY);

		for (int i = 0; i < type.EventCount; ++i)
		{
			currentY = DrawNodeEvent(type.GetEvent(i), currentY);
		}
	}

	private float DrawNodeEvent(SpellNodeEventType nodeEvent, float currentY)
	{
		Handles.color = Color.black;
		Handles.DrawLine(new Vector3(0.0f, currentY + connectorSpacing * 0.5f, 0.0f), new Vector3(position.width, currentY + connectorSpacing * 0.5f, 0.0f));

		currentY += connectorSpacing;

		DrawRightAlignedText(currentY, nodeEvent.Name, nodeEvent.Description);
		
		currentY += connectorSpacing;
		
		for (int i = 0; i < nodeEvent.OutputCount; ++i)
		{
			DrawRightAlignedText(currentY, nodeEvent.GetOutput(i).Name, nodeEvent.GetOutput(i).Description);
			currentY += connectorSpacing;
		}

		return currentY;
	}

	public static bool UseEditor(string type)
	{
		return type == "string" || type == "bool" || type == "int" || type == "float" || type == "Prefab";
	}

	public static Color GetTypeColor(string type)
	{
		switch (type)
		{
		case "event":
			return new Color(1.0f, 0.5f, 0.0f);
		case "IEffect":
			return new Color(0.5f, 0.25f, 0.0f);
		case "Vector3":
			return new Color(0.0f, 0.0f, 1.0f);
		case "bool":
			return new Color(0.0f, 0.75f, 0.75f);
		case "float":
			return new Color(0.0f, 0.75f, 0.0f);
		case "int":
			return new Color(0.0f, 0.5f, 0.25f);
		case "bitMask":
			return new Color(0.5f, 0.5f, 0.0f);
		case "string":
			return new Color(0.75f, 0.0f, 0.6f);
		case "GameObject":
			return new Color(1.0f, 0.0f, 0.0f);
		case "Prefab":
			return new Color(0.5f, 0.0f, 0.0f);
		case "List":
			return new Color(0.5f, 0.5f, 0.5f);
		case "Any":
			return new Color(1.0f, 1.0f, 1.0f);
		case "Color":
			return new Color(0.0f, 0.0f, 0.5f);
		}

		return Color.black;
	}
}
