Shader "Abyss Bar/Mio/Energy V4"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Source alpha cleanup", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha One
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; fixed4 _Color; float _Cutoff;
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(Output v):SV_Target
            {
                fixed4 pixel=tex2D(_MainTex,v.uv);
                // Clean only the source's faint rectangular alpha fringe.
                // Keep renderer alpha separate so fades remain continuous.
                clip(pixel.a-_Cutoff);
                return pixel*v.color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
