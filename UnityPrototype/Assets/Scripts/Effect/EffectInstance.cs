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
using UnityEngine;
using System.Collections.Generic;

public class EffectInstance
{
	private EffectPropertyChain propertyChain;
	private EffectDefinition effectDefinition;
	private Dictionary<string, object> context;
	private bool isCancelled = false;

	private EffectInstance (EffectDefinition definition, EffectPropertyChain propertyChain, Dictionary<string, object> context)
	{
		effectDefinition = definition;
		this.propertyChain = propertyChain;
		this.context = context;
	}

	public EffectInstance (EffectDefinition definition, IEffectPropertySource rootPropertySource, Dictionary<string, object> context)
	{
		effectDefinition = definition;
		propertyChain = new EffectPropertyChain(null, rootPropertySource);
		this.context = context;
	}

	public EffectDefinition Definition
	{
		get
		{
			return effectDefinition;
		}
	}

	public void TriggerEvent(string name, IEffectPropertySource eventPropertySource)
	{
		if (effectDefinition.HasEvent(name) && !isCancelled)
		{
			EffectPropertyChain eventPropertyChain = new EffectPropertyChain(propertyChain, eventPropertySource);
			
			foreach (EffectDefinition childDefinition in effectDefinition.GetChildren(name))
			{
				IEffect previousEffect = EffectFactory.GetInstance().SpawnEffect(new EffectInstance(childDefinition, eventPropertyChain, context));
				eventPropertyChain = previousEffect.Instance.propertyChain;
			}
		}
	}

	public GameObject GetPrefab(string name)
	{
		int result = GetValue<int>(name, -1);

		if (result != -1)
		{
			return effectDefinition.Source.GetPrefab(result);
		}
		else
		{
			return null;
		}
	}

	public I GetContextValue<I>(string name, I defaultValue)
	{
		if (context.ContainsKey(name))
		{
			object result = context[name];

			if (result is I)
			{
				return (I)result;
			}
		}

		return defaultValue;
	}

	public I GetValue<I>(string name)
	{
		return effectDefinition.GetValue<I>(name, propertyChain);
	}

	public I GetValue<I>(string name, I defaultValue)
	{
		return effectDefinition.GetValue<I>(name, propertyChain, defaultValue);
	}

	public float GetFloatValue(string name, float defaultValue)
	{
		object result = effectDefinition.GetValue<object>(name, propertyChain, null);

		if (result is float)
		{
			return (float)result;
		}
		else if (result is int)
		{
			return (float)(int)result;
		}
		else
		{
			return defaultValue;
		}
	}

	public int GetIntValue(string name, int defaultValue)
	{	
		object result = effectDefinition.GetValue<object>(name, propertyChain, null);
		
		if (result is int)
		{
			return (int)result;
		}
		else if (result is float)
		{
			return (int)(float)result;
		}
		else
		{
			return defaultValue;
		}
	}

	public EffectInstance ExtendChain(IEffectPropertySource propertySource)
	{
		return new EffectInstance(effectDefinition, new EffectPropertyChain(propertyChain, propertySource), context);
	}

	public void Cancel()
	{
		isCancelled = true;
	}
}
