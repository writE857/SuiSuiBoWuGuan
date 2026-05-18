Shader "Shader Graphs/Emissive Shader"
{
	Properties
	{
		_BaseColor ("BaseColor", Color) = (1,1,1,1)
		_EmissiveColor ("EmissiveColor", Color) = (1,1,1,1)
		[HDR] _EmissionColor ("EmissionColor", Color) = (0,0,0,1)
		_Multiplier ("Multiplier", Float) = 1
		_Smoothness ("Smoothness", Range(0, 1)) = 0
		_Metallic ("Metallic", Range(0, 1)) = 0
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Back
		ZWrite On

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			fixed4 _BaseColor;
			fixed4 _EmissiveColor;
			fixed4 _EmissionColor;
			float _Multiplier;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed3 baseCol = _BaseColor.rgb;
				fixed3 emission = (_EmissiveColor.rgb * max(_Multiplier, 0.0)) + _EmissionColor.rgb;
				return fixed4(baseCol + emission, _BaseColor.a);
			}
			ENDCG
		}
	}
}
