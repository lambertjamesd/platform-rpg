Shader "Custom/SaturationControl" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Saturation ("Saturation", Range(0,2)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf NoLighting

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Saturation;
		fixed4 _Color;

		fixed4 LightingNoLighting (SurfaceOutput s, fixed3 lightDir, fixed atten) {
             fixed4 c;
             c.rgb = s.Albedo; 
             c.a = s.Alpha;
             return c;
		}
		
         void surf (Input IN, inout SurfaceOutput o)
         {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed grayscale = 0.21 * c.r + 0.72 * c.g + 0.07 * c.b;
			
			o.Albedo = c.rgb * _Saturation + (1 - _Saturation) * grayscale;
			o.Alpha = c.a;       
         }
		ENDCG
	} 
	FallBack "Diffuse"
}
