Shader "Ember/AbyssFog"
{
    Properties { _Boundary ("Boundary", Float) = 122 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float _Boundary;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS);
                output.positionCS=TransformWorldToHClip(world);output.world=world.xy;return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float distance=max(abs(input.world.x),abs(input.world.y));
                return half4(0,0,0,smoothstep(_Boundary-5,_Boundary,distance));
            }
            ENDHLSL
        }
    }
}
