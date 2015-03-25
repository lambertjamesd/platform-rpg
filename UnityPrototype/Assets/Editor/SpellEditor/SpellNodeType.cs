using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellNodeConnectorType {
	private string name;
	private string type;
	private bool supportMultipleConnections;

	private bool isInput;

	public SpellNodeConnectorType(string name, string type, bool supportMultipleConnections)
	{
		this.name = name;
		this.type = type;
		this.supportMultipleConnections = supportMultipleConnections;
	}

	public bool IsInput
	{
		get
		{
			return isInput;
		}

		set
		{
			isInput = value;
		}
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

	public bool SupportMultipleConnections
	{
		get
		{
			return supportMultipleConnections;
		}
	}
	
	public string Description {	get; set; }
}

public class SpellNodeEventType
{
	private SpellNodeConnectorType eventType;

	private List<SpellNodeConnectorType> output = new List<SpellNodeConnectorType>();

	public SpellNodeEventType(string name)
	{
		eventType = new SpellNodeConnectorType(name, "event", false);
		eventType.IsInput = false;
	}

	public SpellNodeConnectorType Type
	{
		get
		{
			return eventType;
		}
	}

	public string Name
	{
		get
		{
			return eventType.Name;
		}
	}

	public void AddOutput(SpellNodeConnectorType connector)
	{
		connector.IsInput = false;
		output.Add(connector);
	}
	
	public int OutputCount
	{
		get
		{
			return output.Count;
		}
	}
	
	public SpellNodeConnectorType GetOutput(int index)
	{
		return output[index];
	}
	
	public string Description {	get; set; }
}

public class SpellNodeType
{
	private string name;
	private SpellNodeConnectorType inConnection;
	private SpellNodeConnectorType outConnection;

	private List<SpellNodeConnectorType> input = new List<SpellNodeConnectorType>();
	private List<SpellNodeConnectorType> output = new List<SpellNodeConnectorType>();
	private List<SpellNodeEventType> events = new List<SpellNodeEventType>();

	private SpellNodeXmlGenerator xmlGenerator;

	public SpellNodeType(string name, SpellNodeConnectorType inConnection, SpellNodeConnectorType outConnection)
	{
		this.name = name;
		this.inConnection = inConnection;
		this.outConnection = outConnection;

		if (inConnection != null)
		{
			inConnection.IsInput = true;
		}

		if (outConnection != null)
		{
			outConnection.IsInput = false;
		}
	}

	public void SetXmlGenerator(SpellNodeXmlGenerator value)
	{
		xmlGenerator = value;
	}

	public SpellNodeXmlGenerator XmlGenerator
	{
		get
		{
			return xmlGenerator;
		}
	}

	public string Name
	{
		get
		{
			return name;
		}
	}

	public SpellNodeConnectorType InConnection
	{
		get
		{
			return inConnection;
		}
	}

	public SpellNodeConnectorType OutConnection
	{
		get
		{
			return outConnection;
		}
	}

	public void AddInput(SpellNodeConnectorType connector)
	{
		connector.IsInput = true;
		input.Add(connector);
	}

	public int InputCount
	{
		get
		{
			return input.Count;
		}
	}

	public SpellNodeConnectorType GetInput(int index)
	{
		return input[index];
	}

	public void AddOutput(SpellNodeConnectorType connector)
	{
		connector.IsInput = false;
		output.Add(connector);
	}

	public int OutputCount
	{
		get
		{
			return output.Count;
		}
	}

	public SpellNodeConnectorType GetOutput(int index)
	{
		return output[index];
	}
	
	
	public void AddEvent(SpellNodeEventType eventType)
	{
		events.Add(eventType);
	}
	
	public int EventCount
	{
		get
		{
			return events.Count;
		}
	}
	
	public SpellNodeEventType GetEvent(int index)
	{
		return events[index];
	}
	
	public string Description {	get; set; }
	public string Namespace { get; set; }
	public bool IsRoot { get; set; }
	public bool IsEffect { get; set; }
}