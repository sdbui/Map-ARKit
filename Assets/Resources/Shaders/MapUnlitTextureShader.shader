// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Map/MapUnlitTextureShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _xClipMin ("X Clip Min", float) = -1
        _xClipMax ("X Clip Max", float) = 1
        _zClipMin ("Z Clip Min", float) = -1
        _zClipMax ("Z Clip Max", float) = 1
		_xzRotation ("XZ Rotation", float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct VSIN 
			{
				float4 pos : POSITION;
				float2 tex : TEXCOORD0;
			};

			struct PSIN
			{
				float4 pos : SV_POSITION;
				float2 tex : TEXCOORD0;
				float4 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
	        float _xClipMin, _xClipMax, _zClipMin, _zClipMax;
			float _xzRotation;

			PSIN vert(VSIN i)
			{
				PSIN o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.worldPos = mul(UNITY_MATRIX_M, i.pos);
				o.tex = i.tex;
				return o;
			}
			
			fixed4 frag (PSIN i) : SV_Target
			{
				float3 _xzClipCenter = float3((_xClipMax + _xClipMin) * 0.5, 0, (_zClipMax + _zClipMin) * 0.5);
				float3 tmpVec = i.worldPos - _xzClipCenter;
				float origX = tmpVec.x * cos(-_xzRotation) - tmpVec.z * sin(-_xzRotation) + _xzClipCenter.x;
				float origZ = tmpVec.x * sin(-_xzRotation) + tmpVec.z * cos(-_xzRotation) + _xzClipCenter.z;
			
				clip(origX - _xClipMin);
				clip(_xClipMax - origX);
				clip(origZ - _zClipMin);
				clip(_zClipMax - origZ);

				fixed4 col = tex2D(_MainTex, i.tex);
				return col;
			}
			ENDCG
		}
	}
}
