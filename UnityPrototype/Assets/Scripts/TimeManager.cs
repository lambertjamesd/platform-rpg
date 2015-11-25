using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface ITimeTravelable
{
	object GetCurrentState();
	void RewindToState(object state);

	TimeManager GetTimeManager();
}

public static class TimeTravelable
{
	public static bool IsPersistant(this ITimeTravelable target)
	{
		return target.GetTimeManager().IsSaved(target);
	}
}

public class TimeSnapshot
{
	private List<object> objectData;
	private int frame;
	private float time;
	private int currentID;
	private object updateState;

	private TimeSnapshot(List<object> objectData, int frame, float time, int currentID, object updateState)
	{
		this.objectData = objectData;
		this.frame = frame;
		this.time = time;
		this.currentID = currentID;
		this.updateState = updateState;
	}

	public static TimeSnapshot Generate(IList<ITimeTravelable> timeObjects, int frame, float time, int currentID, object updateState)
	{
		List<object> objectData = new List<object>();

		foreach (ITimeTravelable timeTravelable in timeObjects)
		{
			objectData.Add(timeTravelable.GetCurrentState());
		}

		return new TimeSnapshot(objectData, frame, time, currentID, updateState);
	}

	public int Frame
	{
		get
		{
			return frame;
		}
	}

	public float Time
	{
		get
		{
			return time;
		}
	}

	public int CurrentID
	{
		get
		{
			return currentID;
		}
	}

	public int ObjectCount
	{
		get
		{
			return objectData.Count;
		}
	}

	public object UpdateState
	{
		get
		{
			return updateState;
		}
	}

	public void Apply(List<ITimeTravelable> timeObjects)
	{
		for (int i = 0; i < timeObjects.Count; ++i)
		{
			ITimeTravelable timeTraveler = timeObjects[i];

			if (i < objectData.Count)
			{
				// Time canonly rewind, an object that was dead in a previous
				// snapshot is dead now. Sending null to RewindToState will
				// destroy the object
				if (objectData[i] != null)
				{
					timeTraveler.RewindToState(objectData[i]);
				}
			}
			else
			{
				timeTraveler.RewindToState(null);
			}
		}

		timeObjects.RemoveRange(objectData.Count, timeObjects.Count - objectData.Count);
	}

	public void MarkNeededObjects(bool[] needed)
	{
		for (int i = 0; i < needed.Length && i < objectData.Count; ++i)
		{
			if (objectData[i] != null)
			{
				needed[i] = true;
			}
		}
	}

	public void CleanUpMarkedObjects(bool[] needed)
	{
		for (int i = Mathf.Min(needed.Length - 1, objectData.Count - 1); i >= 0; --i)
		{
			if (!needed[i])
			{
				objectData.RemoveAt(i);
			}
		}
	}
}

public class TimeManager : MonoBehaviour, IFixedUpdate {
	private List<ITimeTravelable> timeObjects = new List<ITimeTravelable>();
	private List<TimeSnapshot> snapShots = new List<TimeSnapshot>();
	private HashSet<ITimeTravelable> savedObjects = new HashSet<ITimeTravelable>();
	private int currentSnapshotIndex = -1;
	private int currentFrame = 0;
	private float currentTime;
	private int currentObjectId = 1;
	private UpdateManager updateManager;
	
	public void Awake()
	{
		updateManager = GetComponent<UpdateManager>();
		updateManager.AddLateReciever(this);
	}

	public void AddTimeTraveler(ITimeTravelable traveler)
	{
		// object can only be added once
		if (!timeObjects.Contains(traveler))
		{
			timeObjects.Add(traveler);
		}
	}

	public bool IsSaved(ITimeTravelable traveler)
	{
		int index = timeObjects.IndexOf(traveler);

		if (index != -1 && snapShots.Count > 0)
		{
			return index < snapShots[snapShots.Count - 1].ObjectCount;
		}

		return false;
	}

	public void TakeSnapshot()
	{
		if (currentSnapshotIndex == snapShots.Count - 1)
		{
			++currentSnapshotIndex;
			savedObjects.UnionWith(timeObjects);
			snapShots.Add(TimeSnapshot.Generate(timeObjects, currentFrame, currentTime, currentObjectId, updateManager.ModifierState()));
		}
		else
		{
			Debug.LogError("Cannot take snapshot while replaying");
		}
	}

	private void ApplySnapshot(int snapShotIndex)
	{
		TimeSnapshot snapShot = snapShots[currentSnapshotIndex];

		if (snapShot.Frame <= currentFrame)
		{
			snapShot.Apply(timeObjects);
			currentSnapshotIndex = snapShotIndex;
			currentFrame = snapShot.Frame;
			currentTime = snapShot.Time;
			currentObjectId = snapShot.CurrentID;
			updateManager.RestoreModifierState(snapShot.UpdateState);
		}
		else
		{
			Debug.LogError("Cannot apply snapshot in the future");
		}
	}

	public void Rewind()
	{
		if (snapShots.Count > 0)
		{
			ApplySnapshot(snapShots.Count - 1);
		}
	}

	public void RewindToStart()
	{
		if (snapShots.Count > 0)
		{
			ApplySnapshot(0);
		}
	}

	public void CleanUpSnapshots(int startIndex, int endIndex)
	{
		if (startIndex < endIndex)
		{
			int startCount = startIndex == 0 ? 0 : snapShots[startIndex - 1].ObjectCount;
			TimeSnapshot endSnapshop = snapShots[endIndex - 1];

			bool[] needed = new bool[endSnapshop.ObjectCount];

			for (int i = 0; i < startCount; ++i)
			{
				needed[i] = true;
			}

			endSnapshop.MarkNeededObjects(needed);
		
			snapShots.RemoveRange(startIndex, endIndex - startIndex);

			if (currentSnapshotIndex >= endIndex)
			{
				currentSnapshotIndex -= endIndex - startIndex;
			}
			else if (currentSnapshotIndex > startIndex)
			{
				currentSnapshotIndex = startIndex;
			}

			for (int i = startIndex; i < snapShots.Count; ++i)
			{
				snapShots[i].CleanUpMarkedObjects(needed);
			}

			for (int i = Mathf.Min(needed.Length - 1, timeObjects.Count - 1); i >= 0; --i)
			{
				if (!needed[i])
				{
					timeObjects[i].RewindToState(null);
					savedObjects.Remove(timeObjects[i]);
					timeObjects.RemoveAt(i);
				}
			}
		}
	}

	public void FixedUpdateTick(float dt)
	{
		if (currentSnapshotIndex < snapShots.Count - 1)
		{
			if (snapShots[currentSnapshotIndex + 1].Frame == currentFrame)
			{
				ApplySnapshot(currentSnapshotIndex + 1);
			}
		}

		++currentFrame;
		currentTime += dt;
	}

	public float CurrentTime
	{
		get
		{
			return currentTime;
		}
	}

	public int CurrentFrame
	{
		get
		{
			return currentFrame;
		}
	}

	public int SnapshotCount
	{
		get
		{
			return snapShots.Count;
		}
	}

	public static void DestroyGameObject(GameObject gameObject)
	{
		IList<ITimeTravelable> timeTravelableObjects = gameObject.GetInterfaceComponents<ITimeTravelable>();
		
		bool shouldDestroy = true;
		
		foreach (ITimeTravelable travelable in timeTravelableObjects)
		{
			TimeManager timeManager = travelable.GetTimeManager();

			if (timeManager.savedObjects.Contains(travelable))
			{
				shouldDestroy = false;
				break;
			}
			else if (timeManager.timeObjects.Contains(travelable))
			{
				timeManager.timeObjects.Remove(travelable);
			}
		}
		
		if (shouldDestroy)
		{
			GameObject.Destroy(gameObject);
		}
		else
		{
			gameObject.SetActive(false);
		}
	}
}
