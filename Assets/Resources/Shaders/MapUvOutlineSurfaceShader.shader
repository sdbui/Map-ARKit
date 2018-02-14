// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Map/MapUvOutlineSurfaceShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _uLowCutoff ("U Low Cutoff", Range(0,1)) = 0.05
        _uHighCutoff ("U High Cutoff", Range(0,1)) = 0.95
        _vLowCutoff ("V Low Cutoff", Range(0,1)) = 0.015
        _vHighCutoff ("V High Cutoff", Range(0,1)) = 0.985
        _xClipMin ("X Clip Min", float) = -1
        _xClipMax ("X Clip Max", float) = 1
        _zClipMin ("Z Clip Min", float) = -1
        _zClipMax ("Z Clip Max", float) = 1
        _xzRotation ("XZ Rotation", float) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
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

        fixed4 _Color;
        fixed4 _OutlineColor;
        float _uLowCutoff, _uHighCutoff, _vLowCutoff, _vHighCutoff;
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

            int isOutline = 0;
            isOutline = isOutline || IN.uv_MainTex.x < _uLowCutoff;
            isOutline = isOutline || IN.uv_MainTex.x > _uHighCutoff;
            isOutline = isOutline || IN.uv_MainTex.y < _vLowCutoff;
            isOutline = isOutline || IN.uv_MainTex.y > _vHighCutoff;

            o.Albedo = fixed4(0, 0, 0, 0);
            o.Albedo += _Color * !isOutline;
            o.Albedo += _OutlineColor * isOutline;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
