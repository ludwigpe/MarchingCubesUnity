Shader "Custom/chunkMeshShader" 
{
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
			
			struct vertexInput {
                float3 position : POSITION;
                float3 normal : NORMAL;
            };

			struct v2f 
			{
				float4  pos : SV_POSITION;
				float4 col : Color;
			};

			v2f vert(vertexInput vert)
			{
			
				v2f OUT;
				OUT.pos = mul(UNITY_MATRIX_VP, float4(vert.position.xyz, 1));
				OUT.col = float4(vert.normal.xyz, 1.0f);
				//OUT.col = dot(float3(0,1,0), vert.normal) * 0.5 + 0.5;
				
				return OUT;
			}

			float4 frag(v2f IN) : COLOR
			{
				return IN.col;
			}

			ENDCG

		}
	}
}