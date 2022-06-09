using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {
    const int textureSize = 512;
    const int grassTextureSize = 256;
    const TextureFormat textureFormat = TextureFormat.RGBA32;

    public static Material CreateTexturesForTerrainShader(Material material, float maxHeightValue, float terrainHeight, BiomeController biomeController) {
        Material mat = new Material(material);
        float maxHeight = terrainHeight * maxHeightValue;
        float minHeight = 0;
        mat.SetFloat("maxHeight", maxHeight);
        mat.SetFloat("minHeight", minHeight);

        Biome[] biomes = biomeController.biomes;

        Texture2DArray[] biomeTextures = new Texture2DArray[biomes.Length];
        for(int i = 0; i < biomes.Length; i++) {
            biomeTextures[i] = new Texture2DArray(textureSize, textureSize, biomes[i].groundTextures.Count + 1, textureFormat, true);
        }

        float[] colorSlopes = new float[2];
        colorSlopes[0] = 0;
        colorSlopes[1] = biomeController.slopeThreshold;

        float[] scales = new float[2];
        scales[0] = 2;
        scales[1] = 2;

        float[] blends = new float[2];
        blends[0] = 0.02f;
        blends[1] = 0.02f;

        float[] numberOfTextures = new float[biomes.Length];

        for (int i = 0; i < biomes.Length; i++) {
            for (int j = 0; j < biomes[i].groundTextures.Count; j++) {
                biomeTextures[i].SetPixels(biomes[i].groundTextures[j].texture.GetPixels(), j);
            }

            biomeTextures[i].SetPixels(biomes[i].groundTextureSloped.texture.GetPixels(), biomes[i].groundTextures.Count);
        }

        for (int i = 0; i < biomes.Length; i++) {
            biomeTextures[i].Apply();
            int textureNumber = i + 1;
            string textureName = "biome_" + textureNumber;
            mat.SetTexture(textureName, biomeTextures[i]);

            string maxHeightsName = "maxHeights_" + (i + 1);
            float[] maxHeights = new float[biomes[i].groundTextures.Count];

            for(int j = 0; j < biomes[i].groundTextures.Count; j++) {
                maxHeights[j] = biomes[i].groundTextures[j].minHeight;
            }

            numberOfTextures[i] = biomes[i].groundTextures.Count;

            mat.SetFloatArray(maxHeightsName, maxHeights);
        }

        mat.SetFloatArray("colorSlopes", colorSlopes);
        mat.SetFloatArray("scales", scales);
        mat.SetFloatArray("blends", blends);
        mat.SetFloatArray("numberOfTextures", numberOfTextures);
        mat.SetFloat("numberOfBiomes", biomes.Length);

        return mat;
    }

    public static Material[] CreateTexturesForGrassShaders(Material[] materials, BiomeController biomeController, int maxNumberOfGrassModels) {
        List<List<float>> biomes = new List<List<float>>(maxNumberOfGrassModels);
        List<List<float>> minHeights = new List<List<float>>(maxNumberOfGrassModels);
        List<Texture2DArray> textures2DArray = new List<Texture2DArray>(maxNumberOfGrassModels);

        int[] biomeLengths = new int[maxNumberOfGrassModels];
        for(int i = 0; i < maxNumberOfGrassModels; i++) {
            biomeLengths[i] = 0;
        }

        for (int i = 0; i < biomeController.biomes.Length; i++) {
            int numberOfTextures = biomeController.biomes[i].grassModels.Length;
            for (int j = 0; j < numberOfTextures; j++) {
                biomeLengths[j]++;
            }
        }

        for (int i = 0; i < biomeLengths.Length; i++) {
            int numberOfTextures = biomeLengths[i];
            if (numberOfTextures > 0) {
                textures2DArray.Add(new Texture2DArray(grassTextureSize, grassTextureSize, numberOfTextures, textureFormat, true));
                biomes.Add(new List<float>(numberOfTextures));
                minHeights.Add(new List<float>(numberOfTextures));
            }
        }

        for (int i = 0; i < biomeController.biomes.Length; i++) {
            int numberOfTextures = biomeController.biomes[i].grassModels.Length;
            for (int j = 0; j < numberOfTextures; j++) {
                biomes[j].Add(i);
                minHeights[j].Add(biomeController.biomes[i].grassModels[j].minHeight);

                textures2DArray[j].SetPixels(biomeController.biomes[i].grassModels[j].modelTexture.GetPixels(), biomes[j].Count - 1);
            }
        }

        for (int i = 0; i < maxNumberOfGrassModels; i++) {
            textures2DArray[i].Apply();
            materials[i].SetTexture("textures", textures2DArray[i]);
            materials[i].SetFloatArray("temperatures", biomes[i].ToArray());
            materials[i].SetFloatArray("minHeights", minHeights[i].ToArray());
            materials[i].SetFloat("numberOfBiomes", biomes[i].Count);
        }

        return materials;
    }
}

[System.Serializable]
public class ColorInfo {
    public string name;
    public Color color;
    public float minSlope;
    public Texture2D texture;
    public Texture2D texture2;
    public Texture2D texture3;
    public Texture2D texture4;
    public float textureHeight;
    public float scale;
    public float blend;
}

public enum TextureType {
    Color,
    Texture
}

[System.Serializable]
public class GrassBiomeInfo {
    public float[] temperatureValue;
    public Texture2D[] textures;
}
