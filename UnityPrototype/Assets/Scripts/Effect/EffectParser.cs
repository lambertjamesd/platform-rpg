using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

public class EffectParser {

	private EffectAsset textSource;
	private string source;
	private XmlReader reader;

	private List<string> idStack = new List<string>();

	public EffectParser(EffectAsset textSource)
	{
		this.textSource = textSource;
		source = textSource.xmlText;
	}

	public EffectParser(string source)
	{
		this.source = source;
	}

	private void CheckTagMatchup(string expectedTag)
	{
		if (expectedTag != reader.Name)
		{
			Debug.LogWarning("Mismatched xml tags. Open tag: " + expectedTag + ", End tag: " + reader.Name, textSource);
		}
	}

	private List<EffectDefinition> ParseEvent()
	{
		List<EffectDefinition> result = new List<EffectDefinition>();

		string eventName = reader.Name;

		idStack.Add(reader.GetAttribute("id"));

		int definitionCount = 0;

		if (!reader.IsEmptyElement)
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.EndElement)
				{
					CheckTagMatchup(eventName);
					break;
				}
				else if (reader.NodeType == XmlNodeType.Element)
				{
					result.Add(ParseDefinition());
					++definitionCount;
				}
			}
		}

		// remove the ids from all of the effects
		// removing the ids here allows for effects to reference
		// their siblings
		idStack.RemoveRange(idStack.Count - 1 - definitionCount, definitionCount);

		idStack.RemoveAt(idStack.Count - 1);

		return result;
	}

	private EffectProperty ParseProperty()
	{
		string type = reader.GetAttribute("type");
		string stringResult = reader.ReadElementContentAsString().Trim();

		if (type == "string")
		{
			return new EffectConstantProperty<string>(stringResult.ToString());
		}
		else
		{
			EffectProperty result = null;

			if (stringResult != null && stringResult.Length > 0)
			{
				try
				{
					EffectPropertyParser propertyParser = new EffectPropertyParser(stringResult, idStack);
					result = propertyParser.Parse();
				}
				catch (EffectPropertyParseException exception)
				{
					Debug.LogError("Error at line " + ((IXmlLineInfo)reader).LineNumber + " : " + exception.ToString() + " in string '" + stringResult + "'", textSource);
				}
			}

			return result;
		}
	}

	private EffectDefinition ParseDefinition()
	{
		EffectDefinition result = new EffectDefinition(reader.Name, textSource);

		// add the effect id to the list
		// the id is removed when parsing events
		idStack.Add(reader.GetAttribute("id"));

		if (!reader.IsEmptyElement)
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.EndElement)
				{
					CheckTagMatchup(result.EffectType);
					break;
				}
				else if (reader.NodeType == XmlNodeType.Element)
				{
					if (reader.Name.Length > 3 && reader.Name.Substring(0, 3) == "on-")
					{
						result.AddChildren(reader.Name.Substring(3), ParseEvent());
					}
					else
					{
						string propertyName = reader.Name;
						EffectProperty property = ParseProperty();

						if (property  != null)
						{
							result.AddProperty(propertyName, property);
						}
					}
				}
			}
		}

		return result;
	}

	public EffectDefinition Parse()
	{
		using (reader = XmlReader.Create(new StringReader(source)))
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					if (reader.Name == "caster")
					{
						return ParseDefinition();
					}
					else
					{
						Debug.LogWarning("Effect type not recognized: " + reader.Name, textSource);
						return null;
					}
				}
			}
		}

		return null;
	}
}
