using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IEffectPropertySource
{
	object GetObject(string name);
}

public class GenericPropertySource : IEffectPropertySource
{
	private Dictionary<string, object> valueMap = new Dictionary<string, object>();

	public void AddValue(string name, object value)
	{
		valueMap[name] = value;
	}
	
	public object GetObject(string name)
	{
		if (valueMap.ContainsKey(name))
		{
			return valueMap[name];
		}
		else
		{
			return null;
		}
	}
}

public class LambdaPropertySource : IEffectPropertySource
{
	public delegate object MappingFunction(string propertyName);

	private MappingFunction mappingFunction;

	public LambdaPropertySource(MappingFunction mappingFunction)
	{
		this.mappingFunction = mappingFunction;
	}

	public object GetObject(string name)
	{
		return mappingFunction(name);
	}
}

public class EffectPropertyChain {
	private EffectPropertyChain previousChain;
	private IEffectPropertySource source;

	public EffectPropertyChain(EffectPropertyChain previousChain, IEffectPropertySource source)
	{
		this.previousChain = previousChain;
		this.source = source;
	}

	public object GetProperty(int chainIndex, string propertyName)
	{
		if (chainIndex == 0)
		{
			if (source == null)
			{
				Debug.LogError("No source specified");
				return null;
			}
			else
			{
				return source.GetObject(propertyName);
			}
		}
		else
		{
			return previousChain.GetProperty(chainIndex - 1, propertyName);
		}
	}
}
