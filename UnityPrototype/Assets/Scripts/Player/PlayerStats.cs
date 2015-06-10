using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public class PlayerStats : MonoBehaviour {
	public TextAsset jsonStats;

	private Dictionary<string, float> numberStats = new Dictionary<string, float>();

	private List<PlayerBuff> buffs = new List<PlayerBuff>();

	public void Awake()
	{
		JSONClass rootNode = JSON.Parse(jsonStats.text).AsObject;

		foreach (KeyValuePair<string, JSONNode> child in rootNode)
		{
			numberStats[child.Key] = child.Value.AsFloat;
		}
	}

	public float GetStatScale(string name)
	{
		return GetNumberStat(name) / GetBaseStat(name);
	}

	public float GetBaseStat(string name)
	{
		return numberStats.ContainsKey(name) ? numberStats[name] : 0.0f;
	}

	public float GetNumberStat(string name)
	{
		float result = GetBaseStat(name);

		foreach (PlayerBuff buff in buffs)
		{
			result = buff.ApplyBuff(result, name);
		}

		return result;
	}

	public void AddBuff(PlayerBuff buff)
	{
		int insertIndex = 0;

		while (insertIndex < buffs.Count && buff.Priority < buffs[insertIndex].Priority)
		{
			++insertIndex;
		}

		buffs.Insert(insertIndex, buff);
	}

	public void RemoveBuff(PlayerBuff buff)
	{
		buffs.Remove(buff);
	}
}
