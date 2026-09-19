Shader "NeonCoast/CoastalWater" {
 Properties { _BaseColor("Deep water",Color)=(.018,.045,.075,1) }
 SubShader { Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial) float4 _BaseColor; CBUFFER_END
 struct A {float4 positionOS:POSITION;};struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float fog:TEXCOORD1;};
 V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 float4 frag(V i):SV_Target {float t=_Time.y;float2 p=i.world.xz;float x=sin(p.x*.9+p.y*.24+t*.7)*.045+sin(p.x*2.4-p.y*.7+t)*.016;float z=cos(p.y*.8+p.x*.14+t*.45)*.045+cos(p.y*2.1+t*.8)*.018;float3 n=normalize(float3(x,1,z));float3 v=normalize(GetWorldSpaceViewDir(i.world));float f=pow(1-saturate(dot(n,v)),4);Light l=GetMainLight();float spec=pow(saturate(dot(n,normalize(v+l.direction))),160);float3 color=lerp(_BaseColor.rgb,float3(.075,.11,.19),f)+l.color*spec*.8;return float4(MixFog(color,i.fog),1);}
 ENDHLSL
 }}
}
