Shader "Custom/Terrain" {
    Properties{
        grassScale("Scale", Float) = 1
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _MainTex2("Albedo (RGB)", 2D) = "white" {}
        _MainTex3("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        float maxHeight;
        float minHeight;

        const static int numberOfColors = 10;
        const static float epsilon = 1E-4;
        float3 colors[numberOfColors];
        float colorSlopes[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_1);
        float maxHeights_1[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_2);
        float maxHeights_2[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_3);
        float maxHeights_3[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_4);
        float maxHeights_4[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_5);
        float maxHeights_5[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_6);
        float maxHeights_6[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_7);
        float maxHeights_7[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_8);
        float maxHeights_8[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_9);
        float maxHeights_9[numberOfColors];
        UNITY_DECLARE_TEX2DARRAY(biome_10);
        float maxHeights_10[numberOfColors];

        float scales[numberOfColors];
        float blends[numberOfColors];

        float grassScale;

        sampler2D _MainTex;
        sampler2D _MainTex2;
        sampler2D _MainTex3;

        float numberOfBiomes;
        float numberOfTextures[numberOfColors];

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
    // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
    // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

        struct Input {
            float3 worldPos;
            float3 worldNormal;
            float4 color: COLOR;
            float2 uv_MainTex : TEXCOORD0;
            float2 uv2_MainTex: TEXCOORD1;
            float2 uv3_MainTex2: TEXCOORD2;
            float2 uv4_MainTex3: TEXCOORD3;
            INTERNAL_DATA
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value - a) / (b - a));
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex, int biome) {
            float3 scaledWorldPos = worldPos / scale;

            float3 xProjection = 0;
            float3 yProjection = 0;
            float3 zProjection = 0;

            switch (biome) {
                case 0:
                    xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_1, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                    yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_1, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                    zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_1, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                    break;
                case 1:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_2, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_2, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_2, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 2:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_3, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_3, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_3, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 3:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_4, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_4, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_4, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 4:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_5, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_5, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_5, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 5:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_6, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_6, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_6, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 6:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_7, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_7, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_7, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 7:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_8, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_8, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_8, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 8:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_9, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_9, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_9, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
                case 9:
                     xProjection = UNITY_SAMPLE_TEX2DARRAY(biome_10, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
                     yProjection = UNITY_SAMPLE_TEX2DARRAY(biome_10, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
                     zProjection = UNITY_SAMPLE_TEX2DARRAY(biome_10, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
                     break;
            }
            

            return xProjection + yProjection + zProjection;
        }

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv2_MainTex = v.texcoord1.xy;
            o.uv3_MainTex2 = v.texcoord2.xy;
            o.uv4_MainTex3 = v.texcoord3.xy;
        }

        void surf(Input IN, inout SurfaceOutputStandard o) {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            float3 blendAxes = abs(WorldNormalVector(IN, o.Normal));
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            float slopeMultiplier;
            if (blendAxes.x < blendAxes.z) {
                slopeMultiplier = blendAxes.z;
            }
            else {
                slopeMultiplier = blendAxes.x;
            }


            for (int i = 0; i < 2; i++) {
                float drawStrength = inverseLerp(-(blends[i] / 2) - epsilon, blends[i] / 2, slopeMultiplier - colorSlopes[i]);
                float3 textureColor;
                
                if (i == 0) {
                    for (int j = 0; j < numberOfBiomes; j++) {
                        float3 textureColor;

                        float data = 0;

                        float maxHeights[numberOfColors];

                        switch (j) {
                            case 0:
                                data = IN.color.r;
                                maxHeights = maxHeights_1;
                                break;
                            case 1:
                                data = IN.color.g;
                                maxHeights = maxHeights_2;
                                break;
                            case 2:
                                data = IN.color.b;
                                maxHeights = maxHeights_3;
                                break;
                            case 3:
                                data = IN.color.a;
                                maxHeights = maxHeights_4;
                                break;
                            case 4:
                                data = IN.uv2_MainTex.x;
                                maxHeights = maxHeights_5;
                                break;
                            case 5:
                                data = IN.uv2_MainTex.y;
                                maxHeights = maxHeights_6;
                                break;
                            case 6:
                                data = IN.uv3_MainTex2.x;
                                maxHeights = maxHeights_7;
                                break;
                            case 7:
                                data = IN.uv3_MainTex2.y;
                                maxHeights = maxHeights_8;
                                break;
                            case 8:
                                data = IN.uv4_MainTex3.x;
                                maxHeights = maxHeights_9;
                                break;
                            case 9:
                                data = IN.uv4_MainTex3.y;
                                maxHeights = maxHeights_10;
                                break;
                        }

                        float drawStrength2 = data;

                        for (int k = 0; k < numberOfTextures[j]; k++) {
                            float drawStrength3 = inverseLerp(-(0.1f / 2) - epsilon, 0.1f / 2, heightPercent - maxHeights[k] - (slopeMultiplier * 0.2f)) * drawStrength2;
                            textureColor = triplanar(IN.worldPos, scales[i], blendAxes, k, j);
                            o.Albedo = o.Albedo * (1 - drawStrength3) + textureColor * drawStrength3;
                        }

                    }
                }
                else {
                    for (int j = 0; j < numberOfBiomes; j++) {
                        float3 textureColor;

                        float data = 0;

                        switch (j) {
                            case 0:
                                data = IN.color.r;
                                break;
                            case 1:
                                data = IN.color.g;
                                break;
                            case 2:
                                data = IN.color.b;
                                break;
                            case 3:
                                data = IN.color.a;
                                break;
                            case 4:
                                data = IN.uv2_MainTex.x;
                                break;
                            case 5:
                                data = IN.uv2_MainTex.y;
                                break;
                            case 6:
                                data = IN.uv3_MainTex2.x;
                                break;
                            case 7:
                                data = IN.uv3_MainTex2.y;
                                break;
                            case 8:
                                data = IN.uv4_MainTex3.x;
                                break;
                            case 9:
                                data = IN.uv4_MainTex3.y;
                                break;
                        }

                        float drawStrength2 = data;

                        float drawStrength3 = inverseLerp(-(0.01f / 2) - epsilon, 0.01f / 2, heightPercent - (slopeMultiplier * 0.2f)) * drawStrength2 * drawStrength;
                        textureColor = triplanar(IN.worldPos, scales[i], blendAxes, numberOfTextures[j], j);
                        o.Albedo = o.Albedo * (1 - drawStrength3) + textureColor * drawStrength3;
                    }
                }
            }

        }
        ENDCG
    }
    FallBack "Diffuse"
}
