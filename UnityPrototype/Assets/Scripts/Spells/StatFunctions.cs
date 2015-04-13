using UnityEngine;
using System.Collections;

public static class StatFunctions {
	public static object NumberParameter(object[] parameters)
	{
		if (parameters.Length == 4)
		{
			if (!(parameters[0] is GameObject))
			{
				Debug.LogError("NumberParameter expects first parameter to be a GameObject");
			}
			else if (!(parameters[1] is int))
			{
				Debug.LogError("NumberParameter expects second parameter to be an int");
			}
			else if (!(parameters[2] is string))
			{
				Debug.LogError("NumberParameter expects third parameter to be a string");
			}
			else if (!(parameters[3] is float))
			{
				Debug.LogError("NumberParameter expects fourth parameter to be a float");
			}
			else
			{
				SpellCaster caster = ((GameObject)parameters[0]).GetComponent<SpellCaster>();
				int spellIndex = (int)parameters[1];

				if (caster != null)
				{
					SpellDescriptionParameter result;
					if (caster.GetSpell(spellIndex).ParameterMapping.TryGetValue((string)parameters[2], out result))
					{
						return result.value;
					}
				}

				return (float)parameters[3];
			}
		}
		else
		{
			Debug.LogError("NumberParameter expects 4 parameters");
		}

		return null;
	}
}
