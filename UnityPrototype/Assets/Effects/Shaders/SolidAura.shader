Shader "Custom/Outline" {
	Properties {
		_Color ("Main Color", Color) = (0.0, 0.5, 1.0, 0.5)
		_RimColor ("Rim Color", Color) = (0.0, 0.5, 1.0, 1.0)
		_RimThickness ("Rim Thickness", Range(0, 1)) = 0.9
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert

		struct Input {
   			float2 circlePos;
		};

		half4 _Color;
		half4 _RimColor;
		float _RimThickness;
		
	    void vert (inout appdata_full v, out Input o) {
	        UNITY_INITIALIZE_OUTPUT(Input,o);
	        o.circlePos = (v.texcoord.xy - float2(0.5, 0.5)) * 2;
	    }

		void surf (Input IN, inout SurfaceOutput o) {
			float uvDistanceSqrd = dot(IN.circlePos, IN.circlePos);
		
			if (uvDistanceSqrd < _RimThickness)
			{
				o.Emission = _Color.rgb;
				o.Alpha = 1.0f;
			}
			else if (uvDistanceSqrd < 1)
			{
				o.Emission = _RimColor.rgb;
				o.Alpha = 1.0f;
			}
			else
			{
				o.Emission = float3(0, 0, 0);
				o.Alpha = 0.0f;
			}
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
