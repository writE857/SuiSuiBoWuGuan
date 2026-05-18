Shader "Shader Graphs/Additive Sprite"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		[NoScaleOffset] _BaseMap ("BaseMap", 2D) = "white" {}
		[HideInInspector] White ("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			sampler2D _BaseMap;
			float4 _BaseMap_ST;
			fixed4 _Color;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.color = v.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return tex2D(_BaseMap, i.uv) * i.color;
			}
			ENDCG
		}
	}
}
