// Original Ashlight shader, CC0. Built-in rendering pipeline; no particle module required.
Shader "Ashlight/CombatGlow" {
    Properties {
        _Color ("Tint", Color) = (1,1,1,1)
        _UseVertexColor ("Line colors", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _UseVertexColor;
            struct Input { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            Output vert(Input v) {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=_Color*lerp(fixed4(1,1,1,1),v.color,_UseVertexColor); o.uv=v.uv; return o;
            }
            fixed4 frag(Output i) : SV_Target {
                fixed4 color=i.color;
                float edge=smoothstep(0.0,0.18,i.uv.y)*smoothstep(0.0,0.18,1.0-i.uv.y);
                color.a*=lerp(1.0,edge,_UseVertexColor); return color;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
