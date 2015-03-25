using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

public class SpellParameter {
	private string name;
	private string type;

	public SpellParameter(string name, string type)
	{
		this.name = name;
		this.type = type;
	}

	public string Name
	{
		get
		{
			return name;
		}
	}

	public string Type
	{
		get
		{
			return type;
		}
	}

	public SpellNodeConnectorType BuildConnector(bool supportMulti)
	{
		SpellNodeConnectorType result = new SpellNodeConnectorType(name, type, supportMulti);
		result.Description = Description;
		return result;
	}

	public string Description {	get; set; }
}

public class SpellInputConfiguration {
	private List<SpellParameter> parameters = new List<SpellParameter>();

	public SpellInputConfiguration()
	{

	}
	
	public SpellInputConfiguration(SpellInputConfiguration parent)
	{
		foreach (SpellParameter parameter in parent.parameters)
		{
			AddParameter (parameter);
		}
	}

	public void AddParameter(SpellParameter parameter)
	{
		parameters.Add(parameter);
	}

	public int ParameterCount
	{
		get
		{
			return parameters.Count;
		}
	}

	public SpellParameter GetParameter(int i)
	{
		return parameters[i];
	}

	public SpellNodeConnectorType BuildConnector(int i)
	{
		return parameters[i].BuildConnector(false);
	}
}

public class SpellEventConfiguration {
	private string name;
	private List<SpellParameter> parameters = new List<SpellParameter>();
	
	public SpellEventConfiguration(string name)
	{
		this.name = name;
	}
	
	public SpellEventConfiguration(string name, SpellOutputConfiguration parent)
	{
		this.name = name;

		for (int i = 0; i < parent.ParameterCount; ++i)
		{
			AddParameter(parent.GetParameter(i));
		}
	}

	public string Name
	{
		get
		{
			return name;
		}
	}
	
	public void AddParameter(SpellParameter parameter)
	{
		parameters.Add(parameter);
	}
	
	public int ParameterCount
	{
		get
		{
			return parameters.Count;
		}
	}
	
	public SpellParameter GetParameter(int i)
	{
		return parameters[i];
	}

	public SpellNodeEventType BuildEvent()
	{
		SpellNodeEventType result = new SpellNodeEventType(name);

		foreach (SpellParameter parameter in parameters)
		{
			result.AddOutput(parameter.BuildConnector(true));
		}

		result.Description = Description;

		return result;
	}
	
	public string Description {	get; set; }
}

public class SpellOutputConfiguration {
	private List<SpellParameter> parameters = new List<SpellParameter>();
	private List<SpellEventConfiguration> events = new List<SpellEventConfiguration>();
	
	public SpellOutputConfiguration()
	{
		
	}
	
	public SpellOutputConfiguration(SpellOutputConfiguration parent)
	{
		foreach (SpellParameter parameter in parent.parameters)
		{
			AddParameter(parameter);
		}

		foreach (SpellEventConfiguration eventConfig in parent.events)
		{
			AddEvent(eventConfig);
		}
	}
	
	public void AddParameter(SpellParameter parameter)
	{
		parameters.Add(parameter);
	}
	
	public int ParameterCount
	{
		get
		{
			return parameters.Count;
		}
	}
	
	public SpellParameter GetParameter(int i)
	{
		return parameters[i];
	}
	
	public SpellNodeConnectorType BuildConnector(int i)
	{
		return parameters[i].BuildConnector(true);
	}
	
	public void AddEvent(SpellEventConfiguration parameter)
	{
		events.Add(parameter);
	}
	
	public int EventCount
	{
		get
		{
			return events.Count;
		}
	}
	
	public SpellEventConfiguration GetEvent(int i)
	{
		return events[i];
	}

	public SpellNodeEventType BuildEvent(int i)
	{
		return events[i].BuildEvent();
	}
}

public class SpellTypeParser {

	private Dictionary<string, SpellInputConfiguration> inputConfig = new Dictionary<string, SpellInputConfiguration>();
	private Dictionary<string, SpellOutputConfiguration> outputConfig = new Dictionary<string, SpellOutputConfiguration>();
	private SortedDictionary<string, SpellNodeType> spellTypes = new SortedDictionary<string, SpellNodeType>();
	private SortedDictionary<string, SpellNodeType> functions = new SortedDictionary<string, SpellNodeType>();

	private TextAsset source;
	
	private void XmlReaderError(XmlReader reader, string message)
	{
		Debug.LogError(source.name + " line " + ((IXmlLineInfo)reader).LineNumber + ": " + message, source);
	}
	
	private SpellParameter ParseParameter(XmlReader reader)
	{
		string name = null, type = null, description = null;

		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "parameter")
			{
				break;
			}

			if (reader.NodeType == XmlNodeType.Element)
			{
				if (reader.Name == "name")
				{
					name = reader.ReadElementContentAsString().Trim();
				}
				else if (reader.Name == "type")
				{
					type = reader.ReadElementContentAsString().Trim();
				}
				else if (reader.Name == "description")
				{
					description = reader.ReadElementContentAsString().Trim();
				}
			}
		}

		if (name == null || name.Length == 0)
		{
			XmlReaderError(reader, "no parameter name specified");
		}

		if (type == null || type.Length == 0)
		{
			XmlReaderError(reader, "no parameter type specified");
		}

		SpellParameter result = new SpellParameter(name, type);
		result.Description = description;
		return result;
	}

	private SpellInputConfiguration ParseInputConfiguration(XmlReader reader)
	{
		SpellInputConfiguration result;

		string parent = reader.GetAttribute("parent");

		if (parent == null)
		{
			result = new SpellInputConfiguration();
		}
		else if (inputConfig.ContainsKey(parent))
		{
			result = new SpellInputConfiguration(inputConfig[parent]);
		}
		else
		{
			result = new SpellInputConfiguration();
			XmlReaderError(reader, "could not find input-configuration named '" + parent + "'");
		}
		
		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "input-configuration")
			{
				break;
			}
			else if (reader.NodeType == XmlNodeType.Element && reader.Name == "parameter")
			{
				result.AddParameter(ParseParameter(reader));
			}
		}
		
		return result;
	}

	private SpellOutputConfiguration ParseOutputConfiguration(XmlReader reader)
	{
		SpellOutputConfiguration result;
		
		string parent = reader.GetAttribute("parent");
		
		if (parent == null)
		{
			result = new SpellOutputConfiguration();
		}
		else if (outputConfig.ContainsKey(parent))
		{
			result = new SpellOutputConfiguration(outputConfig[parent]);
		}
		else
		{
			result = new SpellOutputConfiguration();
			XmlReaderError(reader, "could not find output-configuration named '" + parent + "'");
		}
		
		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "output-configuration")
			{
				break;
			}
			else if (reader.NodeType == XmlNodeType.Element && reader.Name == "parameter")
			{
				result.AddParameter(ParseParameter(reader));
			}
			else if (reader.NodeType == XmlNodeType.Element && reader.Name == "event")
			{
				SpellEventConfiguration eventConfig = ParseEvent(reader);

				if (eventConfig != null)
				{
					result.AddEvent(eventConfig);
				}
			}
		}

		return result;
	}

	private SpellEventConfiguration ParseEvent(XmlReader reader)
	{
		SpellEventConfiguration result;

		string name = reader.GetAttribute("name");

		if (name == null)
		{
			XmlReaderError(reader, "event must have a name");
			return null;
		}
		
		string parent = reader.GetAttribute("parent");
		
		if (parent == null)
		{
			result = new SpellEventConfiguration(name);
		}
		else if (outputConfig.ContainsKey(parent))
		{
			result = new SpellEventConfiguration(name, outputConfig[parent]);
		}
		else
		{
			result = new SpellEventConfiguration(name);
			XmlReaderError(reader, "could not find output-configuration named '" + parent + "'");
		}
		
		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "event")
			{
				break;
			}
			else if (reader.NodeType == XmlNodeType.Element && reader.Name == "parameter")
			{
				result.AddParameter(ParseParameter(reader));
			}
			else if (reader.NodeType == XmlNodeType.Element && reader.Name == "description")
			{
				result.Description = reader.ReadElementContentAsString().Trim();
			}
		}

		return result;
	}

	private void ParseDefinition(XmlReader reader)
	{
		string input = reader.GetAttribute("input");
		string output = reader.GetAttribute("output");

		if (input == null && output == null)
		{
			return;
		}

		string name = reader.GetAttribute("alias");

		if (name == null)
		{
			XmlReaderError(reader, "effect must have an alias");
			return;
		}

		SpellNodeType result;

		if (reader.GetAttribute("is-root") == "true")
		{
			result = new SpellNodeType(name, null, null);
			result.Namespace = reader.GetAttribute("namespace") ?? "root effect";
			result.IsRoot = true;
		}
		else 
		{
			result = new SpellNodeType(name, new SpellNodeConnectorType("trigger", "event", false), new SpellNodeConnectorType("sibling", "event", false));
			result.Namespace = reader.GetAttribute("namespace") ?? "effect";
		}

		result.SetXmlGenerator(new SpellNodeEffectXmlGenerator());

		if (input != null)
		{
			if (inputConfig.ContainsKey(input))
			{
				SpellInputConfiguration spellInput = inputConfig[input];

				for (int i = 0; i < spellInput.ParameterCount; ++i)
				{
					result.AddInput(spellInput.BuildConnector(i));
				}
			}
			else 
			{
				XmlReaderError(reader, "no input-configuration found with the name '" + input + "'");
			}
		}
		
		if (output != null)
		{
			if (outputConfig.ContainsKey(output))
			{
				SpellOutputConfiguration spellOutput = outputConfig[output];
				
				for (int i = 0; i < spellOutput.ParameterCount; ++i)
				{
					result.AddOutput(spellOutput.BuildConnector(i));
				}

				for (int i = 0; i < spellOutput.EventCount; ++i)
				{
					result.AddEvent(spellOutput.BuildEvent(i));
				}
			}
			else 
			{
				XmlReaderError(reader, "no output-configuration found with the name '" + output + "'");
			}
		}

		if (!reader.IsEmptyElement)
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "effect")
				{
					break;
				}
				else if (reader.NodeType == XmlNodeType.Element && reader.Name == "description")
				{
					result.Description = reader.ReadElementContentAsString().Trim();
				}
			}
		}

		result.IsEffect = true;

		spellTypes[name] = result;
	}

	private void ParseFunction(XmlReader reader)
	{
		string name = reader.GetAttribute("name");

		if (name == null)
		{
			XmlReaderError(reader, "function must specify a name");
		}

		SpellNodeType result = new SpellNodeType(name, null, null);

		result.SetXmlGenerator(new SpellNodeFunctionXmlGenerator());
		
		result.Namespace = reader.GetAttribute("namespace") ?? "function";

		bool hasReturnType = false;

		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "function")
			{
				break;
			}
			else if (reader.NodeType == XmlNodeType.Element)
			{
				if (reader.Name == "return-type")
				{
					if (hasReturnType)
					{
						XmlReaderError(reader, "more than one return type specified");
					}
					else 
					{
						result.AddOutput(new SpellNodeConnectorType("result", reader.ReadElementContentAsString().Trim(), true));
					}
				}
				else if (reader.Name == "parameter")
				{
					SpellParameter parameter = ParseParameter(reader);

					if (parameter != null)
					{
						result.AddInput(parameter.BuildConnector(false));
					}
				}
				else if (reader.Name == "description")
				{
					result.Description = reader.ReadElementContentAsString().Trim();
				}
			}
		}


		functions[name] = result;
	}
	
	private void ParseFile()
	{
		using (XmlReader reader = XmlReader.Create(new StringReader(source.text)))
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					if (reader.Name == "input-configuration")
					{
						string configName = reader.GetAttribute("name");

						if (configName != null)
						{
							inputConfig[configName] = ParseInputConfiguration(reader);
						}
						else 
						{
							XmlReaderError(reader, "no name specified");
						}
					}
					else if (reader.Name == "output-configuration")
					{
						string configName = reader.GetAttribute("name");
						
						if (configName != null)
						{
							outputConfig[configName] = ParseOutputConfiguration(reader);
						}
						else 
						{
							XmlReaderError(reader, "no name specified");
						}
					}
					else if (reader.Name == "effect")
					{
						ParseDefinition(reader);
					}
					else if (reader.Name == "include")
					{
						new SpellTypeParser(reader.ReadElementContentAsString().Trim(), this);
					}
					else if (reader.Name == "function")
					{
						ParseFunction(reader);
					}
				}
			}
		}
	}

	private SpellTypeParser(string settingsFileName, SpellTypeParser parent)
	{
		source = Resources.Load<TextAsset>(settingsFileName);

		inputConfig = parent.inputConfig;
		outputConfig = parent.outputConfig;
		spellTypes = parent.spellTypes;
		functions = parent.functions;
		
		if (source != null)
		{	
			ParseFile();
		}
		else
		{
			Debug.LogError("Could not load '" + settingsFileName + "' from Resources folder");
		}
	}

	public SpellTypeParser(string settingsFileName)
	{
		source = Resources.Load<TextAsset>(settingsFileName);
		
		if (source != null)
		{	
			ParseFile();
		}
		else
		{
			Debug.LogError("Could not load '" + settingsFileName + "' from Resources folder");
		}
	}

	public SortedDictionary<string, SpellNodeType> SpellTypes
	{
		get
		{
			return spellTypes;
		}
	}
	
	public SortedDictionary<string, SpellNodeType> Functions
	{
		get
		{
			return functions;
		}
	}
}
