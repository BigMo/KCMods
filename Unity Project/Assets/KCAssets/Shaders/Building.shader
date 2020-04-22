Shader "Custom/Building" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
        _SnowColor ("SnowColor", Color) = (0.933, 0.933, 0.909,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_WallProgress ("WallProgress", Range(0,1)) = 1
        _Snow ("Snow", Range(0,1)) = 0
        _ScaffoldAdvance ("Scaffold Advance", Range(0,1)) = 0.2
		[Toggle] _ScaffoldMaxOverride("Max Scaffold Override", float) = 0
	    _MinYAdjustment("MinYAdjustment", float) = 0.0
	    _MaxYAdjustment("MaxYAdjustment", float) = 0.0
        _MinHeight("MinHeight", float) = 0.0
        _MaxHeight("MaxHeight", float) = 1.0
        _AlphaCutoff("Alpha Cutoff", float) = 0.5
        _FrameSize("Construct Frame Size", float) = 0.1
		_BreakPoint("Break Point", Vector) = (0,0,0,0)
		_BreakPoint("Break Offset", Vector) = (0,0,0,0)
		_DamageFlash("DamageFlash", Range(0,1)) = 0
        [Toggle] _Invalid("Invalid", float) = 0
        [Toggle] _Disabled("Disabled", float) = 0
	}
	SubShader {
		Tags { "RenderType"="TranparentCutout"}
		LOD 200
		Cull Off

		CGPROGRAM

        
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alphatest:_AlphaCutoff addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		
		

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
		};

		fixed4 _Color;
        float _MinHeight;
        float _MaxHeight;
		float _MinYAdjustment;
		float _MaxYAdjustment;
        float _WallProgress;
        float _ScaffoldAdvance;
		float _ScaffoldMaxOverride;
        float _Snow;
        fixed4 _SnowColor;
		float _DamageFlash;
        
        float _FrameSize;
        float _Invalid;
        float _Disabled;

		float4 _BreakPoint;
		float4 _BreakOffset;
        
		//When first(x) is less than second(y) equal 1, otherwise 0
        float when_lt(float x, float y) {
            return max(sign(y - x), 0.0);
        }
        
		//When first(x) is greater than second(y) equal 1, otherwise 0
        float when_gt(float x, float y) {
            return max(sign(x - y), 0.0);
        }

		float time = 100.0f;
		float2 resolution = 100.0f * 0.5f;

		float hash(float n) {
			return frac(sin(n)*43758.5453);
		}

		float noise(float2 uv) {
			float3 x = float3(uv, 0);

			float3 p = floor(x);
			float3 f = frac(x);

			f = f*f*(3.0 - 2.0*f);
			float n = p.x + p.y*57.0 + 113.0*p.z;

			return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
				lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
				lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
					lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
		}

		float2x2 m = float2x2(0.8, 0.6, -0.6, 0.8);

		float fbm(float2 p)
		{
			float f = 0.0;
			f += 0.5000*noise(p); p = mul(p, m*2.02);
			f += 0.2500*noise(p); p = mul(p, m*2.03);
			f += 0.1250*noise(p); p = mul(p, m*2.01);
			f += 0.0625*noise(p);
			f /= 0.9375;
			return f;
		}

		float3 voronoi(in float2 x)
		{
			int2 p = int2(floor(x));
			float2 f = frac(x);

			int2 mb = int2(0, 0);
			float2 mr = float2(0.0, 0.0);
			float2 mg = float2(0.0, 0.0);

			float md = 8.0;
			for (int j = -1; j <= 1; ++j)
				for (int i = -1; i <= 1; ++i)
				{
					int2 b = int2(i, j);
					float2  r = float2(b) + noise(float2(p + b)) - f;
					float2 g = float2(float(i), float(j));
					float n = noise(float2(p)+g);
					float2 o = float2(n, n);
					float d = length(r);

					if (d<md)
					{
						md = d;
						mr = r;
						mg = g;
					}
				}

			md = 8.0;
			for (int j = -2; j <= 2; ++j)
				for (int i = -2; i <= 2; ++i)
				{
					int2 b = float2(i, j);
					float2 r = float2(b) + noise(float2(p + b)) - f;


					if (length(r - mr)>0.00001)
						md = min(md, dot(0.5*(mr + r), normalize(r - mr)));
				}
			return float3(md, mr);
		}

		float expEaseIn(float t, float b, float c, float d)
		{
			return c * pow(2.0, 10.0 * (t / d - 1.0)) + b;
		}

        float3 threeDField(float3 pos, float3 normal, float frameSize, float thickness){
            float3 offset = {thickness/2, thickness/2, thickness/2};
            float3 modVec = fmod(pos + offset, frameSize);
            bool x = modVec.x <= thickness;
            bool y = abs(modVec.y) <= thickness;
            bool z = modVec.z <= thickness;
            float3 field = {x, y, z};
            normalize(field);
            float3 zero = {0,0,0};
            field = max(zero, field - abs(normal));
            
            return length(field) > 0.5;
        }

		void surf (Input IN, inout SurfaceOutputStandard o) {
			resolution = _ScreenParams.xy;

			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			
			fixed4 lerpedSnow = lerp(c, _SnowColor, _Snow);

			float useSnow = when_gt(dot(IN.worldNormal, fixed3(0, 1, 0)), 0.5f);
			useSnow *= when_gt(IN.worldPos.y,0.02f);

			fixed4 real = lerp(c, lerpedSnow, useSnow * 0.875f);

			//Grayscale
            float gray = dot(c.rgb, float3(0.2, 0.39, 0.05));

			//Create semi red color
            float4 purered = {1, 0, 0, 1};
            float4 red = lerp(purered, real, 0.2);
            
			//Build gray or red, based on settings
            o.Albedo = lerp(lerp(real.xyz, red.xyz, _Invalid), gray, _Disabled); 

            o.Alpha = 0;
			o.Metallic = 0;
			o.Smoothness = 0;

			float adjustedMin = _MinHeight + _MinYAdjustment;
			float adjustedMax = _MaxHeight + _MaxYAdjustment;

			
			float scaffoldCutoff = lerp(lerp(adjustedMin, adjustedMax + _ScaffoldAdvance, _WallProgress), _MaxHeight-0.001, _ScaffoldMaxOverride);
			float builtCutoff = lerp(adjustedMin - _ScaffoldAdvance, adjustedMax + lerp(0.001f, -0.001, _ScaffoldMaxOverride), _WallProgress);

			o.Alpha = o.Alpha + when_lt(IN.worldPos.y, scaffoldCutoff) * threeDField(IN.worldPos, IN.worldNormal, _FrameSize, 0.03);
			o.Alpha = o.Alpha + when_lt(IN.worldPos.y, builtCutoff);
			

			o.Albedo = lerp(o.Albedo, o.Albedo * 0.5f, when_gt(IN.worldPos.y, builtCutoff));

			o.Albedo = lerp(o.Albedo, float3(1, 1, 1), _DamageFlash);

			float3 worldWithOffset = IN.worldPos + _BreakOffset;

			float2 fc = float2((worldWithOffset.x + worldWithOffset.y), (worldWithOffset.z + worldWithOffset.y));

			float3 col = float3(1.0, 1.0, 1.0);

			float vor = 1.0 - voronoi(fc / 0.30).x;
			float radius = _BreakPoint.w;
			float cv = clamp(radius - length(_BreakPoint.xyz - IN.worldPos), 0.0, 1.0);
			
			o.Albedo = lerp(float3(0, 0, 0),
				o.Albedo,
				smoothstep(0.0,
					0.07 * cv,
					1.0 - vor));
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
