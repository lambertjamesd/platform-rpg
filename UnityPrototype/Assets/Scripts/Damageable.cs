using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DamageableEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		GameObject target = instance.GetValue<GameObject>("target", null);

		Damageable damageable = target.GetOrAddComponent<Damageable>();
		damageable.SetMaxHealth(instance.GetFloatValue("maxHealth", damageable.maxHealth));

		damageable.SetDeathCallback(delegate {
			instance.TriggerEvent("die", new LambdaPropertySource(name => null));
		});
	}
}

public class Damageable : MonoBehaviour, ITimeTravelable {

	public List<Shield> shields = new List<Shield>();
	public float maxHealth = 100.0f;
	public float startingHealth = 1.0f;
	private float currentHealth;
	private TimeManager timeManager;

	public delegate void DeathCallback();

	private DeathCallback deathCallback;

	// Use this for initialization
	void Awake () {
		currentHealth = maxHealth * startingHealth;
		timeManager = gameObject.GetComponentWithAncestors<TimeManager>();
		timeManager.AddTimeTraveler(this);
	}

	public void SetDeathCallback(DeathCallback value)
	{
		deathCallback = value;
	}

	public void SetMaxHealth(float value)
	{
		currentHealth = value * HealthPercentage;
		maxHealth = value;
	}

	public float CurrentHealth
	{
		get
		{
			return currentHealth;
		}

		set
		{
			currentHealth = value;
		}
	}

	public float SheildHealth
	{
		get
		{
			float result = 0.0f;

			foreach (Shield sheild in shields)
			{
				result += sheild.Health;
			}

			return result;
		}
	}

	public float CurrentHealthWithSheild
	{
		get
		{
			return currentHealth + SheildHealth;
		}
	}

	public float MaxHealthWithShield
	{
		get
		{
			return Mathf.Max(maxHealth, CurrentHealthWithSheild);
		}
	}

	public float HealthPercentage
	{
		get
		{
			return currentHealth / maxHealth;
		}
	}

	public bool ApplyDamage(float amount)
	{
		if (currentHealth > 0.0f)
		{
			while (shields.Count > 0 && amount > 0.0f)
			{
				amount = shields[0].Damage(amount);

				if (!shields[0].IsActive())
				{
					shields[0].ShieldDestroyed();
					shields.RemoveAt(0);
				}
			}

			currentHealth -= amount;

			if (IsDead && deathCallback != null)
			{
				deathCallback();
			}

			return IsDead;
		}

		return false;
	}

	public void Heal(float amount)
	{
		if (currentHealth > 0.0f)
		{
			currentHealth += amount;

			if (currentHealth > maxHealth)
			{
				currentHealth = maxHealth;
			}
		}
	}

	public bool IsDead
	{
		get
		{
			return currentHealth <= 0.0f;
		}
	}

	public void AddShield(Shield shield)
	{
		shields.Add(shield);
	}

	public void RemoveShield(Shield shield)
	{
		if (shields.Contains(shield))
		{
			shields.Remove(shield);
			shield.Destroy();
			shield.ShieldDestroyed();
		}
	}
	
	public object GetCurrentState()
	{
		if (gameObject.activeSelf)
		{
			return new object[]{
				maxHealth,
				currentHealth,
				shields.Select(shield => shield.GetCurrentState()).ToArray()
			};
		}
		else
		{
			return null;
		}
	}
	
	public void RewindToState(object state)
	{
		if (state == null)
		{
			Destroy(gameObject);
		}
		else
		{
			object[] objectArray = (object[])state;
			maxHealth = (float)objectArray[0];
			currentHealth = (float)objectArray[1];

			object[] shieldStates = (object[])objectArray[2];
			shields = shieldStates.Select(shieldState => Shield.RewindToState(shieldState)).ToList();
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
