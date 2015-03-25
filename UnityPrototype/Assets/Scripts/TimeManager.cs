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
	private Dictionary<ITimeTravelable, object> objectData;
	private int frame;
	private float time;
	private int currentID;

	private TimeSnapshot(Dictionary<ITimeTravelable, object> objectData, int frame, float time, int currentID)
	{
		this.objectData = objectData;
		this.frame = frame;
		this.time = time;
		this.currentID = currentID;
	}

	public static TimeSnapshot Generate(IList<ITimeTravelable> timeObjects, int frame, float time, int currentID)
	{
		Dictionary<ITimeTravelable, object> objectData = new Dictionary<ITimeTravelable, object>();

		foreach (ITimeTravelable timeTravelable in timeObjects)
		{
			objectData[timeTravelable] = timeTravelable.GetCurrentState();
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

	public void Apply(IList<ITimeTravelable> timeObjects)
	{
		foreach (ITimeTravelable timeTraveler in timeObjects)
		{
			if (objectData.ContainsKey(timeTraveler))
			{
				timeTraveler.RewindToState(objectData[timeTraveler]);
			}
			else
			{
				timeTraveler.RewindToState(null);
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
	
	public void Start()
	{
		updateManager = GetComponent<UpdateManager>();
		updateManager.AddPriorityReciever(this);
	}

	public void AddTimeTraveler(ITimeTravelable traveler)
	{
		// object can only be added once
		if (!timeObjects.Contains(traveler))
		{
			timeObjects.Add(traveler);
		}
	}

	public void RemoveTimeTraveler(ITimeTravelable traveler)
	{
		// once an object has been added to a snapshot it cannot be removed
		if (!savedObjects.Contains(traveler))
		{
			timeObjects.Remove(traveler);
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
			if (travelable.GetTimeManager().savedObjects.Contains(travelable))
			{
				shouldDestroy = false;
				break;
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
