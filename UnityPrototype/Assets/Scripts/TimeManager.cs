using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface ITimeTravelable
{
	object GetCurrentState();
	void RewindToState(object state);

	TimeManager GetTimeManager();
}

public class TimeSnapshot
{
	private List<object> objectData;
	private int frame;
	private float time;
	private int currentID;

	private TimeSnapshot(List<object> objectData, int frame, float time, int currentID)
	{
		this.objectData = objectData;
		this.frame = frame;
		this.time = time;
		this.currentID = currentID;
	}

	public static TimeSnapshot Generate(IList<ITimeTravelable> timeObjects, int frame, float time, int currentID)
	{
		List<object> objectData = new List<object>();

		foreach (ITimeTravelable timeTravelable in timeObjects)
		{
			objectData.Add(timeTravelable.GetCurrentState());
		}

		return new TimeSnapshot(objectData, frame, time, currentID);
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

	public void Apply(List<ITimeTravelable> timeObjects)
	{
		for (int i = 0; i < timeObjects.Count; ++i)
		{
			ITimeTravelable timeTraveler = timeObjects[i];

			if (i < objectData.Count)
			{
				timeTraveler.RewindToState(objectData[i]);
			}
			else
			{
				timeTraveler.RewindToState(null);
			}
		}

		timeObjects.RemoveRange(objectData.Count, timeObjects.Count - objectData.Count);
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
	
	public void Start()
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

	public void TakeSnapshot()
	{
		if (currentSnapshotIndex == snapShots.Count - 1)
		{
			++currentSnapshotIndex;
			savedObjects.UnionWith(timeObjects);
			snapShots.Add(TimeSnapshot.Generate(timeObjects, currentFrame, currentTime, currentObjectId));
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

	public int SnapshotIndex
	{
		get
		{
			return currentSnapshotIndex;
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
