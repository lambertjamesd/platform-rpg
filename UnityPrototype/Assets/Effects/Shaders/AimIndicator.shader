Shader "Custom/SolidColor" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
		_PathCoeff ("Path Coefficients", Vector) = (1, 1, 0, -1)
		_StartingWidth ("Starting Width", Float) = 0.25
		_EndingWidth ("Ending Width", Float) = 0.1
		
		_Color ("Main Color", Color) = (1, 1, 1, 0.25)
		_ChargeColor ("Charge Color", Color) = (1, 0, 0, 0.5)
		
		_ChargeAmount ("Charge Amount", Range(0, 1)) = 0.5
    }
    SubShader {
        Pass {
            CGPROGRAM

            #pragma vertex vert
            
            struct VertOut {
                float4 pos:SV_POSITION;
                float2 uvCoord:TEXCOORD0;
            };
            
			float4 _PathCoeff;
			float _StartingWidth;
			float _EndingWidth;
			
			float4 _Color;
			float4 _ChargeColor;
			float _ChargeAmount;

            VertOut vert(float4 v:POSITION) {
            	VertOut result;
            	
            	float2x2 coeffMatrix = float2x2(_PathCoeff);
            	float2 pointCenter = mul(coeffMatrix, float2(v.x, v.x * v.x));
            	float2 pointVelocity = mul(coeffMatrix, float2(1, 2 * v.x));
            	
            	float2 pointTanget = normalize(float2(-pointVelocity.y, pointVelocity.x));
            	
            	float2 finalPosition = pointCenter + v.y * pointTanget * lerp(_StartingWidth, _EndingWidth, v.x);
            	
                result.pos = mul(UNITY_MATRIX_MVP, float4(finalPosition, 0.0, 1.0));
                result.uvCoord = float2(v.x, v.y * 0.5 + 0.5);
                return result;
            }

            #pragma fragment frag
            
            uniform sampler2D _MainTex;
            
            fixed4  frag(VertOut vertOut) : COLOR {
                return tex2D(_MainTex, vertOut.uvCoord) * lerp(_ChargeColor, _Color, saturate((vertOut.uvCoord.x - _ChargeAmount * 1.1) * 10 + 1));
            }

            ENDCG
        }
    }
}