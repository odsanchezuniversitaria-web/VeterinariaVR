Shader "Veterinaria/Marco de devolución en pantalla" {
    Properties { _BaseColor ("Estado de la zona", Color) = (.05,.57,.98,.24) }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass {
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 panel : TEXCOORD0; };
            Varyings Vert(Attributes input) {
                Varyings result;
                result.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                result.panel=input.positionOS.xy;
                return result;
            }
            half4 Frag(Varyings input) : SV_Target {
                float2 edge=.5-abs(input.panel);
                float2 aa=max(fwidth(input.panel),.0001);
                float2 sides=1-smoothstep(float2(.003,.005),float2(.003,.005)+aa,edge);
                float bracket=max(sides.x*(1-smoothstep(.065,.09,edge.y)),
                                  sides.y*(1-smoothstep(.04,.06,edge.x)));
                // The original trigger switches from blue to green when a bone is inside.
                float ready=step(_BaseColor.b+.08,_BaseColor.g);
                half alpha=max(bracket*max(.55h,_BaseColor.a),ready*.075);
                return half4(_BaseColor.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
