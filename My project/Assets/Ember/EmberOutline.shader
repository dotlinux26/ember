Shader "Ember/PixelOutline"
{
 Properties { _MainTex("Sprite",2D)="white"{} [HideInInspector]_Color("Tint",Color)=(1,1,1,1) [PerRendererData]_CellUV("Frame bounds",Vector)=(0,0,1,1) }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Blend SrcAlpha OneMinusSrcAlpha
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
   struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; };
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _MainTex_TexelSize;
   CBUFFER_START(UnityPerMaterial)
    half4 _Color;float4 _CellUV;
   CBUFFER_END
   Varyings Vert(Attributes input)
   {
    UNITY_SETUP_INSTANCE_ID(input);SetUpSpriteInstanceProperties();
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    Varyings o=(Varyings)0;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS=TransformObjectToHClip(input.positionOS);o.uv=input.uv;o.color=input.color*_Color*unity_SpriteColor;return o;
   }
   half A(float2 uv)
   {
    float2 lo=_CellUV.xy+_MainTex_TexelSize.xy*.5,hi=_CellUV.zw-_MainTex_TexelSize.xy*.5;
    if(any(uv<lo)||any(uv>hi))return 0;
    return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).a;
   }
   half4 Frag(Varyings input):SV_Target
   {
    float2 d=_MainTex_TexelSize.xy;half a=A(input.uv),edge=0;
    edge=max(edge,A(input.uv+float2(d.x,0)));edge=max(edge,A(input.uv-float2(d.x,0)));
    edge=max(edge,A(input.uv+float2(0,d.y)));edge=max(edge,A(input.uv-float2(0,d.y)));
    return half4(input.color.rgb,saturate(edge-a)*input.color.a);
   }
   ENDHLSL
  }
 }
}
