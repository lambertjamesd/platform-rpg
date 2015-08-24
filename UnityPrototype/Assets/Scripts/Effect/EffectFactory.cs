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
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.IO;

public abstract class EffectConstructor
{
	public abstract IEffect SpawnEffect(EffectInstance instance);
}

public static class BasicFunctions
{
	public static object Select(object[] parameters)
	{
		if (parameters.Length == 3)
		{
			if (!(parameters[0] is bool))
			{
				Debug.LogError("Select expects the first parameter to be a bool");
			}
			else
			{
				return (bool)parameters[0] ? parameters[1] : parameters[2];
			}
		}
		else
		{
			Debug.LogError("Select requires three parameters");
		}
		
		return null;
	}
}

public static class VectorFunctions
{
	public static object RotateVector(object[] parameters)
	{
		if (parameters.Length == 2)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("RotateVector expects the first parameter to be a vector");
			}
			else if (!(parameters[1] is float))
			{
				Debug.LogError("RotateVector expects the second parameter to be a float");
			}
			else
			{
				Vector3 vector = (Vector3)parameters[0];
				float angle = (float)parameters[1] * Mathf.Deg2Rad;

				float cosAngle = Mathf.Cos(angle);
				float sinAngle = Mathf.Sin(angle);

				return new Vector3(vector.x * cosAngle - vector.y * sinAngle, vector.x * sinAngle + vector.y * cosAngle, vector.z);
			}
		}
		else
		{
			Debug.LogError("RotateVector requires two parameters");
		}

		return null;
	}

	public static object Magnitude(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("Magnitude expects the first parameter to be a vector");
			}
			else
			{
				Vector3 vector = (Vector3)parameters[0];
				return vector.magnitude;
			}
		}
		else
		{
			Debug.LogError("Magnitude requires one parameter");
		}
		
		return null;
	}
	
	public static object Normalize(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("Normalize expects the first parameter to be a vector");
			}
			else
			{
				Vector3 vector = (Vector3)parameters[0];
				return vector.normalized;
			}
		}
		else
		{
			Debug.LogError("Normalize requires one parameter");
		}
		
		return null;
	}
	
	public static object Project(object[] parameters)
	{
		if (parameters.Length == 2)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("Project expects the first parameter to be a vector");
			}
			else if (!(parameters[0] is Vector3))
			{
				Debug.LogError("Project expects the second parameter to be a vector");
			}
			else
			{
				Vector3 vector = (Vector3)parameters[0];
				Vector3 normal = (Vector3)parameters[0];
				return Vector3.Project(vector, normal);
			}
		}
		else
		{
			Debug.LogError("Project requires two parameters");
		}
		
		return null;
	}

	public static object AngleToDirection(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is Single))
			{
				Debug.LogError("AngleToDirection expects the first parameter to be a float");
			}
			else
			{
				float angle = (float)parameters[0] * Mathf.Rad2Deg;
				return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
			}
		}
		else
		{
			Debug.LogError("Magnitude requires one parameter");
		}
		
		return null;
	}
	
	public static object DirectionToAngle(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("DirectionToAngle expects the first parameter to be a Vector3");
			}
			else
			{
				Vector3 vector = (Vector3)parameters[0];
				return Math.Atan2(vector.y, vector.x);
			}
		}
		else
		{
			Debug.LogError("DirectionToAngle requires one parameter");
		}
		
		return null;
	}
	
	public static object GetX(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("GetX expects the first parameter to be a Vector3");
			}
			else
			{
				return ((Vector3)parameters[0]).x;
			}
		}
		else
		{
			Debug.LogError("GetX requires one parameter");
		}
		
		return null;
	}
	
	public static object GetY(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("GetY expects the first parameter to be a Vector3");
			}
			else
			{
				return ((Vector3)parameters[0]).y;
			}
		}
		else
		{
			Debug.LogError("GetY requires one parameter");
		}
		
		return null;
	}

	public static object RotateTowards(object[] parameters)
	{
		if (parameters.Length == 3)
		{
			if (!(parameters[0] is Vector3))
			{
				Debug.LogError("RotateTowards expects the first parameter to be a vector");
			}
			else if (!(parameters[1] is Vector3))
			{
				Debug.LogError("RotateTowards expects the second parameter to be a vector");
			}
			else if (!(parameters[2] is float))
			{
				Debug.LogError("RotateTowards expects the third parameter to be a float");
			}
			else
			{
				Vector3 vector = (Vector3)parameters[0];
				Vector3 target = (Vector3)parameters[1];
				float angle = (float)parameters[2];

				return Vector3.RotateTowards(vector, target, angle * Mathf.Deg2Rad, 1.0f);
			}
		}
		else
		{
			Debug.LogError("RotateTowards requires three parameters");
		}
		
		return null;
	}
}

public static class GameObjectFunctions
{
	public static object GetObjectPosition(object[] parameters)
	{
		if (parameters.Length == 1)
		{
			if (!(parameters[0] is GameObject))
			{
				Debug.LogError("GetObjectPosition expects to get a game object");
			}
			else
			{
				return ((GameObject)parameters[0]).transform.position;
			}
		}
		else
		{
			Debug.LogError("GetObjectPosition expects 1 parameters");
		}

		return null;
	}
}

public class GameObjectEffectConstructor<I> : EffectConstructor where I : EffectGameObject
{
	public override IEffect SpawnEffect(EffectInstance eventInstance)
	{
		EffectInstance temporaryInstance = eventInstance.ExtendChain(null);

		GameObject prefabObject = temporaryInstance.GetPrefab("prefab");
		GameObject existingGameObject = temporaryInstance.GetValue<GameObject>("gameObject", null);

		GameObject gameObject;

		if (existingGameObject != null)
		{
			gameObject = existingGameObject;
		}
		else if (prefabObject != null)
		{
			gameObject = GameObject.Instantiate(prefabObject) as GameObject;
		}
		else
		{
			gameObject = new GameObject(typeof(I).Name);
		}

		I result = gameObject.GetOrAddComponent<I>();

		EffectInstance effectInstance = eventInstance.ExtendChain(result.PropertySource);

		GameObject defaultParent = gameObject.GetParent();

		if (defaultParent == null)
		{
			defaultParent = eventInstance.GetContextValue<GameObject>("parentGameObject", null);
		}

		gameObject.layer = effectInstance.GetValue<int>("layer", 8);
		GameObject parentObject = effectInstance.GetValue<GameObject>("parent", defaultParent);
		gameObject.transform.parent = parentObject ? parentObject.transform : null;
		Vector3 lastUp = gameObject.transform.TransformDirection(Vector3.up);
		gameObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, effectInstance.GetValue<Vector3>("up", lastUp));
		gameObject.transform.position = effectInstance.GetValue<Vector3>("position", gameObject.transform.position);
		result.StartEffect(effectInstance);
		return result;
	}
}

public class ObjectEffectConstructor<I> : EffectConstructor where I : IEffect, new()
{
	public override IEffect SpawnEffect(EffectInstance eventInstance)
	{
		I result = new I();
		EffectInstance effectInstance = eventInstance.ExtendChain(result.PropertySource);
		result.StartEffect(effectInstance);
		return result;
	}
}

public class EffectSettingsParser
{
	private TextAsset source;
	private EffectFactory factory;

	public EffectSettingsParser(TextAsset source, EffectFactory factory)
	{
		this.source = source;
		this.factory = factory;
	}
	
	private void XmlReaderError(XmlReader reader, string message)
	{
		Debug.LogError(source.name + " line " + ((IXmlLineInfo)reader).LineNumber + ": " + message, source);
	}
	
	private void ApplyAlias(XmlReader reader)
	{
		string className = reader.GetAttribute("class");
		string alias = reader.GetAttribute("alias");
		
		if (className == null)
		{
			// do nothing, an effect with no class name can still have
			// node editor properties
			return;
		}
		else if (alias == null)
		{
			XmlReaderError(reader, "'alias' attribute missing from alias");
		}
		else
		{
			if (factory.HasEffect(className))
			{
				factory.CreateAlias(className, alias);
			}
			else
			{
				XmlReaderError(reader, "The class named '" + className + "' does not exist");
			}
		}
	}
	
	public void ApplySettingsFile()
	{
		using (XmlReader reader = XmlReader.Create(new StringReader(source.text)))
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.Element && reader.Name == "effect")
				{
					ApplyAlias(reader);
				}
				else if (reader.NodeType == XmlNodeType.Element && reader.Name == "include")
				{
					string includeFile = reader.ReadElementContentAsString();
					
					TextAsset xmlSource = Resources.Load<TextAsset>(includeFile);
					
					if (xmlSource != null)
					{	
						EffectSettingsParser settingsParser = new EffectSettingsParser(xmlSource, factory);
						settingsParser.ApplySettingsFile();
					}
					else
					{
						Debug.LogError("Could not load '" + includeFile + "' from Resources folder");
					}

				}
			}
		}
	}
}

public class EffectFactory
{
	private Dictionary<string, EffectConstructor> constructors = new Dictionary<string, EffectConstructor>();
	private Dictionary<string, EffectFunctionProperty.PropertyFunction> runtimeFunctions = new Dictionary<string, EffectFunctionProperty.PropertyFunction>();
	private static EffectFactory singleton;

	private const string settingsFileName = "EffectSettings";

	private void AddPropertyMethod(MethodInfo method)
	{
		ParameterInfo[] parameters = method.GetParameters();

		if (method.IsStatic && parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]) && method.ReturnType == typeof(object))
		{
			runtimeFunctions[method.Name] = (EffectFunctionProperty.PropertyFunction)Delegate.CreateDelegate(typeof(EffectFunctionProperty.PropertyFunction), method);
		}
	}

	public EffectFactory ()
	{
		Type gameObjectConstructorType = typeof(GameObjectEffectConstructor<>);
		Type objectConstructorType = typeof(ObjectEffectConstructor<>);

		foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (type.IsAbstract && type.IsSealed)
			{
				foreach (MethodInfo method in type.GetMethods())
				{
					AddPropertyMethod(method);
				}
			}
			else if (!type.IsAbstract && !type.IsInterface)
			{
				if (type.IsSubclassOf(typeof(EffectGameObject)))
				{
					Type[] typeList = {type};
					Type genericType = gameObjectConstructorType.MakeGenericType(typeList);

					AddGameObjectConstructor(type.Name, Activator.CreateInstance(genericType) as EffectConstructor);
				}
				else if (typeof(IEffect).IsAssignableFrom(type))
				{
					Type[] typeList = {type};
					Type genericType = objectConstructorType.MakeGenericType(typeList);
					
					AddGameObjectConstructor(type.Name, Activator.CreateInstance(genericType) as EffectConstructor);
				}
			}
		}

		ApplySettingsFile();
	}

	private void ApplySettingsFile()
	{
		TextAsset xmlSource = Resources.Load<TextAsset>(settingsFileName);

		if (xmlSource != null)
		{	
			EffectSettingsParser settingsParser = new EffectSettingsParser(xmlSource, this);
			settingsParser.ApplySettingsFile();
		}
		else
		{
			Debug.LogError("Could not load '" + settingsFileName + "' from Resources folder");
		}
	}

	public static EffectFactory GetInstance()
	{
		if (singleton == null)
		{
			singleton = new EffectFactory();
		}
		
		return singleton;
	}

	public void AddGameObjectConstructor<I>(string typeName) where I : EffectGameObject
	{
		constructors[typeName] = new GameObjectEffectConstructor<I>();
	}

	public void AddObjectConstructor<I>(string typeName) where I : IEffect, new()
	{
		constructors[typeName] = new ObjectEffectConstructor<I>();
	}
	
	
	public void AddGameObjectConstructor(string typeName, EffectConstructor constructor)
	{
		constructors[typeName] = constructor;
	}

	public bool HasEffect(string name)
	{
		return constructors.ContainsKey(name);
	}

	public void CreateAlias(string className, string alias)
	{
		constructors[alias] = constructors[className];
	}

	public Dictionary<string, EffectFunctionProperty.PropertyFunction>.KeyCollection GetFunctionNames()
	{
		return runtimeFunctions.Keys;
	}

	public EffectFunctionProperty.PropertyFunction GetFunction(string name)
	{
		if (runtimeFunctions.ContainsKey(name))
		{
			return runtimeFunctions[name];
		}
		else
		{
			return null;
		}
	}
	
	public IEffect SpawnEffect(EffectInstance instance)
	{
		if (HasEffect(instance.Definition.EffectType))
		{
			EffectConstructor constructor = constructors[instance.Definition.EffectType];
			return constructor.SpawnEffect(instance);
		}
		else 
		{
			Debug.LogError("Could not spawn effect of type '" + instance.Definition.EffectType + "'. No such type exists", instance.Definition.Source);
			return null;
		}
	}
}
