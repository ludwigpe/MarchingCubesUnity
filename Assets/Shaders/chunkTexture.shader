Shader "Tri-Planar projection" {
  Properties {
		_Side("Side", 2D) = "white" {}
		_SideNormal("Side Normalmap", 2D) = "white" {}
		_Top("Top", 2D) = "white" {}
		_TopNormal("Top Normalmap", 2D) = "white" {}
		_Bottom("Bottom", 2D) = "white" {}
		_BottomNormal("Bottom Normalmap", 2D) = "white" {}
	}
	
	SubShader {
		Pass{
			Tags {
				"Queue"="Geometry"
				"IgnoreProjector"="False"
				"RenderType"="Opaque"
			}
 
			Cull Back
			ZWrite On
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		
			// user defined variables
			uniform sampler2D _Side, _Top, _Bottom;
			uniform sampler2D _SideNormal, _TopNormal, _BottomNormal;
			uniform float4 _Side_ST, _Top_ST, _Bottom_ST;
			uniform float4 _SideNormal_ST, _TopNormal_ST, _BottomNormal_ST;

			// Unity defined variables
			uniform float4 _LightColor0;
			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 wsPos : TEXCOORD0;
				float3 norm  : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.wsPos = mul(_Object2World, v.vertex);
				o.norm = v.normal;

				return o;
			}

			fixed4 frag(v2f f) : SV_Target
			{
				float3 normalDir = f.norm.xyz;
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - f.wsPos.xyz);
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);



				// Determine the blend weights for the 3 planar projections.  
				// N_orig is the vertex-interpolated normal vector.  
				float3 blend_weights = abs( f.norm.xyz );   // Tighten up the blending zone:  
				blend_weights = (blend_weights - 0.2) * 7;  
				blend_weights = max(blend_weights, 0);      // Force weights to sum to 1.0 (very important!)  
				blend_weights /= (blend_weights.x + blend_weights.y + blend_weights.z ).xxx;   
				// Now determine a color value and bump vector for each of the 3  
				// projections, blend them, and store blended results in these two  
				// vectors:  
				float4 blended_color; // .w hold spec value  
				float3 blended_bump_vec;  

				// Compute the UV coords for each of the 3 planar projections.  
				// tex_scale (default ~ 1.0) determines how big the textures appear.  
				float4 col1 = tex2D(_Side, frac(f.wsPos.zy * _Side_ST.xy + _Side_ST.zw));
				float4 col2 = tex2D(_Top, frac(f.wsPos.zx * _Top_ST.xy + _Top_ST.zw ));
				float4 col3 = tex2D(_Bottom, frac(f.wsPos.xy * _Bottom_ST.xy + _Bottom_ST.zw));

				// Sample bump maps too, and generate bump vectors.  
				// (Note: this uses an oversimplified tangent basis.)  
				float2 bumpFetch1 = tex2D(_SideNormal, frac(f.wsPos.zy * _SideNormal_ST.xy + _SideNormal_ST.zw)).xy - 0.5;
				float2 bumpFetch2 = tex2D(_TopNormal, frac(f.wsPos.zx * _TopNormal_ST.xy + _TopNormal_ST.zw )).xy - 0.5;
				float2 bumpFetch3 = tex2D(_BottomNormal, frac(f.wsPos.xy * _BottomNormal_ST.xy + _BottomNormal_ST.zw)).xy - 0.5;
				float3 bump1 = float3(0, bumpFetch1.x, bumpFetch1.y);  
				float3 bump2 = float3(bumpFetch2.y, 0, bumpFetch2.x);  
				float3 bump3 = float3(bumpFetch3.x, bumpFetch3.y, 0);  
				// Finally, blend the results of the 3 planar projections.  
				blended_color = col1.xyzw * blend_weights.xxxx +  
								col2.xyzw * blend_weights.yyyy +  
								col3.xyzw * blend_weights.zzzz;  
				blended_bump_vec = bump1.xyz * blend_weights.xxx +  
									bump2.xyz * blend_weights.yyy +  
									bump3.xyz * blend_weights.zzz;  
			//}  
			// Apply bump vector to vertex-interpolated normal vector.  
			
			normalDir = normalize(normalDir + blended_bump_vec);  
			normalDir *= -1;
			float3 diffuseReflection = 1.0f * _LightColor0.xyz * saturate(dot(normalDir, lightDir));
			float3 lightFinal = diffuseReflection + 0.25f;
			return float4(blended_color.xyz * lightFinal, 1.0);
			//return float4(f.norm.xyz, 1.0);
			}
		
			ENDCG
		}
	}
	Fallback "Diffuse"
}