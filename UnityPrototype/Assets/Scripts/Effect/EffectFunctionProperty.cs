using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectFunctionProperty : EffectProperty
{
	public delegate object PropertyFunction(object[] parameters);

	private string name;
	private PropertyFunction function;
	private List<EffectProperty> parameters;
	private object[] parameterValues;

	public EffectFunctionProperty (string name, PropertyFunction function, List<EffectProperty> parameters)
	{
		this.name = name;
		this.function = function;
		this.parameters = parameters;
		parameterValues = new object[parameters.Count];
	}
	
	public override object GetObjectValue(EffectPropertyChain chain)
	{
		for (int i = 0; i < parameters.Count; ++i)
		{
			parameterValues[i] = parameters[i].GetObjectValue(chain);
		}

		return function(parameterValues);
	}
	
	public override void Accept (EffectPropertyVisitor visitor)
	{
		visitor.Visit(this);
	}

	public string Name
	{
		get
		{
			return name;
		}
	}

	public int ParameterCount
	{
		get
		{
			return parameters.Count;
		}
	}

	public EffectProperty GetParameter(int index)
	{
		return parameters[index];
	}
}

public static class EffectCastingFunctions
{
	public static object ConstantNumber(object[] parameters)
	{
		return parameters[0];
	}

	public static object Float(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			object value = parameters[0];

			if (value is float)
			{
				return (float)value;
			}
			else if (value is int)
			{
				return (float)(int)value;
			}
			else if (value is string)
			{
				return float.Parse((string)value);
			}
			else
			{
				Debug.LogError("Cannot cast type " + value.GetType().Name + " to a float");
			}
		}
		else
		{
			Debug.LogError("Float expects a single argument");
		}

		return null;
	}
	
	public static object Int(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			object value = parameters[0];
			
			if (value is float)
			{
				return (int)(float)value;
			}
			else if (value is int)
			{
				return (int)value;
			}
			else if (value is string)
			{
				return int.Parse((string)value);
			}
			else
			{
				Debug.LogError("Cannot cast type " + value.GetType().Name + " to an int");
			}
		}
		else
		{
			Debug.LogError("Int expects a single argument");
		}
		
		return null;
	}
	
	public static object String(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			return parameters[0].ToString();
		}
		else
		{
			Debug.LogError("String expects a single argument");
		}
		
		return null;
	}
	
	public static object BuildList(object[] parameters)
	{
		if (parameters.Length == 2)
		{
			if (parameters[0] != null && !(parameters[0] is List<object>))
			{
				Debug.LogError("BuildList expects first argument to be null or a list");
			}

			List<object> result = (List<object>)parameters[0] ?? new List<object>();
			result.Add(parameters[1]);
			return result;
		}
		else
		{
			Debug.LogError("BuildList expects two arguments");
		}
		
		return null;
	}

	
	public static object CreateVector3(object[] parameters)
	{
		if (parameters.Length == 3)
		{
			try
			{
				return new Vector3((float)parameters[0], (float)parameters[1], (float)parameters[2]);
			}
			catch (InvalidCastException)
			{
				return Vector3.zero;
			}
		}
		else
		{
			Debug.LogError("String expects a single argument");
		}
		
		return null;
	}
	
	public static object Bool(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			object value = parameters[0];
			
			if (value is float)
			{
				return (float)value != 0.0f;
			}
			else if (value is int)
			{
				return (int)value != 0;
			}
			else if (value is string)
			{
				return bool.Parse((string)value);
			}
			else if (value is Vector3)
			{
				return ((Vector3)value) != Vector3.zero;
			}
			else
			{
				return value != null;
			}
		}
		else
		{
			Debug.LogError("Int expects a single argument");
		}
		
		return null;
	}

	
	public static object MapRange(object[] parameters)
	{
		if (parameters.Length == 5)
		{
			for (int i = 0; i < parameters.Length; ++i)
			{
				if (!(parameters[i] is float))
				{
					Debug.LogError("MapRange expects all floats");

					return null;
				}
			}

			float input = (float)parameters[0];
			float minInput = (float)parameters[1];
			float maxInput = (float)parameters[2];
			float minOutput = (float)parameters[3];
			float maxOutput = (float)parameters[4];

			return (maxOutput - minOutput) * (input - minInput) / (maxInput - minInput) + minInput;
		}
		else
		{
			Debug.LogError("MapRange expects five arguments");
		}
		
		return null;
	}
}