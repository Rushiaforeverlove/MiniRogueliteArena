Shader "AbyssBar/HubOutline"
{
    Properties{[PerRendererData]_MainTex("Sprite",2D)="white"{} [HideInInspector]_Flip("Flip",Vector)=(1,1,1,1) _Radius("Radius in texture pixels",Float)=3 _UvRect("Sprite UV bounds",Vector)=(0,0,1,1)}
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize,_UvRect;float _Radius;
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct outline_v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            outline_v2f vert(appdata v){outline_v2f o;o.vertex=UnityObjectToClipPos(UnityFlipSprite(v.vertex,_Flip));o.uv=v.uv;return o;}
            float sampleAlpha(float2 uv){if(uv.x<_UvRect.x||uv.y<_UvRect.y||uv.x>_UvRect.z||uv.y>_UvRect.w)return 0;return step(.3,tex2D(_MainTex,uv).a);}
            fixed4 frag(outline_v2f i):SV_Target
            {
                float center=sampleAlpha(i.uv);float edge=0,soft=0;float2 d=_MainTex_TexelSize.xy*_Radius;
                for(int n=0;n<8;n++){float a=n*.78539816;float2 direction=float2(cos(a),sin(a));edge=max(edge,sampleAlpha(i.uv+direction*d));soft=max(soft,sampleAlpha(i.uv+direction*d*2.2));}
                return fixed4(1,1,1,(1-center)*max(edge*.95,soft*.22));
            }
            ENDCG
        }
    }
}
