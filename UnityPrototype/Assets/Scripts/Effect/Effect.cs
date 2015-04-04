using UnityEngine;
using System.Collections;

public class GameObjectPropertySource : IEffectPropertySource
{
	private GameObject target;

	public GameObjectPropertySource(GameObject target)
	{
		this.target = target;
	}

	public object GetObject(string name)
	{
		switch (name)
		{
		case "gameObject":
			return target;
		case "position":
			return (target == null) ? Vector3.zero : target.transform.position;
		case "up":
			return (target == null) ? Vector3.zero : target.transform.TransformDirection(Vector3.up);
		case "layer":
			return (target == null) ? 0 : target.layer;
		}

		return null;
	}
}

public abstract class EffectGameObject : MonoBehaviour, IEffect {

	protected EffectInstance instance;

	public virtual void StartEffect(EffectInstance instance) {
		this.instance = instance;
	}

	public virtual IEffectPropertySource PropertySource
	{
		get
		{
			return new GameObjectPropertySource(gameObject);
		}
	}
	
	public EffectInstance Instance 
	{ 
		get
		{
			return instance;
		}
	}

	public virtual void Cancel()
	{
		Debug.LogError("Cancel not implimented for " + this.GetType().ToString());
	}
}


public abstract class EffectObject : IEffect {
	
	protected EffectInstance instance;
	
	public virtual void StartEffect(EffectInstance instance) {
		this.instance = instance;
	}
	
	public virtual IEffectPropertySource PropertySource
	{
		get
		{
			return new LambdaPropertySource(name => {
				switch (name)
				{
				case "effect":
					return this;
				}

				return null;
			});
		}
	}
	
	public EffectInstance Instance 
	{ 
		get
		{
			return instance;
		}
	}
	
	public virtual void Cancel()
	{
		Debug.LogError("Cancel not implimented for " + this.GetType().ToString());
	}
}