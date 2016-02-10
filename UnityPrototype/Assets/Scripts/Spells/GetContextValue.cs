using UnityEngine;

public class GetContextValue : EffectObject
{
	private System.Object value;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);
		value = instance.GetContextValue<System.Object>(instance.GetValue<string>("name", null), null);
	}

	public override IEffectPropertySource PropertySource {
		get
		{
			return new LambdaPropertySource(name => {
				switch (name)
				{
				case "value":
					return value;
				}
				
				return null;
			});
		}
	}
}
