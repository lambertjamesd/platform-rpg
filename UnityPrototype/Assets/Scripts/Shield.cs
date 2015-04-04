using UnityEngine;
using System.Collections;

public interface IShieldDelegate
{
	void Destroyed();
}

public class Shield
{
	private float health = 0.0f;
	private IShieldDelegate shieldDelegate;

	public Shield(float health, IShieldDelegate shieldDelegate)
	{
		this.health = health;
		this.shieldDelegate = shieldDelegate;
	}

	public bool IsActive()
	{
		return health > 0.0f;
	}

	public object GetCurrentState()
	{
		return new object[]{
			this,
			health,
			shieldDelegate
		};
	}
	
	public static Shield RewindToState(object state)
	{
		object[] objectArray = (object[])state;
		Shield result = (Shield)objectArray[0];
		result.health = (float)objectArray[1];
		result.shieldDelegate = (IShieldDelegate)objectArray[2];
		return result;
	}

	public void Destroy()
	{
		health = 0.0f;
	}

	public void ShieldDestroyed()
	{
		shieldDelegate.Destroyed();
	}

	public float Damage(float damageAmount)
	{
		health -= damageAmount;

		if (health < 0.0f)
		{
			return -health;
		}
		else
		{
			return 0.0f;
		}
	}
}
