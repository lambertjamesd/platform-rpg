using UnityEngine;
using System.Collections;

public class Damageable : MonoBehaviour {

	public float maxHealth = 100.0f;
	private float currentHealth;

	// Use this for initialization
	void Awake () {
		currentHealth = maxHealth;
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
			currentHealth -= amount;

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
}
