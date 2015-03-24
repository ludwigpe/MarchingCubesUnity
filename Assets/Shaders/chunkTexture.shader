Shader "Custom/chunkTexture" 
{
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_SpecColor ("Color", Color) = (1,1,1,1)
		_Shininess("Shininess", Float) = 10.0
		_ColorTex1("Albedo (RGB)", 2D) = "white" {}
		_ColorTex2("Albedo (RGB)", 2D) = "white" {}
		_ColorTex3("Albedo (RGB)", 2D) = "white" {}
		
	}
	SubShader 
	{
		Pass 
		{
			Cull back

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			uniform fixed4 _Color;
			uniform fixed4 _SpecColor;
			uniform float _Shininess;
			uniform sampler2D _ColorTex1;
			uniform float4	  _ColorTex1_ST;
			uniform sampler2D _ColorTex2;
			uniform float4	  _ColorTex2_ST;
			uniform sampler2D _ColorTex3;
			uniform float4	  _ColorTex3_ST;
			uniform float4 _LightColor0;
			struct vertexInput {
                float3 position : POSITION;
                float3 normal : NORMAL;
            };

			struct v2f 
			{
				float4  pos : SV_POSITION;
				float4 wsPos: TEXCOORD0;
				float3  N_orig : TEXCOORD1;
			};

			v2f vert(vertexInput vert)
			{
			
				v2f OUT;
				OUT.pos = mul(UNITY_MATRIX_MVP, float4(vert.position.xyz, 1));
				OUT.N_orig = float4(vert.normal.xyz, 1.0f);
				
				return OUT;
			}

			float4 frag(v2f IN) : COLOR
			{
				float3 normalDir = IN.N_orig.xyz;
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.wsPos.xyz);
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

				float4 blended_color;
				float3 blend_weights = abs( IN.N_orig.xyz );   // Tighten up the blending zone:  
				blend_weights = (blend_weights - 0.2) * 7;  
				blend_weights = max(blend_weights, 0);      // Force weights to sum to 1.0 (very important!)  
				blend_weights /= (blend_weights.x + blend_weights.y + blend_weights.z ).xxx;   
				
				float4 col1 = tex2D( _ColorTex1, (IN.wsPos.yz) * _ColorTex1_ST.xy + _ColorTex1_ST.zw );
				float4 col2 = tex2D( _ColorTex2, (IN.wsPos.zx) * _ColorTex2_ST.xy + _ColorTex2_ST.zw );
				float4 col3 = tex2D( _ColorTex3, (IN.wsPos.xy) * _ColorTex3_ST.xy + _ColorTex3_ST.zw );
				blended_color = col1.xyzw * blend_weights.xxxx +  
								col2.xyzw * blend_weights.yyyy +  
								col3.xyzw * blend_weights.zzzz;  

				//return blended_color;

				float3 diffuseReflection = 1.0f * _LightColor0.xyz * saturate(dot(normalDir, lightDir));
				float3 specReflection = diffuseReflection * _SpecColor.xyz * pow(saturate(dot(reflect(-lightDir, normalDir), viewDir)), _Shininess);
				specReflection = 0;
				float3 lightFinal = diffuseReflection + specReflection;
				//return float4(blended_color * lightFinal * _Color.xyz, 1.0);
				//return col1;


				float3 projNormal = saturate(pow(IN.N_orig * 1.4, 4));
			
				// SIDE X
				float3 x = tex2D(_ColorTex1, frac(IN.wsPos.zy * _ColorTex1_ST.xy)) * abs(IN.N_orig.x);
			
				// TOP / BOTTOM
				float3 y = 0;
				if (IN.N_orig.y > 0) {
					y = tex2D(_ColorTex2, frac(IN.wsPos.zx * _ColorTex2_ST.xy)) * abs(IN.N_orig.y);
				} else {
					y = tex2D(_ColorTex3, frac(IN.wsPos.zx * _ColorTex3_ST.xy)) * abs(IN.N_orig.y);
				}
			
				// SIDE Z	
				float3 z = tex2D(_ColorTex1, frac(IN.wsPos.xy * _ColorTex1_ST.xy)) * abs(IN.N_orig.z);
				
				float3 finalColor;
				finalColor = z;
				finalColor = lerp(finalColor, x, projNormal.x);
				finalColor = lerp(finalColor, y, projNormal.y);
				return float4(finalColor * lightFinal * _Color.xyz, 1.0);

			}

			ENDCG

		}
	}
}