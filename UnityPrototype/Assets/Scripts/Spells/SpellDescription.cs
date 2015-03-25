using UnityEngine;
using System.Collections;

public class SpellDescription : ScriptableObject {
	public string spellName;
	public string description;
	public float cooldown;
	public float maxHoldTime;
	public EffectAsset effect;
	public Texture2D icon;

	public Vector3 castOrigin;
}
