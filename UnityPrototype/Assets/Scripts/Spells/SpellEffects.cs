using UnityEngine;
using System.Collections;

public class ResetSpellCooldown : EffectObject {
	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);
		instance.GetContextValue<SpellCaster>("caster", null).ResetCooldown(instance.GetIntValue("spellIndex", 0), instance.GetFloatValue("cooldownRedux", 0.0f));
	}
}
