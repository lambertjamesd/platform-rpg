using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface IFixedUpdate {
	void FixedUpdateTick(float timestep);
}

public static class UpdateManagerHelper
{	
	public static void AddToUpdateManager(this IFixedUpdate fixedUpdate, UpdateManager updateManager)
	{
		if (updateManager != null)
		{
			updateManager.AddReciever(fixedUpdate);
		}
	}
	
	public static void RemoveFromUpdateManager(this IFixedUpdate fixedUpdate, UpdateManager updateManager)
	{
		if (updateManager != null)
		{
			updateManager.RemoveReciever(fixedUpdate);
		}
	}
}

public class UpdateSpeedModifier
{
	private float updateScalar;

	public UpdateSpeedModifier(float scalar)
	{
		this.updateScalar = scalar;
	}

	public float TimeScalar
	{
		get
		{
			return updateScalar;
		}
	}
}

public class UpdateManager : MonoBehaviour {

	public float fixedFrameRate = 120.0f;
	public bool useUnityFixedUpdate = true;
	private float globalTimeModifier = 1.0f;
	private float fixedTimestep = 1.0f / 120.0f;

	private List<IFixedUpdate> updateList = new List<IFixedUpdate>();
	private List<IFixedUpdate> lateUpdateList = new List<IFixedUpdate>();
	private float accumulatedTime;

	private Dictionary<IFixedUpdate, List<UpdateSpeedModifier>> speedModifiers = new Dictionary<IFixedUpdate, List<UpdateSpeedModifier>>();

	private int loopIndex = -1;
	private int lateLoopIndex = -1;

	private bool paused = true;

	// Use this for initialization
	public void Start() {
		fixedTimestep = 1.0f / fixedFrameRate;
	}

	public float SpeedModifierForTarget(IFixedUpdate target)
	{
		float result = globalTimeModifier;

		if (speedModifiers.ContainsKey(target))
		{
			foreach (UpdateSpeedModifier modifier in speedModifiers[target])
			{
				result *= modifier.TimeScalar;
			}
		}

		return result;
	}

	private void UpdateForTarget(IFixedUpdate target, float dt)
	{
		target.FixedUpdateTick(dt * SpeedModifierForTarget(target));
	}

	private void FixedUpdateInternal(float timestep)
	{
		// loopIndex is used to allow adding and removing recievers inside
		// this loop. See RemoveReciever below to see why its needed
		for (loopIndex = 0; loopIndex < updateList.Count; ++loopIndex)
		{
			UpdateForTarget(updateList[loopIndex], timestep);
		}

		for (lateLoopIndex = 0; lateLoopIndex < lateUpdateList.Count; ++lateLoopIndex)
		{
			UpdateForTarget(lateUpdateList[lateLoopIndex], timestep);
		}
		
		loopIndex = -1;
	}

	private static float lastTimestep = 0.0f;

	public void FixedUpdate()
	{
		// just checking my assumptions
		if (lastTimestep == 0.0f)
		{
			lastTimestep = Time.fixedDeltaTime;
		} 
		else if (lastTimestep != Time.fixedDeltaTime)
		{
			Debug.LogError("Unity is being an idiot");
		}


		if (!paused && useUnityFixedUpdate)
		{
			FixedUpdateInternal(Time.fixedDeltaTime);
		}
	}
	
	// Update is called once per frame
	public void Update() {
		if (!paused && !useUnityFixedUpdate)
		{
			accumulatedTime += Time.deltaTime;

			while (!paused && accumulatedTime >= fixedTimestep)
			{
				FixedUpdateInternal(fixedTimestep);
				accumulatedTime -= fixedTimestep;
			}
		}
	}

	public int UpdateListCount
	{
		get
		{
			return updateList.Count;
		}
	}

	public void AddPriorityReciever(IFixedUpdate reciever)
	{
		if (!updateList.Contains(reciever))
		{
			updateList.Insert(0, reciever);
		}
	}

	public void AddReciever(IFixedUpdate reciever)
	{
		if (!updateList.Contains(reciever))
		{
			updateList.Add(reciever);
		}
	}

	public void RemoveReciever(IFixedUpdate reciever)
	{
		int index = updateList.IndexOf(reciever);

		if (index != -1)
		{
			// allows the update list to be modified while being iterated over
			if (loopIndex >= index)
			{
				--loopIndex;
			}

			updateList.RemoveAt(index);
		}
	}
	
	public void AddLateReciever(IFixedUpdate reciever)
	{
		if (!lateUpdateList.Contains(reciever))
		{
			lateUpdateList.Add(reciever);
		}
	}
	
	public void RemoveLateReciever(IFixedUpdate reciever)
	{
		int index = lateUpdateList.IndexOf(reciever);
		
		if (index != -1)
		{
			// allows the update list to be modified while being iterated over
			if (lateLoopIndex >= index)
			{
				--lateLoopIndex;
			}
			
			lateUpdateList.RemoveAt(index);
		}
	}

	public void AddUpdateModifier(IFixedUpdate target, UpdateSpeedModifier modifier)
	{
		if (speedModifiers.ContainsKey(target))
		{
			speedModifiers[target].Add(modifier);
		}
		else
		{
			List<UpdateSpeedModifier> modifierList = new List<UpdateSpeedModifier>();
			modifierList.Add(modifier);
			speedModifiers.Add(target, modifierList);
		}
	}

	public void RemoveUpdateModifier(IFixedUpdate target, UpdateSpeedModifier modifier)
	{
		if (speedModifiers.ContainsKey(target))
		{
			speedModifiers[target].Remove(modifier);
		}
	}

	private static Dictionary<IFixedUpdate, List<UpdateSpeedModifier>> Clone(Dictionary<IFixedUpdate, List<UpdateSpeedModifier>> input)
	{
		Dictionary<IFixedUpdate, List<UpdateSpeedModifier>> result = new Dictionary<IFixedUpdate, List<UpdateSpeedModifier>>();
		
		foreach (IFixedUpdate key in input.Keys)
		{
			result.Add(key, new List<UpdateSpeedModifier>(input[key]));
		}
		
		return result;
	}

	public object ModifierState()
	{
		return Clone(speedModifiers);
	}

	public void RestoreModifierState(object input)
	{
		speedModifiers = Clone((Dictionary<IFixedUpdate, List<UpdateSpeedModifier>>)input);
	}

	public bool Paused
	{
		get
		{
			return paused;
		}

		set
		{
			paused = value;
		}
	}
}
