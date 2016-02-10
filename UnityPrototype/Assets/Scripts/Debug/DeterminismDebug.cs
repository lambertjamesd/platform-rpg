#define ENABLE_DETERMINSIM_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_DETERMINSIM_DEBUG

public class DeterminismEntity {

	private Object target;
	private List<string> history;
	private int currentPosition;
	private bool invalidState = false;

	public DeterminismEntity(Object target)
	{
		this.target = target;
		StartRecording();
	}

	public void StartRecording()
	{
		history = new List<string>();
		currentPosition = -1;
		invalidState = false;
	}

	public void StartVerifying()
	{
		currentPosition = 0;
		invalidState = false;
	}

	private void Error(string message)
	{
		Debug.LogError(message, target);
		invalidState = true;
	}

	public void StopVerifying()
	{
		if (currentPosition != -1 && currentPosition != history.Count)
		{
			Error("Got less values than what was in history");
		}
	}

	public void Log(string value)
	{
		if (!invalidState)
		{
			if (currentPosition == -1)
			{
				history.Add(value);
			}
			else if (currentPosition < history.Count)
			{
				if (history[currentPosition] != value)
				{
					Error("Expect value " + history[currentPosition] + " got " + value);
				}

				++currentPosition;
			}
			else
			{
				Error("Got more values than stored in history");
			}
		}
	}
}

#endif

public class DeterminismDebug {

	private static DeterminismDebug singleton;

	public static DeterminismDebug GetSingleton()
	{
		if (singleton == null)
		{
			singleton = new DeterminismDebug();
		}

		return singleton;
	}

#if ENABLE_DETERMINSIM_DEBUG
	private Dictionary<Object, DeterminismEntity> mappings = new Dictionary<Object, DeterminismEntity>();

	private DeterminismEntity GetEntity(Object target)
	{
		if (mappings.ContainsKey(target))
		{
			return mappings[target];
		}
		else
		{
			return null;
		}
	}
#endif

	public void Log(Object target, float value)
	{
		byte[] bitValue = System.BitConverter.GetBytes(value);

		// switch -0 to 0
		if (bitValue[0] == 0x00 && bitValue[1] == 0x0 && bitValue[2] == 0x0 && bitValue[3] == 0x80)
		{
			bitValue[3] = 0;
		}

		Log(target, value.ToString() + System.BitConverter.ToString(bitValue));
	}

	public void Log(Object target, string message)
	{
#if ENABLE_DETERMINSIM_DEBUG
		DeterminismEntity tmp = GetEntity(target);

		if (tmp != null)
		{
			tmp.Log(message);
		}
#endif
	}

	public void Reset()
	{
#if ENABLE_DETERMINSIM_DEBUG
		mappings = new Dictionary<Object, DeterminismEntity>();
#endif
	}

	public void Rewind()
	{
#if ENABLE_DETERMINSIM_DEBUG
		foreach (DeterminismEntity entity in mappings.Values)
		{
			entity.StopVerifying();
			entity.StartVerifying();
		}
#endif
	}

	public void StartRecording(Object target)
	{
#if ENABLE_DETERMINSIM_DEBUG
		mappings.Add(target, new DeterminismEntity(target));
#endif
	}
}