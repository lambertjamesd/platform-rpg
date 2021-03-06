﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HealthShard
{
	public Vector2 position;
	public Vector2 velocity;
	public float liftime;
	public float fadeOutTime;
	public Color color;
	public float amount;
}

public class HealthBar : MonoBehaviour {
	public Material healthbarMaterial;
	public Vector2 screenSize;
	public Vector3 worldOffset = Vector3.up * 1.5f;
	public Sprite manditorySprite;
	public string sortLayer;

	public Color shieldColor = new Color(0.7f, 0.7f, 0.7f);
	public Color backgroundColor = Color.black;
	public Color damageColor = Color.red;

	public Vector2 shardGraviy = new Vector2(0.0f, -10.0f);
	public Vector2 shardSize = new Vector2(32.0f, 20.0f);

	private float healthTickVelocity = 100.0f;
	private float healthTickDelay = 0.5f;

	private GameObject spriteObject;

	private Material materialCopy;

	private Damageable damageSource;
	private float previousHealth;
	private float currentDamagePos;
	private float damageTime;

	private List<HealthShard> healthShards = new List<HealthShard>();

	private void UpdateMaterial()
	{
		float maxHealth = damageSource.MaxHealthWithShield;
		float shieldHealth = damageSource.CurrentHealthWithSheild;
		float currentHealth = damageSource.CurrentHealth;

		materialCopy.SetFloat("_PixelWidth", maxHealth / screenSize.x);
		materialCopy.SetFloat("_MaxHealth", maxHealth);
		materialCopy.SetFloat("_CurrentHealth", currentHealth);
		materialCopy.SetFloat("_ShieldHealth", shieldHealth);
		materialCopy.SetFloat("_PreviousHealth", Mathf.Max(currentDamagePos, shieldHealth));
	}

	public void Start()
	{
		damageSource = GetComponent<Damageable>();
		previousHealth = damageSource.CurrentHealth;
		currentDamagePos = previousHealth;
		materialCopy = new Material(healthbarMaterial);

		materialCopy.SetColor("_Color", TeamColors.GetColor(Player.LayerToTeam(gameObject.layer)));
		materialCopy.SetColor("_ShieldColor", shieldColor);
		materialCopy.SetColor("_DamageColor", damageColor);
		materialCopy.SetColor("_BackgroundColor", backgroundColor);

		UpdateMaterial();

		spriteObject = new GameObject();
		spriteObject.transform.parent = transform;
		spriteObject.name = "Health Bar";

		SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = manditorySprite;
		spriteRenderer.material = materialCopy;
		spriteRenderer.sortingLayerName = sortLayer;
	}

	public void Update()
	{
		UpdateMaterial();

		float currentHealth = damageSource.CurrentHealth;
		float deltaHealth = currentHealth - previousHealth;
		previousHealth = currentHealth;

		if (currentDamagePos > currentHealth && damageTime < Time.time)
		{
			currentDamagePos -= Time.deltaTime * healthTickVelocity;

			if (currentDamagePos <= currentHealth)
			{
				currentDamagePos = currentHealth;
			}
		}

		if (deltaHealth != 0)
		{
			if (damageTime < Time.time)
			{
				damageTime = Time.time + healthTickDelay;
			}

			HealthShard shard = new HealthShard();

			shard.amount = Mathf.Abs(deltaHealth);
			shard.color = deltaHealth > 0.0f ? Color.green : Color.red;
			shard.liftime = 0.25f;
			shard.velocity = new Vector2(Random.Range(-5.0f, 5.0f), 10.0f);

			FireShard(shard);
		}

		for (int i = healthShards.Count - 1; i >= 0; --i)
		{
			HealthShard shard = healthShards[i];

			shard.position += shard.velocity * Time.deltaTime;
			shard.velocity += shardGraviy * Time.deltaTime;

			shard.liftime -= Time.deltaTime;

			if (shard.liftime < 0.0f)
			{
				healthShards.RemoveAt(i);
			}
		}
	}

	private Vector3 RayToWorldPos(Ray ray, Vector3 targetPos, Vector3 targetForward)
	{
		float distance = Mathf.Abs(Vector3.Dot(targetForward, targetPos - ray.origin) / Vector3.Dot(ray.direction, targetForward));
		return ray.GetPoint(distance);
	}

	public void LateUpdate()
	{
		Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + worldOffset);

		screenPos.x = Mathf.Round(screenPos.x) + 0.5f;
		screenPos.y = Mathf.Round(screenPos.y);

		Vector3 halfScreenSize = new Vector3(Mathf.Round(screenSize.x * 0.5f), Mathf.Round(screenSize.y * 0.5f));

		Vector3 bottomLeft = screenPos - halfScreenSize;
		Vector3 topRight = screenPos + halfScreenSize;

		Vector3 targetPos = transform.position;
		Vector3 targetForward = spriteObject.transform.forward;

		Vector3 bottomLeftWorld = RayToWorldPos(Camera.main.ScreenPointToRay(bottomLeft), targetPos, targetForward);
		Vector3 topRightWorld = RayToWorldPos(Camera.main.ScreenPointToRay(topRight), targetPos, targetForward);

		spriteObject.transform.position = (bottomLeftWorld + topRightWorld) * 0.5f - Vector3.forward * 2.0f;;

		Vector3 offsetLocal = transform.InverseTransformPoint(topRightWorld) - transform.InverseTransformPoint(bottomLeftWorld);
		spriteObject.transform.localScale = new Vector3(
			manditorySprite.pixelsPerUnit * offsetLocal.x / manditorySprite.rect.width, 
			manditorySprite.pixelsPerUnit * offsetLocal.y / manditorySprite.rect.height, 
			1.0f);
	}

	public void FireShard(HealthShard shard)
	{
		healthShards.Add(shard);
	}
}
