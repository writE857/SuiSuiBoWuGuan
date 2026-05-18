Shader "Fix/Utils/R2G"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Cutoff("Cutoff",range(0,1))=0.1
        _RedThreshold("RedThreshold",float)=0
        _Color("Color",color)=(1,1,1,1)
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
        CGINCLUDE
        float _RedThreshold;

        float3 rgb2hsv(float3 rgb)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
            float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                          d / (q.x + e),
                          q.x);
        }

        float3 hsv2rgb(float3 hsv)
        {
            float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
            return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
        }

        float isRedColor(float3 rgb)
        {
            // 红色检测公式：R > G * 1.5 且 R > B * 1.5
            float redStrength = smoothstep(_RedThreshold, _RedThreshold + 0.2, rgb.r);
            float greenRatio = smoothstep(0.5, 0.7, rgb.r / (rgb.g + 0.001));
            float blueRatio = smoothstep(0.5, 0.7, rgb.r / (rgb.b + 0.001));

            return redStrength * greenRatio * blueRatio;
        }

        float3 r2g(float3 rgb)
        {
            float3 hsv = rgb2hsv(rgb);
            // 转换回RGB

            // 将红色色调偏移120度（0.333）到绿色区域
            if (isRedColor(rgb) > 0.1)
            {
                // 色调偏移：红色(0°) → 绿色(120°)
                hsv.x = fmod(hsv.x + 0.333, 1.0);

                // 增强饱和度使绿色更鲜艳
                // hsv.y = saturate(hsv.y * _SaturationBoost);
            }
            rgb = hsv2rgb(hsv);

            return rgb;
        }

        float4 Convert(float4 color)
        {
            color.rgb = r2g(color.rgb);
            return color;
        }
        ENDCG
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float4 _Color;


            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                const float4 col = tex2D(_MainTex, i.uv);
                if (col.a <= _Cutoff)discard;
                return Convert(col) * _Color;
            }
            ENDCG
        }
    }
    Fallback Off
}