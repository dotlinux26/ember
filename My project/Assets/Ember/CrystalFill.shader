Shader "Ember/CrystalFill"
{
 Properties
 {
  _MainTex("Sprite",2D)="white"{}
  [HideInInspector]_Color("Tint",Color)=(1,1,1,1)
  [PerRendererData]_FillLevel("Fill Level",Float)=1.0
  [PerRendererData]_TimeVal("Time Val",Float)=0.0
 }
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
    half4 _Color;float _FillLevel;float _TimeVal;
   CBUFFER_END
   Varyings Vert(Attributes input)
   {
    UNITY_SETUP_INSTANCE_ID(input);SetUpSpriteInstanceProperties();
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    Varyings o=(Varyings)0;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS=TransformObjectToHClip(input.positionOS);o.uv=input.uv;o.color=input.color*_Color*unity_SpriteColor;return o;
   }
   half4 Frag(Varyings input):SV_Target
   {
    half4 baseCol=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
    if(baseCol.a<=0.05)return half4(0,0,0,0);
    float wave=sin(_TimeVal*4.5+input.uv.x*28.0)*0.022;
    float surface=_FillLevel+wave;
    if(input.uv.y>surface)return half4(0,0,0,0);
    float distToSurface=abs(input.uv.y-surface);
    if(distToSurface<0.038)
    {
     half3 rimColor=lerp(half3(0.4,0.95,1.0),half3(1.0,1.0,1.0),saturate(1.0-distToSurface/0.038));
     return half4(rimColor,baseCol.a*input.color.a);
    }
    return half4(baseCol.rgb*input.color.rgb,baseCol.a*input.color.a);
   }
   ENDHLSL
  }
 }
}
