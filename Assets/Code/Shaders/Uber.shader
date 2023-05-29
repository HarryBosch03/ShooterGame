Shader "Unlit/Uber"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/macros.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;

                float4 vertex : SV_POSITION;
                float3 pos : VAR_POSITION;
                float3 normal : VAR_NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.pos = TransformObjectToWorld(input.vertex);
                output.vertex = TransformWorldToHClip(output.pos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normal = TransformObjectToWorldNormal(input.normal);

                return output;
            }

            float4 _Color;

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                InputData inputData = (InputData)0;
                inputData.positionCS = input.vertex;
                inputData.positionWS = input.pos;
                inputData.normalWS = normalize(input.normal);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = col;

                float4 final = UniversalFragmentBlinnPhong(inputData, surfaceData);
                final.rgb += half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                
                return final; 
            }
            ENDHLSL
        }
    }
}