// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/AlphaShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		// No culling or depth
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off ZWrite On ZTest Always

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float when_eq(float x, float y) {
				return 1.0 - abs(sign(x - y));
			}

			float when_neq(float x, float y) {
				return abs(sign(x - y));
			}

			float and(float a, float b) {
				return a * b;
			}

			float or(float a, float b) {
				return min(a + b, 1.0);
			}


			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				float showPixel = or(when_neq(col.r + col.b, 2.00), when_neq(col.g, 0));
				return fixed4(col.r, col.g, col.b, showPixel);
			}
			ENDCG
		}
	}
}
