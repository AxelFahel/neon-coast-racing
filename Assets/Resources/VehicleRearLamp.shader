Shader "NeonCoast/RearLamp"
{
 Properties { _BaseColor("Lens", Color) = (0.3,0,0,1) [HDR] _EmissionColor("LED", Color) = (0.6,0,0,1) }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
  Pass {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Cull Off ZWrite On ZTest LEqual
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   half4 _EmissionColor;
   CBUFFER_END
   struct Attributes { float4 positionOS : POSITION; };
   struct Varyings { float4 positionCS : SV_POSITION; };
   Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
   half4 Frag(Varyings i) : SV_Target { return half4(min(_BaseColor.r*.2h+_EmissionColor.r,3.0h),0,0,1); }
   ENDHLSL
  }
 }
}
