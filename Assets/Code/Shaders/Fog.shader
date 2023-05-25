Shader "Hidden/Fog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Value ("Fog Density", float) = 1.0
        _Color ("Fog Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _FarPlane ("Far Plane", float) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float3 _Color;
            float _Value, _FarPlane;
            
            float4 frag (Varyings i) : SV_Target
            {
                float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                float depth = SampleSceneDepth(i.uv);
                float3 pos = ComputeWorldSpacePosition(i.uv, depth, UNITY_MATRIX_I_VP);
                float dist = length(pos - _WorldSpaceCameraPos);
                
                float fog = pow(2.71, dist / 1000.0 * _Value) - 1.0;
                fog = clamp(fog, 0.0, 1.0);

                float isSkybox = (dist / _ProjectionParams.z) > _FarPlane;
                col = lerp(col, _Color, fog * !isSkybox);
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
