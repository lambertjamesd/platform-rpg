using UnityEngine;
using System.Collections;

public class BreakableBarrier : MonoBehaviour, IFixedUpdate, ITimeTravelable
{
	private Damageable damageable;

	private UpdateManager updateManager;
	private TimeManager timeManager;

	private class BarrierState
	{
		private Vector3 position;
		private Quaternion rotation;
		private float health;

		public BarrierState(Vector3 position, Quaternion rotation, float health)
		{
			this.position = position;
			this.rotation = rotation;
			this.health = health;
		}

		public Vector3 Position
		{
			get
			{
				return position;
			}
		}

		public Quaternion Rotation
		{
			get
			{
				return rotation;
			}
		}

		public float Health
		{
			get
			{
				return health;
			}
		}
	}

	void OnEnable()
	{
		updateManager = updateManager ?? gameObject.GetComponentWithAncestors<UpdateManager>();
		timeManager = timeManager ?? gameObject.GetComponentWithAncestors<TimeManager>();

		damageable = GetComponent<Damageable>();
		this.AddToUpdateManager(updateManager);
		timeManager.AddTimeTraveler(this);
	}

	void OnDisable()
	{
		this.RemoveFromUpdateManager(updateManager);
	}

	public void FixedUpdateTick (float dt)
	{
		if (gameObject.activeSelf && damageable.IsDead)
		{
			TimeManager.DestroyGameObject(gameObject);
		}
	}

	public object GetCurrentState()
	{
		return new BarrierState(transform.position, transform.rotation, damageable.CurrentHealth);
	}

	public void RewindToState(object state)
	{
		if (state == null)
		{
			Destroy(gameObject);
		}
		else
		{
			BarrierState barrierState = (BarrierState)state;

			transform.position = barrierState.Position;
			transform.rotation = barrierState.Rotation;
			damageable.CurrentHealth = barrierState.Health;

			gameObject.SetActive(!damageable.IsDead);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
