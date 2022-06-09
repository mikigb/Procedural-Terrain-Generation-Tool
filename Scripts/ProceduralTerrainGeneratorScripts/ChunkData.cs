using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ChunkData {
    public Chunk chunk;
    public bool isNoiseCalculated; // when the noise values are calculated -> true
    public bool isGenerated;       // when the mesh is generated -> true
    public int levelOfDetail;
    public float distanceToCenter;
    public bool colliderGenerated;
    public bool meshCalculated;
    public bool meshGenerated;
    public MeshInfo meshInfo;
    public int meshQueueInfoIndex;
    public bool treesCalculated;
    public bool treesGenerated;
    public List<Tree> trees;
    public bool rocksCalculated;
    public bool rocksGenerated;
    public List<Tree> rocks;
    public int treeCounter;
    public int rockCounter;
    public bool generatingTrees;
    public bool generatingRocks;
    public bool grassGenerated;

    public ChunkData(Chunk chunk, int levelOfDetail) {
        this.chunk = chunk;
        this.isNoiseCalculated = false;
        this.isGenerated = false;
        this.levelOfDetail = levelOfDetail;
        this.colliderGenerated = false;
        this.meshCalculated = false;
        this.meshGenerated = false;
        this.meshInfo = null;
        this.meshQueueInfoIndex = -1;
        this.treesCalculated = false;
        this.treesGenerated = false;
        this.trees = new List<Tree>();
        this.rocks = new List<Tree>();
        this.treeCounter = 0;
        this.rockCounter = 0;
        this.generatingTrees = false;
        this.generatingRocks = false;
        this.grassGenerated = false;

    }
}

public struct Chunk {
    public float[,] heightValues;
    public float[,] temperatureValues;
    public float[][,] biomeValues;
    public float[,] treeValues;
    public float[,] rockValues;
    public int terrainLength;
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public int seed;
    public int xChunk;
    public int yChunk;
    private float maxHeightValue;
    private float minHeightValue;
    public int spaceBetweenVertices;
    public float xPos;
    public float yPos;
    public float proximityX;
    public float proximityY;
    public int numberOfBiomes;
    public BiomeController biomeController;

    public GameObject meshObject;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public Chunk(int terrainLength, float scale, int octaves, float persistance, float lacunarity, int seed, int xChunk, int yChunk, float maxHeightValue, float minHeightValue, int spaceBetweenVertices, float xPos, float yPos, int numberOfBiomes, BiomeController biomeController) {
        this.heightValues = new float[terrainLength + 2, terrainLength + 2];
        this.temperatureValues = new float[terrainLength + 2, terrainLength + 2];
        this.biomeValues = new float[numberOfBiomes][,];
        for(int i = 0; i < numberOfBiomes; i++) {
            this.biomeValues[i] = new float[terrainLength + 2, terrainLength + 2];
        }
        this.treeValues = new float[terrainLength + 2, terrainLength + 2];
        this.rockValues = new float[terrainLength + 2, terrainLength + 2];
        this.terrainLength = terrainLength + 2;
        this.scale = scale;
        this.octaves = octaves;
        this.persistance = persistance;
        this.lacunarity = lacunarity;
        this.seed = seed;
        this.xChunk = xChunk;
        this.yChunk = yChunk;
        this.meshObject = null;
        this.meshFilter = null;
        this.meshRenderer = null;
        this.meshCollider = null;
        this.maxHeightValue = maxHeightValue;
        this.minHeightValue = minHeightValue;
        this.spaceBetweenVertices = spaceBetweenVertices;
        this.xPos = xPos;
        this.yPos = yPos;
        this.proximityX = 0;
        this.proximityY = 0;
        this.numberOfBiomes = numberOfBiomes;
        this.biomeController = biomeController;
    }

    public void GenerateNoise(object callback) {

        ChunkData chunkData = (ChunkData)callback;

        if (scale <= 0) {
            scale = 0.001f;
        }

        for (int y = 0; y < terrainLength; y++) {
            for (int x = 0; x < terrainLength; x++) {
                float heightValue = 0f;
                for (int octave = 0; octave < octaves; octave++) {
                    float amplitude = Mathf.Pow(persistance, octave);
                    float frequency = Mathf.Pow(lacunarity, octave);

                    float xCoord = ((float)x - ((float)(terrainLength - 2) / 2) + (xChunk * (terrainLength - 3))) / scale * frequency + seed;
                    float yCoord = ((float)y - ((float)(terrainLength - 2) / 2) - (yChunk * (terrainLength - 3))) / scale * frequency + seed;

                    float sample = (Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1) * amplitude;

                    heightValue += sample;
                }

                if (heightValue > maxHeightValue) {
                    heightValue = maxHeightValue;
                }
                if (heightValue < minHeightValue) {
                    heightValue = minHeightValue;
                }                

                heightValues[x, y] = heightValue;
            }
        }

        float[] voronoiCellsValues = new float[9];
        Vector2[] voronoiCellsPositions = new Vector2[9];

        float biomeLength = 1;      // how many chunks to group that will give the same seed -> how big the biome is

        int generatorDistance = 12;

        float totalRandomness = 0;
        for(int i = 0; i < numberOfBiomes; i++) {
            totalRandomness += biomeController.biomes[i].randomness;
        }

        float[] biomeRandomness = new float[numberOfBiomes];
        for (int i = 0; i < numberOfBiomes; i++) {
            biomeRandomness[i] = biomeController.biomes[i].randomness / totalRandomness;
        }

        for (int y = yChunk - generatorDistance, i = 0; y < yChunk + generatorDistance + 1; y++) {
            if (y % generatorDistance != 0) continue;
            for(int x = xChunk - generatorDistance; x < xChunk + generatorDistance + 1; x++) {
                if (x % generatorDistance != 0) continue;
                Vector2 coord = new Vector2(x + seed, y + seed);

                var rng = new System.Random(coord.GetHashCode());
                float s = rng.Next(0, 1000) * 0.001f;
                float xPosition = s * (terrainLength - 2);

                s = rng.Next(0, 1000) * 0.001f;
                float yPosition = s * (terrainLength - 2);

                voronoiCellsPositions[i] = new Vector2(xPosition + (x * (terrainLength - 3)), yPosition - (y * (terrainLength - 3)));

                if(x == 0 && y == 0) {
                    voronoiCellsValues[i] = 0;
                    i++;
                    continue;
                }

                coord = new Vector2(Mathf.FloorToInt(x / (biomeLength * generatorDistance)) + seed, Mathf.FloorToInt(y / (biomeLength * generatorDistance)) + seed);

                rng = new System.Random(coord.GetHashCode());

                float randomBiomeValue = rng.Next(0, 1000) * 0.001f;
                for(int j = 0; j < numberOfBiomes; j++) {
                    if(randomBiomeValue <= biomeRandomness[j]) {
                        voronoiCellsValues[i] = j;
                    }
                }

                i++;
            }
        }

        for (int y = 0; y < terrainLength; y++) {
            for (int x = 0; x < terrainLength; x++) {
                heightValues[x, y] = Mathf.InverseLerp(minHeightValue, maxHeightValue, heightValues[x, y]);

                float[] distanceBiome = new float[numberOfBiomes];
                for(int i = 0; i < numberOfBiomes; i++) {
                    distanceBiome[i] = 10000;
                }

                Vector2 position = new Vector2(x + (xChunk * (terrainLength - 3)), y - (yChunk * (terrainLength - 3)));

                for (int i = 0; i < voronoiCellsPositions.Length; i++) {
                    float distance = Vector2.Distance(voronoiCellsPositions[i], position);

                    for (int j = 0; j < numberOfBiomes; j++) {
                        if (voronoiCellsValues[i] == j && distanceBiome[j] > distance) {
                            distanceBiome[j] = distance;
                        }
                    }
                }

                float distanceBiome1 = 100000;
                int indexBiome1 = -1;
                float distanceBiome2 = 100000;
                int indexBiome2 = -1;
                float distanceBiome3 = 100000;
                int indexBiome3 = -1;

                float distanceBiomeHeight1 = 100000;
                float distanceBiomeHeight2 = 100000;
                float distanceBiomeHeight3 = 100000;

                float maxDistance = 0;
                float maxDistanceHeight = 0;

                for (int i = 0; i < numberOfBiomes; i++) {
                    if(distanceBiome[i] < distanceBiome1) {
                        distanceBiome3 = distanceBiome2;
                        indexBiome3 = indexBiome2;
                        distanceBiome2 = distanceBiome1;
                        indexBiome2 = indexBiome1;
                        distanceBiome1 = distanceBiome[i];
                        indexBiome1 = i;
                    } else {
                        if (distanceBiome[i] < distanceBiome2) {
                            distanceBiome3 = distanceBiome2;
                            indexBiome3 = indexBiome2;
                            distanceBiome2 = distanceBiome[i];
                            indexBiome2 = i;
                        } else {
                            if (distanceBiome[i] < distanceBiome3) {
                                distanceBiome3 = distanceBiome[i];
                                indexBiome3 = i;
                            }
                        }
                    }
                    
                }

                if (numberOfBiomes == 1)
                    maxDistance = distanceBiome1;
                else if (numberOfBiomes == 2)
                    maxDistance = distanceBiome1 + distanceBiome2;
                else if (numberOfBiomes > 2)
                    maxDistance = distanceBiome1 + distanceBiome2 + distanceBiome3;

                distanceBiomeHeight1 = Mathf.Pow(distanceBiome1 / maxDistance, 10);
                distanceBiomeHeight2 = Mathf.Pow(distanceBiome2 / maxDistance, 10);
                distanceBiomeHeight3 = Mathf.Pow(distanceBiome3 / maxDistance, 10);

                distanceBiome1 = Mathf.Pow(distanceBiome1 / maxDistance, 20);
                distanceBiome2 = Mathf.Pow(distanceBiome2 / maxDistance, 20);
                distanceBiome3 = Mathf.Pow(distanceBiome3 / maxDistance, 20);

                distanceBiome1 = 1 / distanceBiome1;
                distanceBiomeHeight1 = 1 / distanceBiomeHeight1;
                if (distanceBiome1 > float.MaxValue) {
                    distanceBiome1 = float.MaxValue;
                } else if (distanceBiome1 < float.MinValue) { 
                    distanceBiome1 = float.MinValue; 
                }

                if (distanceBiomeHeight1 > float.MaxValue) {
                    distanceBiomeHeight1 = float.MaxValue;
                } else if (distanceBiomeHeight1 < float.MinValue) {
                    distanceBiomeHeight1 = float.MinValue;
                }

                distanceBiome2 = 1 / distanceBiome2;
                distanceBiomeHeight2 = 1 / distanceBiomeHeight2;
                if (distanceBiome2 > float.MaxValue) {
                    distanceBiome2 = float.MaxValue;
                } else if (distanceBiome2 < float.MinValue) {
                    distanceBiome2 = float.MinValue;
                }

                if (distanceBiomeHeight2 > float.MaxValue) {
                    distanceBiomeHeight2 = float.MaxValue;
                } else if (distanceBiomeHeight2 < float.MinValue) {
                    distanceBiomeHeight2 = float.MinValue;
                }

                distanceBiome3 = 1 / distanceBiome3;
                distanceBiomeHeight3 = 1 / distanceBiomeHeight3;
                if (distanceBiome3 > float.MaxValue) {
                    distanceBiome3 = float.MaxValue;
                } else if (distanceBiome3 < float.MinValue) {
                    distanceBiome3 = float.MinValue;
                }

                if (distanceBiomeHeight3 > float.MaxValue) {
                    distanceBiomeHeight3 = float.MaxValue;
                } else if (distanceBiomeHeight3 < float.MinValue) {
                    distanceBiomeHeight3 = float.MinValue;
                }

                if (numberOfBiomes == 1) {
                    maxDistance = distanceBiome1;
                    maxDistanceHeight = distanceBiomeHeight1;
                } else if (numberOfBiomes == 2) {
                    maxDistance = distanceBiome1 + distanceBiome2;
                    maxDistanceHeight = distanceBiomeHeight1 + distanceBiomeHeight2;
                } else if (numberOfBiomes > 2) {
                    maxDistance = distanceBiome1 + distanceBiome2 + distanceBiome3;
                    maxDistanceHeight = distanceBiomeHeight1 + distanceBiomeHeight2 + distanceBiomeHeight3;
                }

                distanceBiome1 = distanceBiome1 / maxDistance;
                distanceBiome2 = distanceBiome2 / maxDistance;
                distanceBiome3 = distanceBiome3 / maxDistance;

                distanceBiomeHeight1 = distanceBiomeHeight1 / maxDistanceHeight;
                distanceBiomeHeight2 = distanceBiomeHeight2 / maxDistanceHeight;
                distanceBiomeHeight3 = distanceBiomeHeight3 / maxDistanceHeight;

                temperatureValues[x, y] = indexBiome1;

                for (int i = 0; i < numberOfBiomes; i++) {
                    biomeValues[i][x, y] = 0;
                }

                biomeValues[indexBiome1][x, y] = distanceBiome1;
                if (numberOfBiomes > 1)
                    biomeValues[indexBiome2][x, y] = distanceBiome2;
                if (numberOfBiomes > 2)
                    biomeValues[indexBiome3][x, y] = distanceBiome3;

                float xCoord = ((float)x - ((float)(terrainLength - 2) / 2) + (xChunk * (terrainLength - 3))) / (scale * 4) + seed;
                float yCoord = ((float)y - ((float)(terrainLength - 2) / 2) - (yChunk * (terrainLength - 3))) / (scale * 4) + seed;

                float multiplier = Mathf.PerlinNoise(xCoord, yCoord);

                float multiplier2 = 0;
                multiplier2 = biomeController.biomes[indexBiome1].maxHeight * distanceBiomeHeight1;
                if (numberOfBiomes > 1)
                    multiplier2 += biomeController.biomes[indexBiome2].maxHeight * distanceBiomeHeight2;
                if (numberOfBiomes > 2)
                    multiplier2 += biomeController.biomes[indexBiome3].maxHeight * distanceBiomeHeight3;

                heightValues[x, y] *= Mathf.Pow(2, multiplier2);

                float xTreeCoord = ((float)x - ((float)(terrainLength - 2) / 2) + (xChunk * (terrainLength - 3))) / (scale * 0.4f) + seed + 100;
                float yTreeCoord = ((float)y - ((float)(terrainLength - 2) / 2) - (yChunk * (terrainLength - 3))) / (scale * 0.4f) + seed + 100;

                if (x > 0 && y > 0 && x < terrainLength - 1 && y < terrainLength - 1) {
                    if (x % 6 == 0 && y % 6 == 0) {
                        float perlinValue = Mathf.PerlinNoise(xTreeCoord, yTreeCoord);
                        treeValues[x, y] = heightValues[x, y];
                    } else {
                        treeValues[x, y] = 0;
                    }
                } else {
                    treeValues[x, y] = 0;
                }

                float xRockCoord = ((float)x - ((float)(terrainLength - 2) / 2) + (xChunk * (terrainLength - 3))) / (scale * 0.02f) + seed;
                float yRockCoord = ((float)y - ((float)(terrainLength - 2) / 2) - (yChunk * (terrainLength - 3))) / (scale * 0.02f) + seed;

                if (x > 0 && y > 0 && x < terrainLength - 1 && y < terrainLength - 1) {
                    if (x % 3 == 0 && y % 3 == 0) {
                        float perlinValue = Mathf.PerlinNoise(xRockCoord, yRockCoord);
                        if (perlinValue > 0.8f) {
                            rockValues[x, y] = heightValues[x, y];
                        } else {
                            rockValues[x, y] = 0;
                        }
                    } else {
                        rockValues[x, y] = 0;
                    }
                } else {
                    rockValues[x, y] = 0;
                }
            }
        }

        chunkData.isNoiseCalculated = true;
    }
}
