using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

public class UpdateManager : MonoBehaviour {

	public float fixedFrameRate = 120.0f;
	public bool useUnityFixedUpdate = true;
	private float fixedTimestep = 1.0f / 120.0f;

	private List<IFixedUpdate> updateList = new List<IFixedUpdate>();
	private float accumulatedTime;

	private int loopIndex = -1;

	private bool paused = false;

	// Use this for initialization
	public void Start() {
		fixedTimestep = 1.0f / fixedFrameRate;
	}

	private void FixedUpdateInternal(float timestep)
	{
		// loopIndex is used to allow adding and removing recievers inside
		// this loop. See RemoveReciever below to see why its needed
		for (loopIndex = 0; loopIndex < updateList.Count; ++loopIndex)
		{
			updateList[loopIndex].FixedUpdateTick(timestep);
		}
		
		loopIndex = -1;
	}

	public void FixedUpdate()
	{
		if (!paused && useUnityFixedUpdate)
		{
			FixedUpdateInternal(Time.deltaTime);
		}
	}
	
	// Update is called once per frame
	public void Update() {
		if (!paused && !useUnityFixedUpdate)
		{
			accumulatedTime += Time.deltaTime;

			while (accumulatedTime >= fixedTimestep)
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
