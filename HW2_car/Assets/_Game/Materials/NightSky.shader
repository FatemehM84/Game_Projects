Shader "NeonHighway/NightSky" {
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" } Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes { float4 positionOS:POSITION; }; struct Varyings { float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0; };
 Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
 half4 frag(Varyings i):SV_Target {float3 d=normalize(i.direction);float h=saturate(d.y);float3 color=lerp(float3(.07,.025,.014),float3(.004,.003,.009),pow(h,.5));float2 uv=d.xz/(abs(d.y)+.2)*220;float2 cell=floor(uv);float hash=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);float star=smoothstep(.08,0,length(frac(uv)-.5))*step(.993,hash)*step(.05,d.y);color+=star*float3(1.2,.83,.56);return half4(color,1);}
 ENDHLSL }
 }
}
