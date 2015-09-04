using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

public class SpellXmlLoader {

	EffectAsset file;
	XmlReader reader;
	SpellEditor editor;
	private List<string> effectIDStack = new List<string>();

	Dictionary<string, SpellNode> spellNodes = new Dictionary<string, SpellNode>();
	Dictionary<int, SpellNode> attributeNodes = new Dictionary<int, SpellNode>();
	Dictionary<string, string> eventIdMapping = new Dictionary<string, string>();

	Vector2 currentPosition = new Vector2();

	private const string eventPrefix = "on-";

	public SpellEditor SpellEditor
	{
		get
		{
			return editor;
		}
	}

	public SpellNode GetNode(string id)
	{
		if (spellNodes.ContainsKey(id))
		{
			return spellNodes[id];
		}
		else 
		{
			return null;
		}
	}
	
	public string GetEventName(string id)
	{
		if (eventIdMapping.ContainsKey(id))
		{
			return eventIdMapping[id];
		}
		else 
		{
			return null;
		}
	}

	public SpellXmlLoader(EffectAsset file, SpellEditor editor)
	{
		this.file = file;
		this.editor = editor;
	}
	
	public void XmlReaderError(string message)
	{
		Debug.LogError(file.name + " line " + ((IXmlLineInfo)reader).LineNumber + ": " + message, file);
	}

	public SpellNodeType GetBuiltInType(string name)
	{
		if (editor.BuiltInTypes.BuiltInTypes.ContainsKey(name))
		{
			return editor.BuiltInTypes.BuiltInTypes[name];
		}
		else
		{
			return null;
		}
	}

	public SpellNodeType GetFunctionType(string name)
	{
		if (editor.EffectTypes.Functions.ContainsKey(name))
		{
			return editor.EffectTypes.Functions[name];
		}
		else
		{
			return null;
		}
	}

	private Vector2 ParsePosition(string positionText)
	{
		if (positionText != null)
		{
			string[] elements = positionText.Split(',');
			currentPosition = new Vector2(float.Parse(elements[0]), float.Parse(elements[1]));
		}
		else
		{
			currentPosition += new Vector2(50.0f, 50.0f);
		}
		return currentPosition;
	}

	public void ParseEvent(SpellNode spellNode)
	{
		string eventName = reader.Name;
		SpellNodeConnector currentEventOutput = spellNode.GetEventConnector(eventName);

		if (currentEventOutput == null)
		{
			XmlReaderError("spell node named " + spellNode.Type.Name + " does not have event named " + eventName);
			reader.Skip();
		}
		else
		{
			string id = reader.GetAttribute("id");

			if (id != null)
			{
				spellNodes[id] = spellNode;
				eventIdMapping[id] = eventName;
			}

			if (reader.IsEmptyElement)
			{
				reader.Read();
				return;
			}

			effectIDStack.Add(id);

			reader.Read();

			int idCount = 1;

			while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					SpellNode childEvent = ParseEffect();

					++idCount;

					if (childEvent != null)
					{
						editor.AddConnection(currentEventOutput, childEvent.InConnector);
						currentEventOutput = childEvent.OutConnector;
					}
				}
				else
				{
					reader.Read();
				}
			}

			// remove the ids for the event and all of its children
			effectIDStack.RemoveRange(effectIDStack.Count - idCount, idCount);

			reader.Read();
		}
	}

	private List<int> ParseIDList(string idList)
	{
		if (idList == null || idList.Length == 0)
		{
			return null;
		}
		else
		{
			List<int> result = new List<int>();

			foreach (string id in idList.Split(','))
			{
				result.Add(int.Parse(id));
			}

			return result;
		}
	}

	private void SetAttributePositions(string attributeList, SpellNodeConnector connector)
	{
		if (attributeList == null)
		{
			return;
		}

		string[] stringList = attributeList.Split(';');
		int index = 0;

		SpellNodeIterator iterator = new SpellNodeIterator();
		iterator.SetExpressionNodeCallback( delegate(SpellNode expressionNode) {
			if (index < stringList.Length)
			{
				expressionNode.Position = ParsePosition(stringList[index]);
				++index;
			}
		});
		iterator.IterateOverExpressionNodes(connector);
	}

	public bool HasExistingAttribute(int id)
	{
		return attributeNodes.ContainsKey(id);
	}

	public SpellNode AddAttributeNode(SpellNodeType type, int id)
	{
		if (id > 0 && attributeNodes.ContainsKey(id))
		{
			return attributeNodes[id];
		}
		else
		{
			SpellNode result = editor.AddNode(type, Vector2.zero);
			if (id > 0)
			{
				attributeNodes[id] = result;
			}
			return result;
		}
	}

	public SpellNode ParseEffect()
	{
		string typeName = reader.Name;

		if (!editor.EffectTypes.SpellTypes.ContainsKey(typeName))
		{
			XmlReaderError("could not find node type " + typeName);
			reader.Skip();
			return null;
		}
		else
		{
			SpellNodeType spellType = editor.EffectTypes.SpellTypes[typeName];
			string id = reader.GetAttribute("id");
			Vector2 position = ParsePosition(reader.GetAttribute("editor:position"));
			SpellNode newNode = editor.AddNode(spellType, position);
			
			effectIDStack.Add(id);
			
			if (id != null)
			{
				spellNodes[id] = newNode;
			}

			if (reader.IsEmptyElement)
			{
				reader.Read();
				return newNode;
			}

			reader.Read();

			while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					if (reader.Name.Length > eventPrefix.Length && reader.Name.Substring(0, 3) == eventPrefix)
					{
						ParseEvent(newNode);
					}
					else
					{
						SpellNodeConnector startingConnector = newNode.GetInputConnector(reader.Name);

						if (startingConnector == null)
						{
							XmlReaderError("no input named " + reader.Name + " on node " + newNode.Type.Name);
							reader.Skip();
						}
						else
						{
							try
							{
								string positionList = reader.GetAttribute("editor:positionList");
								string attributeIDList = reader.GetAttribute("editor:idList");
								EffectPropertyParser propertyParser = new EffectPropertyParser(reader.ReadElementContentAsString(), effectIDStack);
								SpellAttributeBuilder attributeBuilder = new SpellAttributeBuilder(this, ParseIDList(attributeIDList), startingConnector);
								EffectProperty property = propertyParser.Parse();
								property.Accept(attributeBuilder);
								SetAttributePositions(positionList, startingConnector);
							}
							catch (EffectPropertyParseException e)
							{
								XmlReaderError("Error parsing attribute " + reader.NodeType + " error: " + e.ToString());
							}
						}
					}
				}
				else
				{
					reader.Read();
				}
			}
			reader.Read();

			// the owner is responsible to ids from the stack

			return newNode;
		}
	}

	public void Load()
	{
		using (reader = XmlReader.Create(new StringReader(file.xmlText)))
		{
			while (reader.NodeType != XmlNodeType.Element)
			{
				reader.Read();
			}

			ParseEffect();
			effectIDStack.RemoveAt(effectIDStack.Count - 1);
		}
	}
}
