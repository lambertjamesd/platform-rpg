using UnityEngine;
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
	public Vector2 screenOffset;
	public Texture2D manditoryTexture;

	public Color shieldColor = new Color(0.7f, 0.7f, 0.7f);
	public Color backgroundColor = Color.black;
	public Color damageColor = Color.red;

	public Vector2 shardGraviy = new Vector2(0.0f, -10.0f);
	public Vector2 shardSize = new Vector2(32.0f, 20.0f);

	private Material materialCopy;
	private Vector3 localHealthbarAnchor;

	private Damageable damageSource;
	private float previousHealth;

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
		materialCopy.SetFloat("_PreviousHealth", shieldHealth);
	}

	public void Start()
	{
		damageSource = GetComponent<Damageable>();
		previousHealth = damageSource.CurrentHealth;
		materialCopy = new Material(healthbarMaterial);

		materialCopy.SetColor("_Color", TeamColors.GetColor(Player.LayerToTeam(gameObject.layer)));
		materialCopy.SetColor("_ShieldColor", shieldColor);
		materialCopy.SetColor("_DamageColor", damageColor);
		materialCopy.SetColor("_BackgroundColor", backgroundColor);

		Bounds playerBounds = GUIHelper.ObjectBoundingBox(gameObject);
		localHealthbarAnchor = transform.InverseTransformPoint(Vector3.Scale(playerBounds.min, new Vector3(0.5f, 0.0f, 0.5f)) + Vector3.Scale(playerBounds.max, new Vector3(0.5f, 1.0f, 0.5f)));

		UpdateMaterial();
	}

	public void Update()
	{
		UpdateMaterial();

		float currentHealth = damageSource.CurrentHealth;
		float deltaHealth = currentHealth - previousHealth;
		previousHealth = currentHealth;

		if (deltaHealth != 0)
		{
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

	public void FireShard(HealthShard shard)
	{
		healthShards.Add(shard);
	}

	public void OnGUI()
	{
		Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.TransformPoint(localHealthbarAnchor));
		screenPos.y = Screen.height - screenPos.y;

		Graphics.DrawTexture(new Rect(screenPos.x + screenOffset.x, screenPos.y + screenOffset.y, screenSize.x, screenSize.y), manditoryTexture, materialCopy);

		foreach (HealthShard shard in healthShards)
		{
			Vector2 shardPosition = new Vector2(screenPos.x, screenPos.y) + shard.position;
			GUI.color = shard.color;
			GUI.Label(new Rect(shardPosition.x - shardSize.x * 0.5f, shardPosition.y - shardSize.y, shardSize.x, shardSize.y), shard.amount.ToString());
		}
	}
}
