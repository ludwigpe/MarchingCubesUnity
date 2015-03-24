Shader "Tri-Planar BumpSurf" {
  Properties {
		_Side("Side", 2D) = "white" {}
		_SideNormal("Side Normalmap", 2D) = "white" {}
		_Top("Top", 2D) = "white" {}
		_TopNormal("Top Normalmap", 2D) = "white" {}
		_Bottom("Bottom", 2D) = "white" {}
		_BottomNormal("Bottom Normalmap", 2D) = "white" {}
		_NormalPower("Normal power", Float) = 1.0
	}
	
	SubShader {
		Tags {
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"RenderType"="Opaque"
		}
 
		Cull Back
		ZWrite On
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert
		#pragma exclude_renderers flash
 
		// user defined variables
		uniform sampler2D _Side, _Top, _Bottom;
		uniform sampler2D _SideNormal, _TopNormal, _BottomNormal;
		uniform float4 _Side_ST, _Top_ST, _Bottom_ST;
		uniform float4 _SideNormal_ST, _TopNormal_ST, _BottomNormal_ST;
		uniform float _NormalPower;
		struct Input {
			float3 worldPos;
			float3 wNormal;
		};
			
		void vert (inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input,data);
			data.wNormal = mul(_Object2World, v.normal);
		}
		void surf (Input IN, inout SurfaceOutput o) 
		{
			float3 nDir = IN.wNormal;
			float3 wsPos = IN.worldPos;
			float3 blending = abs(nDir);
			blending = normalize(max(blending,0.00001));
			float b = (blending.x + blending.y + blending.z);
			blending /= float3(b,b,b);
			
			float4 xaxis = tex2D(_Side, wsPos.yz * _Side_ST.xy + _Side_ST.zw);
			//float4 xaxisN = tex2D(_SideNormal, wsPos.yz * _SideNormal_ST.xy + _SideNormal_ST.zw);
			float3 xaxisN = UnpackNormal(tex2D(_SideNormal, wsPos.yz * _SideNormal_ST.xy + _SideNormal_ST.zw));
			float4 yaxis;
			float3 yaxisN;
			if(nDir.y > 0)
			{
				yaxis = tex2D(_Top, wsPos.xz * _Top_ST.xy + _Top_ST.zw);
				//yaxisN = tex2D(_TopNormal, wsPos.xz * _TopNormal_ST.xy + _TopNormal_ST.zw);
				yaxisN = UnpackNormal(tex2D(_TopNormal, wsPos.xz * _TopNormal_ST.xy + _TopNormal_ST.zw));
			}
			else
			{
				yaxis = tex2D(_Bottom, wsPos.xz * _Bottom_ST.xy + _Bottom_ST.zw);
				//yaxisN = tex2D(_BottomNormal, wsPos.xz * _BottomNormal_ST.xy + _BottomNormal_ST.zw);
				yaxisN = UnpackNormal(tex2D(_BottomNormal, wsPos.xz * _BottomNormal_ST.xy + _BottomNormal_ST.zw));
			}
			float4 zaxis = tex2D(_Side, wsPos.xy * _Side_ST.xy + _Side_ST.zw);
			//float4 zaxisN = tex2D(_SideNormal, wsPos.xy * _SideNormal_ST.xy + _SideNormal_ST.zw);
			float3 zaxisN = UnpackNormal(tex2D(_SideNormal, wsPos.xy * _SideNormal_ST.xy + _SideNormal_ST.zw));
			float4 tex = xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;
			//float4 texN = xaxisN * blending.x + yaxisN * blending.y + zaxisN * blending.z;
			float3 texN = xaxisN * blending.x + yaxisN * blending.y + zaxisN * blending.z;
			o.Albedo = tex;
			//o.Normal = normalize(nDir + texN);
			o.Normal = texN;
		} 
		ENDCG
	}
	Fallback "Diffuse"
}