Shader "NeonCoast/NightSky" {
 Properties { _Top("Zenith", Color)=(.015,.025,.065,1) _Horizon("Horizon",Color)=(.12,.09,.22,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Top,_Horizon;
 struct appdata {float4 vertex:POSITION;};struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
 float4 frag(v2f i):SV_Target {float3 d=normalize(i.dir);float h=saturate(d.y);float3 c=lerp(_Horizon.rgb,_Top.rgb,pow(h,.45));float moon=dot(d,normalize(float3(-.5,.3,.8)));c+=float3(.65,.8,1)*(smoothstep(.9995,.9998,moon)*2+pow(saturate(moon),250)*.09);float2 cell=floor(d.xz/max(.1,d.y)*420);float hash=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);c+=step(.9988,hash)*smoothstep(.1,.4,h)*.18;return float4(c,1);}
 ENDHLSL }
 } Fallback Off
}
