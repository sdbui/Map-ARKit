Shader "Map/MapTransparentSurfaceShader+1" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        _xClipMin ("X Clip Min", float) = -1
        _xClipMax ("X Clip Max", float) = 1
        _zClipMin ("Z Clip Min", float) = -1
        _zClipMax ("Z Clip Max", float) = 1
		_xzRotation ("XZ Rotation", float) = 0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float3 worldPos;
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
        float _xClipMin, _xClipMax, _zClipMin, _zClipMax;
		float _xzRotation;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 _xzClipCenter = float3((_xClipMax + _xClipMin) * 0.5, 0, (_zClipMax + _zClipMin) * 0.5);
            float3 tmpVec = IN.worldPos - _xzClipCenter;
            float origX = tmpVec.x * cos(-_xzRotation) - tmpVec.z * sin(-_xzRotation) + _xzClipCenter.x;
            float origZ = tmpVec.x * sin(-_xzRotation) + tmpVec.z * cos(-_xzRotation) + _xzClipCenter.z;
           
        	clip(origX - _xClipMin);
        	clip(_xClipMax - origX);
        	clip(origZ - _zClipMin);
        	clip(_zClipMax - origZ);

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
