Shader "Custom/DrawMesh" 
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
			
			struct Vert
			{
				float4 position;
				float4 normal;
			};

			uniform StructuredBuffer<Vert> vertexBuffer;

			struct v2f 
			{
				float4  pos : SV_POSITION;
				float4 col : Color;
			};

			v2f vert(uint id : SV_VertexID)
			{
				Vert vert = vertexBuffer[id];

				v2f OUT;
				OUT.pos = mul(UNITY_MATRIX_VP, float4(vert.position.xyz, 1));
				OUT.col = vert.normal;
				//OUT.col = dot(float3(0,1,0), vert.normal) * 0.5 + 0.5;
				
				return OUT;
			}

			float4 frag(v2f IN) : COLOR
			{
				return IN.col;
				//return float4(IN.col);
			}

			ENDCG

		}
	}
}