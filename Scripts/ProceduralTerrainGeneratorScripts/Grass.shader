// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Grass"
{
	Properties{
		[HDR] _BackgroundColor("Background Color", Color) = (1,0,0,1)
		[HDR]_ForegroundColor("Foreground Color", Color) = (0,0,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Cutoff("Cutoff", Range(0,1)) = 0.25
		_GrassHeight("Grass Height", Float) = 0.25
		_GrassWidth("Grass Width", Float) = 0.25
		_WindSpeed("Wind Speed", Float) = 100
		_WindStength("Wind Strength", Float) = 0.05
		_MaxViewDistance("Maximum View Distance", Float) = 200.0
		_MaxHeight("Maximum Height", Float) = 90.0
		_MinHeight("Minimum Height", Float) = 0.0
	}
	SubShader{
		Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "Opaque" "LightMode" = "ForwardBase" /*"LightMode" = "ShadowCaster"*/ }
		LOD 200

		Pass {
			CULL OFF

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom 

			//#pragma alphatest : _Cutoff

			// shadow helper functions and macros
			#include "AutoLight.cginc"

			// Use shader model 4.0 target, we need geometry shader support
			#pragma target 4.0

			const static int numberOfTotalBiomes = 10;
			UNITY_DECLARE_TEX2DARRAY(textures);
			float temperatures[numberOfTotalBiomes];
			float minHeights[numberOfTotalBiomes];

			sampler2D _MainTex;

			struct v2g {
				float4 pos : SV_POSITION;
				float3 norm : NORMAL;
				float2 uv : TEXCOORD0;
				float3 color : TEXCOORD1;
			};

			struct g2f {
				float4 pos : SV_POSITION;
				float3 norm : NORMAL;
				float2 uv : TEXCOORD0;
				float4 diffuseColor : TEXCOORD2;
				SHADOW_COORDS(1) // put shadows data into TEXCOORD1
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				//float3 specularColor : TEXCOORD2;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _BackgroundColor;
			fixed4 _ForegroundColor;
			half _GrassHeight;
			half _GrassWidth;
			half _Cutoff;
			half _WindStength;
			half _WindSpeed;
			float _MaxViewDistance;
			float _MaxHeight;
			float _MinHeight;

			float numberOfBiomes;

			float rand(float3 co) {
				return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
			}

			float getDistanceToCamera(float3 grassPosition) {
				return sqrt(pow(abs(_WorldSpaceCameraPos.x - grassPosition.x), 2) + pow(abs(_WorldSpaceCameraPos.z - grassPosition.z), 2));
			}

			float inverseLerp(float a, float b, float value) {
				return saturate((value - a) / (b - a));
			}

			int biomePicker(float color) {
				int selection = -1;
				for (int i = 0; i < numberOfBiomes; i++) {
					if (color == temperatures[i]) {
						selection = i;
						break;
					}
				}

				return selection;
			}

			v2g vert(appdata_full v) {
				float3 v0 = v.vertex.xyz;

				v2g OUT;
				OUT.pos = v.vertex;
				OUT.norm = v.normal;
				OUT.uv = v.texcoord;
				OUT.color = v.color.rgb;

				return OUT;
			}

			[maxvertexcount(24)]
			void geom(point v2g IN[1], inout TriangleStream<g2f> triStream) {
				float3 lightPosition = _WorldSpaceLightPos0;
				float heightRandomness = (rand(IN[0].pos) % 10);
				if (heightRandomness < 0.7f) {
					heightRandomness = 0.7f;
				}

				if (IN[0].color.r == -1)
					return;

				int biomeId = biomePicker(IN[0].color.r);
				if (biomeId == -1) {
					return;
				}
				else {
					IN[0].color.r = biomeId;
				}

				float3 perpendicularAngle = float3(0, 0, 1);
				float3 faceNormal = cross(perpendicularAngle, IN[0].norm);

				float3 v0 = IN[0].pos.xyz;
				float3 v1 = IN[0].pos.xyz + IN[0].norm * _GrassHeight * heightRandomness;

				float3 wind = float3(sin(_Time.x * _WindSpeed + v0.x) + sin(_Time.x * _WindSpeed + v0.z * 2) + sin(_Time.x * _WindSpeed * 0.1 + v0.x), 0,
					cos(_Time.x * _WindSpeed + v0.x * 2) + cos(_Time.x * _WindSpeed + v0.z));
				v1 += wind * _WindStength;

				float4 color;

				float3 blendAxes = abs(IN[0].norm);
				blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

				float slopeMultiplier;
				if (blendAxes.x < blendAxes.z) {
					slopeMultiplier = blendAxes.z;
				}
				else {
					slopeMultiplier = blendAxes.x;
				}

				float3 worldPos = mul(unity_ObjectToWorld, IN[0].pos.xyz).xyz;

				float heightPercent = inverseLerp(_MinHeight, _MaxHeight, worldPos.y);

				if (heightPercent >= minHeights[biomeId] + (slopeMultiplier * 0.2f)) {
					float distanceToCamera = getDistanceToCamera(IN[0].pos.xyz);
					if (distanceToCamera >= _MaxViewDistance) {
						distanceToCamera = _MaxViewDistance;
					}

					float alphaDistance = abs((distanceToCamera / _MaxViewDistance) - 1);

					if (blendAxes.x < 0.3f && blendAxes.z < 0.3f) {
						color = float4(IN[0].color.x, IN[0].color.y, IN[0].color.z, alphaDistance);
					}
					else {
						return;
					}
				} else {
					return;
				}

				float sin30 = 0.5;
				float sin60 = 0.866f;
				float cos30 = sin60;
				float cos60 = sin30;

				half3 worldNormal = UnityObjectToWorldNormal(IN[0].norm);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

				g2f OUT;

				// Quad 1

				OUT.pos = UnityObjectToClipPos(v0 + perpendicularAngle * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(1, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1 + perpendicularAngle * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(1, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1 - perpendicularAngle * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0 - perpendicularAngle * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				// Quad 2

				OUT.pos = UnityObjectToClipPos(v0 + float3(sin60, 0, -cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(1, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1 + float3(sin60, 0, -cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(1, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0 - float3(sin60, 0, -cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1 - float3(sin60, 0, -cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				// Quad 3

				OUT.pos = UnityObjectToClipPos(v0 + float3(sin60, 0, cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(1, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1 + float3(sin60, 0, cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(1, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0 - float3(sin60, 0, cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1 - float3(sin60, 0, cos60) * 0.5 * _GrassHeight * heightRandomness);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v0);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 0);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);

				OUT.pos = UnityObjectToClipPos(v1);
				OUT.norm = faceNormal;
				OUT.diffuseColor = color;
				OUT.uv = float2(0.5, 1);
				OUT.diff = nl * _LightColor0.rgb;
				OUT.ambient = ShadeSH9(half4(worldNormal, 1));
				TRANSFER_SHADOW(OUT);
				triStream.Append(OUT);
			}

			half4 frag(g2f IN) : COLOR{
				fixed4 c = UNITY_SAMPLE_TEX2DARRAY(textures, float3(IN.uv.x, IN.uv.y, IN.diffuseColor.r));

				c.a = c.a * IN.diffuseColor.a;
				fixed shadow = SHADOW_ATTENUATION(IN);
				// darken light's illumination with shadow, keep ambient intact
				fixed3 lighting = IN.diff * shadow + IN.ambient;
				c.rgb *= lighting;

				clip(c.a - _Cutoff);

				return c;
			}
			
			ENDCG

		}
		
		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
