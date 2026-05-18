Shader "Fix/Utils/FormatTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale_Offset("ScaleAndOffset",vector)=(1,1,0,0)
    }
    SubShader
    {


        CGINCLUDE
        #include "UnityCG.cginc"
        #pragma shader_feature ALPHA_TEXTURE
        sampler2D _MainTex;
        float4 _MainTex_ST;
        float4 _Scale_Offset;

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.uv = o.uv * _Scale_Offset.xy + _Scale_Offset.zw;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            const float inner = step(0, i.uv.x) * step(i.uv.x, 1) * step(0, i.uv.y) * step(i.uv.y, 1);
            fixed4 baseColor;
            #if ALPHA_TEXTURE
            baseColor = fixed4(0, 0, 0, 0);
            #else
            baseColor = fixed4(0, 0, 0, 1);
            #endif
            return inner * tex2D(_MainTex, i.uv) + (1 - inner) * baseColor;
        }
        ENDCG
        Pass
        {
            Name "Opaque"
            Tags
            {
                "RenderType"="Opaque"
                "Queue"="Geometry"
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
        Pass
        {
            Name "Transparent"
            Tags
            {
                "RenderType"="Transparent"
                "Queue"="Transparent"
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
        Pass
        {
            Name "Clear"
            ColorMask 0
        }
    }
}