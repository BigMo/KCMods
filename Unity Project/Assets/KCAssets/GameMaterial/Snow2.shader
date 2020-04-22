// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/Snow2" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		
		_SnowColor("SnowColor", Color) = (0.933, 0.933, 0.909,1)
		_Snow("Snow", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		fixed4 _SnowColor;
		float _Snow;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		float when_gt(float x, float y) {
			return max(sign(x - y), 0.0);
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 texCol = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			
			fixed4 lerpedSnow = lerp(texCol,
								_SnowColor,
								_Snow);

			float r = 1 - clamp(IN.worldPos.y * 0.75 + 0.25,0.2,1.5);
			float useSnow = when_gt(dot(IN.worldNormal, fixed3(0, 1, 0)), 0);

			float snowMod = clamp(IN.worldPos.y + 0.25f,0,1);
			snowMod *= snowMod;
			fixed4 c = lerp(texCol, lerpedSnow, useSnow  * 0.875f * snowMod);

			
			o.Albedo = c.rgb - float3(r, r, r) * 0.3;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
