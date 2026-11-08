Shader "AbyssBar/HubSprite"
{
    Properties{[PerRendererData]_MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) [HideInInspector]_Flip("Flip",Vector)=(1,1,1,1) _Cutoff("Alpha cleanup",Range(0,1))=.2}
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #include "UnitySprites.cginc"
            float _Cutoff;
            fixed4 frag(v2f input):SV_Target{fixed4 c=SampleSpriteTexture(input.texcoord)*input.color;clip(c.a-_Cutoff);return c;}
            ENDCG
        }
    }
}
