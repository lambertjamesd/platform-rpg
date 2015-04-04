using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SpellDescriptionParameter
{
	public string name;
	public float value;
}

public class SpellDescription : ScriptableObject {
	public string spellName;
	[Multiline]
	public string description;
	private string formattedDescription;
	public float cooldown;
	public float maxHoldTime;
	public EffectAsset effect;
	public Texture2D icon;

	public Vector3 castOrigin;
	
	public bool blockedWhenRooted;

	public List<SpellDescriptionParameter> parameters = new List<SpellDescriptionParameter>();
	private Dictionary<string, SpellDescriptionParameter> parameterMapping;

	public Dictionary<string, SpellDescriptionParameter> ParameterMapping
	{
		get
		{
			if (parameterMapping == null)
			{
				parameterMapping = new Dictionary<string, SpellDescriptionParameter>();

				foreach (SpellDescriptionParameter parameter in parameters)
				{
					parameterMapping.Add(parameter.name, parameter);
				}
			}

			return parameterMapping;
		}
	}

	public string FormattedDescription
	{
		get
		{
			if (string.IsNullOrEmpty(formattedDescription) && !string.IsNullOrEmpty(description))
			{
				Dictionary<string, object> paramterValues = new Dictionary<string, object>();

				foreach (SpellDescriptionParameter parameter in parameters)
				{
					paramterValues[parameter.name] = parameter.value;
				}

				formattedDescription = description.FormatWith(paramterValues);
			}

			return formattedDescription;
		}
	}
}
