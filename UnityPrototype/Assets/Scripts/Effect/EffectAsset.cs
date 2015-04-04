using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectAsset : ScriptableObject
{
	public List<GameObject> prefabList = new List<GameObject>();
	[SerializeField]
	private List<int> prefabReferenceCount = new List<int>();

	public string xmlText;

	private int AddPrefab(GameObject gameObject)
	{
		for (int i = 0; i < prefabList.Count; ++i)
		{
			if (prefabList[i] == null)
			{
				prefabList[i] = gameObject;
				prefabReferenceCount[i] = 0;
				return i;
			}
		}

		int result = prefabList.Count;
		prefabList.Add(gameObject);
		prefabReferenceCount.Add(0);
		return result;
	}

	private void RemovePrefab(int index)
	{
		prefabList[index] = null;
		prefabReferenceCount[index] = 0;

		if (index == prefabList.Count - 1)
		{
			prefabList.RemoveAt(index);
			prefabReferenceCount.RemoveAt(index);
		}
	}

	public int GetPrefabIndex(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return -1;
		}

		int result = prefabList.IndexOf(gameObject);

		if (result == -1)
		{
			result = AddPrefab(gameObject);
		}

		return result;
	}

	public GameObject GetPrefab(int index)
	{
		if (index < 0 || index >= prefabList.Count)
		{
			return null;
		}
		else
		{
			return prefabList[index];
		}
	}

	public void AddReference(int index)
	{
		while (index >= prefabReferenceCount.Count && index < prefabList.Count)
		{
			prefabReferenceCount.Add(0);
		}

		if (index != -1)
		{
			++prefabReferenceCount[index];
		}
	}

	public void RemoveReference(int index)
	{
		while (index >= prefabReferenceCount.Count && index < prefabList.Count)
		{
			prefabReferenceCount.Add(1);
		}

		if (index != -1 && index < prefabReferenceCount.Count)
		{
			--prefabReferenceCount[index];

			if (prefabReferenceCount[index] <= 0)
			{
				RemovePrefab(index);
			}
		}
	}
}
