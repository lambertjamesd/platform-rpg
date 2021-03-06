//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public abstract class EffectProperty
{
	public abstract object GetObjectValue(EffectPropertyChain chain);
	
	public abstract void Accept(EffectPropertyVisitor visitor);

	public I GetValue<I>(EffectPropertyChain chain)
	{
		try
		{
			return (I)GetObjectValue(chain);
		}
		catch (NullReferenceException e)
		{
			Debug.LogError(this.ToString() + " cannot be null");
			throw e;
		}
	}
}

public class EffectConstantProperty<I> : EffectProperty
{
	private I value;

	public EffectConstantProperty(I value)
	{
		this.value = value;
	}

	public override object GetObjectValue(EffectPropertyChain chain)
	{
		return value;
	}

	public I GetValue()
	{
		return value;
	}

	public override void Accept (EffectPropertyVisitor visitor)
	{
		visitor.Visit<I>(this);
	}
}

// retrieves a property from a spell node ancestor
// in xml this is a value represented by nodeName.propertyName
// the node name is converted to a number when an effect
// is parsed.
public class EffectChainProperty : EffectProperty
{
	private int chainIndex;
	private string nodeID;
	private string propertyName;
	
	public EffectChainProperty(int chainIndex, string nodeID, string propertyName)
	{
		this.chainIndex = chainIndex;
		this.nodeID = nodeID;
		this.propertyName = propertyName;
	}

	public string NodeID
	{
		get
		{
			return nodeID;
		}
	}
	
	public string PropertyName
	{
		get
		{
			return propertyName;
		}
	}
	
	public override object GetObjectValue(EffectPropertyChain chain)
	{
		return chain.GetProperty(chainIndex, propertyName);
	}

	public override void Accept (EffectPropertyVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override string ToString ()
	{
		return string.Format("{0}.{1}", nodeID, propertyName);
	}
}

public class EffectDefinition
{
	private Dictionary<string, EffectProperty> properties = new Dictionary<string, EffectProperty>();

	private Dictionary<string, List<EffectDefinition> > childrenDefinitions = new Dictionary<string, List<EffectDefinition>>();

	private string effectType;
	private EffectAsset source;

	public EffectDefinition (string effectType, EffectAsset source)
	{
		this.effectType = effectType;
		this.source = source;
	}

	public void AddProperty(string name, EffectProperty value)
	{
		properties[name] = value;
	}

	public void AddChildren(string eventName, List<EffectDefinition> childDefinition)
	{
		childrenDefinitions[eventName] = childDefinition;
	}

	public EffectAsset Source
	{
		get
		{
			return source;
		}
	}

	public string EffectType
	{
		get
		{
			return effectType;
		}
	}

	public I GetValue<I>(string name, EffectPropertyChain chain)
	{
		if (properties.ContainsKey(name))
		{
			return properties[name].GetValue<I>(chain);
		}
		else
		{
			throw new Exception("Could not find property named " + name);
		}
	}
	
	public I GetValue<I>(string name, EffectPropertyChain chain, I defaultValue)
	{
		if (properties.ContainsKey(name))
		{
			return properties[name].GetValue<I>(chain);
		}
		else
		{
			return defaultValue;
		}
	}

	public IEnumerable<EffectDefinition> GetChildren(string eventName)
	{
		if (childrenDefinitions.ContainsKey(eventName))
		{
			return childrenDefinitions[eventName];
		}
		else
		{
			return new List<EffectDefinition>();
		}
	}

	public bool HasEvent(string eventName)
	{
		return childrenDefinitions.ContainsKey(eventName);
	}

	private void WriteIndent(StringWriter output, int depth)
	{
		for (int i = 0; i < depth; ++i)
		{
			output.Write("  ");
		}
	}

	private void ToString(StringWriter output, int depth)
	{
		WriteIndent(output, depth);
		output.WriteLine(string.Format("EffectDefinition: {0}", effectType));
		++depth;

		foreach(KeyValuePair<string, EffectProperty> propertyPair in properties)
		{
			WriteIndent(output, depth);
			output.WriteLine(propertyPair.Key + ": " + propertyPair.Value.ToString());
		}
		
		foreach(KeyValuePair<string, List<EffectDefinition> > eventPair in childrenDefinitions)
		{
			WriteIndent(output, depth);
			output.WriteLine(eventPair.Key);

			foreach (EffectDefinition childDefinition in eventPair.Value)
			{
				childDefinition.ToString(output, depth + 1);
			}
		}
	}

	public override string ToString ()
	{
		StringWriter result = new StringWriter();
		ToString(result, 0);
		return result.ToString();
	}
}