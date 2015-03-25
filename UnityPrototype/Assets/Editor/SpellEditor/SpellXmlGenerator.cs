using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.IO;

public class SpellXmlGenerator {
	private int currentNodeID = 1;
	private int currentAttributeID = 1;
	private XmlWriter xmlWriter;
	private Dictionary<object, int> nodeIDs = new Dictionary<object, int>();
	private Dictionary<object, int> attributeIDs = new Dictionary<object, int>();
	private StringBuilder currentAttribute;
	private bool namespaceWritten;

	public static void WriteFile(EffectAsset asset, SpellNode rootNode)
	{
		new SpellXmlGenerator(asset, rootNode);
	}

	private SpellXmlGenerator(EffectAsset asset, SpellNode rootNode)
	{
		XmlWriterSettings settings = new XmlWriterSettings();
		settings.Indent = true;
		settings.IndentChars = "\t";

		StringWriter target = new StringWriter();

		using (xmlWriter = XmlWriter.Create(target, settings))
		{
			xmlWriter.WriteStartDocument();
			GenerateTree(rootNode);
			xmlWriter.WriteEndDocument();
		}

		asset.xmlText = target.ToString();
	}

	public XmlWriter Writer
	{
		get
		{
			return xmlWriter;
		}
	}

	public string GetNodeName(string prefix, SpellNode node)
	{
		int id = 0;

		if (!nodeIDs.ContainsKey(node))
		{
			nodeIDs[node] = id = currentNodeID;
			++currentNodeID;
		}
		else
		{
			id = nodeIDs[node];
		}

		string result = prefix + id;
		result = result.Replace("-", "");
		return result;
	}

	public int GetAttributeID(SpellNode node)
	{
		if (attributeIDs.ContainsKey(node))
		{
			return attributeIDs[node];
		}
		else
		{
			int result = currentAttributeID;
			attributeIDs[node] = result;
			++currentAttributeID;
			return result;
		}
	}

	public void GenerateTree(SpellNode node)
	{
		if (node != null)
		{
			node.Type.XmlGenerator.GenerateTree(node, this);
		}
	}
	
	public void GenerateAttributeInput(SpellNodeConnector connector)
	{
		SpellNodeConnector connection = connector.ConnectedTo;

		if (connection == null)
		{
			if (connector.DefaultValue == null || connector.DefaultValue.Length == 0)
			{
				Writer.WriteString("null");
			}
			else
			{
				try 
				{
					switch (connector.Type.Type)
					{
					case "float":
						Writer.WriteString(float.Parse(connector.DefaultValue).ToString());
						break;
					case "int":
					case "Prefab":
						Writer.WriteString(int.Parse(connector.DefaultValue).ToString() + 'i');
						break;
					case "bool":
						Writer.WriteString(connector.DefaultValue ?? "false");
						break;
					case "string":
						Writer.WriteString('"' + connector.DefaultValue.Replace("\"", "\\\"") + '"');
						break;
					default:
						Writer.WriteString("null");
						break;
					}
				}
				catch (System.Exception)
				{
					Writer.WriteString("null");
				}
			}
		}
		else
		{
			GenerateAttributeOutput(connection);
		}
	}

	public void GenerateAttributeOutput(SpellNodeConnector node)
	{
		node.Parent.Type.XmlGenerator.GenerateAttributeOutput(node, this);
	}

	public bool NamespaceWritten
	{
		get
		{
			return namespaceWritten;
		}
	}

	public void WriteNamespace()
	{
		Writer.WriteAttributeString("xmlns:editor", "text://editor");
		namespaceWritten = true;
	}

	public string GeneratePositionList(SpellNodeConnector node)
	{
		StringBuilder result = new StringBuilder();

		bool isFirst = true;

		SpellNodeIterator iterator = new SpellNodeIterator();
		iterator.SetExpressionNodeCallback( delegate(SpellNode expressionNode) {
			if (isFirst)
			{
				isFirst = false;
			}
			else
			{
				result.Append(";");
			}

			result.AppendFormat("{0},{1}", expressionNode.Position.x, expressionNode.Position.y);
		});
		iterator.IterateOverExpressionNodes(node);

		return result.ToString();
	}

	public string GenerateIDList(SpellNodeConnector node)
	{
		StringBuilder result = new StringBuilder();
		
		bool isFirst = true;
		
		SpellNodeIterator iterator = new SpellNodeIterator();
		iterator.SetExpressionNodeCallback( delegate(SpellNode expressionNode) {
			if (isFirst)
			{
				isFirst = false;
			}
			else
			{
				result.Append(",");
			}

			result.Append(GetAttributeID(expressionNode));
		});
		iterator.IterateOverExpressionNodes(node);
		
		return result.ToString();
	}
}

public abstract class SpellNodeXmlGenerator {
	public abstract void GenerateTree(SpellNode node, SpellXmlGenerator generator);
	public abstract void GenerateAttributeOutput(SpellNodeConnector nodeConnector, SpellXmlGenerator generator);
}

public class SpellNodeEffectXmlGenerator : SpellNodeXmlGenerator {

	public SpellNodeEffectXmlGenerator()
	{

	}

	public override void GenerateTree(SpellNode node, SpellXmlGenerator generator)
	{
		generator.Writer.WriteStartElement(node.Type.Name);
		generator.Writer.WriteAttributeString("id", generator.GetNodeName(node.Type.Name, node));
		generator.Writer.WriteAttributeString("editor:position", string.Format("{0},{1}", node.Position.x, node.Position.y));

		if (!generator.NamespaceWritten)
		{
			generator.WriteNamespace();
		}

		for (int i = 0; i < node.Type.InputCount; ++i)
		{
			SpellNodeConnector connnector = node.GetInputConnector(i);
			if (connnector.ConnectedTo != null || (connnector.DefaultValue != null && connnector.DefaultValue.Length > 0))
			{
				generator.Writer.WriteStartElement(connnector.Type.Name);
				string positionList = generator.GeneratePositionList(connnector);
				if (positionList.Length > 0)
				{
					generator.Writer.WriteAttributeString("editor:positionList", positionList);
				}
				string idList = generator.GenerateIDList(connnector);
				if (idList.Length > 0)
				{
					generator.Writer.WriteAttributeString("editor:idList", idList);
				}
				generator.GenerateAttributeInput(connnector);
				generator.Writer.WriteEndElement();
			}
		}

		for (int i = 0; i < node.Type.EventCount; ++i)
		{
			SpellNodeEventType eventType = node.Type.GetEvent(i);

			SpellNode eventNode = node.GetEventTarget(i);

			if (eventNode != null)
			{
				generator.Writer.WriteStartElement(eventType.Name);
				generator.Writer.WriteAttributeString("id", generator.GetNodeName(eventType.Name, node));

				generator.GenerateTree(eventNode);

				generator.Writer.WriteEndElement();
			}
		}

		generator.Writer.WriteEndElement();

		generator.GenerateTree(node.Sibling);
	}
	
	public override void GenerateAttributeOutput(SpellNodeConnector connector, SpellXmlGenerator generator)
	{
		SpellNode node = connector.Parent;

		SpellNodeConnector eventNode = node.GetEventConnector(connector);

		string prefixName = eventNode == null ? node.Type.Name : eventNode.Type.Name;
		generator.Writer.WriteString(generator.GetNodeName(prefixName, node) + "." + connector.Type.Name);
	}
}

public class SpellNodeFunctionXmlGenerator : SpellNodeXmlGenerator {
	
	public SpellNodeFunctionXmlGenerator()
	{
		
	}
	
	public override void GenerateTree(SpellNode node, SpellXmlGenerator generator)
	{
		
	}
	
	public override void GenerateAttributeOutput(SpellNodeConnector connector, SpellXmlGenerator generator)
	{
		SpellNode node = connector.Parent;

		generator.Writer.WriteString(node.Type.Name + "(");

		bool isFirst = true;

		for (int i = 0; i < node.Type.InputCount; ++i)
		{
			if (isFirst)
			{
				isFirst = false;
			}
			else
			{
				generator.Writer.WriteString(", ");
			}

			generator.GenerateAttributeInput(node.GetInputConnector(i));
		}

		generator.Writer.WriteString(")");
	}
}

public class SpellNodeBinaryOpXmlGenerator : SpellNodeXmlGenerator {

	string tokenString;

	public SpellNodeBinaryOpXmlGenerator(string tokenString)
	{
		this.tokenString = tokenString;
	}
	
	public override void GenerateTree(SpellNode node, SpellXmlGenerator generator)
	{
		
	}
	
	public override void GenerateAttributeOutput(SpellNodeConnector connector, SpellXmlGenerator generator)
	{
		SpellNode node = connector.Parent;
		
		generator.Writer.WriteString("(");
		
		generator.GenerateAttributeInput(node.GetInputConnector(0));
		generator.Writer.WriteString(tokenString);
		generator.GenerateAttributeInput(node.GetInputConnector(1));

		generator.Writer.WriteString(")");
	}
}

public class SpellNodeUnaryOpXmlGenerator : SpellNodeXmlGenerator {
	
	string tokenString;
	
	public SpellNodeUnaryOpXmlGenerator(string tokenString)
	{
		this.tokenString = tokenString;
	}
	
	public override void GenerateTree(SpellNode node, SpellXmlGenerator generator)
	{
		
	}
	
	public override void GenerateAttributeOutput(SpellNodeConnector connector, SpellXmlGenerator generator)
	{
		SpellNode node = connector.Parent;

		generator.Writer.WriteString(tokenString);
		generator.GenerateAttributeInput(node.GetInputConnector(0));
	}
}