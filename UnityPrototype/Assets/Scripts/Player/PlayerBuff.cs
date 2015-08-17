using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BuffStat {
	public BuffStat(string statName, float statValue, bool multiply)
	{
		this.statName = statName;
		this.statValue = statValue;
		this.multiplyStat = multiply;
	}

	public string statName;
	public float statValue = 1.0f;
	public bool multiplyStat = true;
}

public static class BuffStatFunctions {
	public static object CreateBuff(object[] parameters)
	{
		if (parameters.Length == 3)
		{
			if (!(parameters[0] is string))
			{
				Debug.LogError("CreateBuff expects first parameter to be a string");
			}
			else if (!(parameters[1] is float))
			{
				Debug.LogError("CreateBuff expects second parameter to be a float");
			}
			else if (!(parameters[2] is bool))
			{
				Debug.LogError("CreateBuff expects third parameter to be a bool");
			}

			return new BuffStat((string)parameters[0], (float)parameters[1], (bool)parameters[2]);
		}
		else
		{
			Debug.LogError("CreateBuff expects 3 parameters");
		}

		return null;
	}
}

public class BuffEffect : EffectObject {

	private PlayerBuff buff;
	private PlayerStats target;

	private static HashSet<string> movementBuffs = new HashSet<string>(new string[]{
		"maxMoveSpeed",
		"moveAcceleration",
		"minJumpHeight",
		"maxJumpHeight",
		"jumpHeightControlWindow"
	});

	private static bool HasMovementBuff(IEnumerable<BuffStat> buffs)
	{
		return buffs.Any(buff => movementBuffs.Contains(buff.statName));
	}

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);

		GameObject gameObjectTarget = instance.GetValue<GameObject>("target", null);

		if (gameObjectTarget != null)
		{
			target = gameObjectTarget.GetComponent<PlayerStats>();
			bool applyMovementBuffs = Player.GetTurnOrder(gameObjectTarget) >= Player.GetTurnOrder(instance.GetContextValue<GameObject>("gameObject", null));

			if (target != null)
			{
				List<BuffStat> stats = instance.GetValue<List<object>>("buffs", new List<object>()).ConvertAll<BuffStat>(objectStat => (BuffStat)objectStat);

				if (applyMovementBuffs || !HasMovementBuff(stats))
				{
					buff = new PlayerBuff(instance.GetIntValue("priority", 0), stats);
					target.AddBuff(buff);
				}
			}
		}
	}

	public override void Cancel()
	{
		if (target != null && buff != null)
		{
			target.RemoveBuff(buff);
		}
	}
}

public class PlayerBuff {
	private int priority = 0;

	private Dictionary<string, BuffStat> stats = new Dictionary<string, BuffStat>();

	public PlayerBuff(int priority, List<BuffStat> stats)
	{
		this.priority = priority;

		foreach (BuffStat stat in stats)
		{
			AddStat(stat);
		}
	}

	public void AddStat(BuffStat stat)
	{
		stats[stat.statName] = stat;
	}

	public float ApplyBuff(float input, string statName)
	{
		if (stats.ContainsKey(statName))
		{
			BuffStat stat = stats[statName];

			if (stat.multiplyStat)
			{
				return input * stat.statValue;
			}
			else
			{
				return input + stat.statValue;
			}
		}

		return input;
	}

	public int Priority
	{
		get
		{
			return priority;
		}
	}
}
