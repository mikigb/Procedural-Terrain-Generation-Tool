using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BiomeController {
    public Biome[] biomes; // max is 10
    [Range(0.1f, 1)]
    public float slopeThreshold = 0.4f;     // slope value to change from primary to secondary textures
}

[System.Serializable]
public class Biome {
    public float randomness;
    public float maxHeight;
    public List<GroundTexture> groundTextures;
    public GroundTextureSloped groundTextureSloped;
    public _3dModel[] largeModels;
    public _3dModel[] smallModels;
    public GrassModel[] grassModels;
}

[System.Serializable]
public class GroundTexture {
    public float minHeight;
    public Texture2D texture;
}

[System.Serializable]
public class GroundTextureSloped {
    public Texture2D texture;
}

[System.Serializable]
public class _3dModel {
    public float randomness;
    public GameObject model;
}

[System.Serializable]
public class GrassModel {
    public float randomness;
    public float minHeight;
    public Texture2D modelTexture;
}
