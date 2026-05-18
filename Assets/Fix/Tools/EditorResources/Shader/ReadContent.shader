Shader "Fix/Utils/ReadContent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RotateMode ("RotateMode", int) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }
        // No culling or depth

        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        sampler2D _MainTex;
        int _RotateMode;

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        void clipOther(float2 uv)
        {
        }


        half4 frag(v2f i) : SV_Target
        {
            float2 uv;
            switch (_RotateMode)
            {
            case 0: uv = i.uv;
                break;
            case 1: uv = float2(1 - i.uv.x, i.uv.y);
                break;
            case 2: uv = float2(i.uv.x, 1 - i.uv.y);
                break;
            case 3: uv = float2(1 - i.uv.x, 1 - i.uv.y);
                break;
            case 4: uv = i.uv;
                break;
            default: uv = i.uv;
                break;
            }
            half4 col = tex2D(_MainTex, uv);
            clipOther(uv);
            return col;
        }
        ENDCG
        Pass
        {
            Tags
            {
                "RenderType"="Transparent"
                "Queue"="Transparent"
                "IgnoreProjector"="True"
                "PreviewType"="Plane"
            }
            Cull Off
            ZWrite Off
            ZTest Always
            Blend Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}