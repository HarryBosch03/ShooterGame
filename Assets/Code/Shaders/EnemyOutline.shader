Shader "Unlit/EnemyOutline"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Value ("Intensity", float) = 1.0
        _Exponent("Exponent", float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 dir : VAR_DIR;
                float3 normal : VAR_NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz).xyz;
                o.vertex = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.dir = _WorldSpaceCameraPos - worldPos;
                o.normal = TransformObjectToWorldNormal(v.normal);
                return o;
            }

            float4 _Color;
            float _Value, _Exponent;
            
            float4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float3 normal = normalize(i.normal);
                float d = pow(1.0 - dot(dir, normal), _Exponent) * _Value;
                return _Color * d;
            }
            ENDHLSL
        }
    }
}
