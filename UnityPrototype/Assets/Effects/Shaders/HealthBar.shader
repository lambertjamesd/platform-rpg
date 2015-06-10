Shader "Custom/HealthBar" {
	Properties {
		_Color ("Main Color", Color) = (0.0, 1.0, 0.0, 1)
		_ShieldColor ("Shield Color", Color) = (1.0, 1.0, 1.0, 1)
		_DamageColor ("Damage Color", Color) = (1.0, 0.0, 0.0, 1)
		_BackgroundColor ("Background Color", Color) = (0.0, 0.0, 0.0, 1)
		_PixelWidth ("Pixel Width", Float) = 100.0
		_MaxHealth ("Max Heath", Float) = 100.0
		_CurrentHealth ("Current Health", Float) = 50.0
		_ShieldHealth ("Shield Health", Float) = 75.0
		_PreviousHealth ("Previous Health", Float) = 75.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		ZWrite Off
		ZTest Always
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert

		half4 _Color;
		half4 _ShieldColor;
		half4 _DamageColor;
		half4 _BackgroundColor;
		float _PixelWidth;
		float _MaxHealth;
		float _CurrentHealth;
		float _ShieldHealth;
		float _PreviousHealth;

		struct Input {
			float health;
		};
		
	    void vert (inout appdata_full v, out Input o) {
	        UNITY_INITIALIZE_OUTPUT(Input,o);
	        o.health = v.texcoord.x * _MaxHealth;
	    }

		void surf (Input IN, inout SurfaceOutput o) {
		
			if (IN.health <= _PreviousHealth)
			{
				if (fmod(IN.health, 100) >= 100 - _PixelWidth || fmod(IN.health, 1000) >= 1000 - _PixelWidth * 2)
				{
					o.Emission = _BackgroundColor.rgb;
				}
				else if (IN.health <= _CurrentHealth)
				{
					o.Emission = _Color.rgb;
				}
				else if (IN.health <= _ShieldHealth)
				{
					o.Emission = _ShieldColor.rgb;	
				}
				else
				{
					o.Emission = _DamageColor.rgb;
				}
			}
			else
			{
				o.Emission = _BackgroundColor.rgb;
			}
			
			o.Alpha = 1.0f;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
