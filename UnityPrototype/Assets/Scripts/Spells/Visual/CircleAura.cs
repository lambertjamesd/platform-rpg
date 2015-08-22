using UnityEngine;
using System.Collections;

public class CircleAura : EffectGameObject {

	private int team;
	private static readonly float RIM_THICKNESS = 0.1f;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect (instance);

		Radius = instance.GetValue<float>("radius", 1.0f);
		AuraColor = instance.GetValue<Color>("color", Color.white);
		Team = instance.GetContextValue<int>("casterTeam", 0);
	}

	public Color AuraColor
	{
		get
		{
			return GetComponent<Renderer>().material.color;
		}

		set
		{
			GetComponent<Renderer>().material.color = value;
		}
	}

	public int Team
	{
		get
		{
			return team;
		}

		set
		{
			team = value;
			GetComponent<Renderer>().material.SetColor("_RimColor", TeamColors.GetColor(team));
		}
	}

	public float Radius
	{
		get
		{
			return transform.localScale.x * 0.5f;
		}

		set
		{
			transform.localScale = Vector3.one * value * 2.0f;
			float uvThickness = 1.0f - RIM_THICKNESS / (value * 2.0f);
			GetComponent<Renderer>().material.SetFloat("_RimThickness", uvThickness * uvThickness);
		}
	}
}
