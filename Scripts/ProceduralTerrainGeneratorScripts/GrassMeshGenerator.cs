using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public struct GrassMeshGenerator {
    private int quantity;
    private int grassTypes;
    private int chunkDivision; // each terrain chunk is divided into x squares
    private ChunkData chunkData;
    static int seed = Environment.TickCount;
    float[] probabilities;
    public BiomeController biomeController;

    static readonly ThreadLocal<System.Random> random =
        new ThreadLocal<System.Random>(() => new System.Random(Interlocked.Increment(ref seed)));

    public GrassMeshGenerator(int quantity, int chunkDivision, int grassTypes, ChunkData chunkData, BiomeController biomeController) {
        this.quantity = quantity;
        this.grassTypes = grassTypes;
        this.chunkDivision = chunkDivision;
        this.chunkData = chunkData;
        this.probabilities = new float[3] { 0.3f, 0.7f, 0.9f };
        this.biomeController = biomeController;
    }

    public static float Rand() {
        return random.Value.Next(0, 1000) * 0.001f;
    }

    public void CalculateGrass(object callback) {
        Dictionary<Vector2, GrassData> grassData = (Dictionary<Vector2, GrassData>)callback;

        int sqrtChunkDivision = (int)Mathf.Sqrt(chunkDivision);     // the square root of the number of chunk divisions
        Vector2 keyPosition = new Vector2(chunkData.chunk.xChunk, chunkData.chunk.yChunk);
        Vector2 grassKeyPosition = new Vector2(keyPosition.x * sqrtChunkDivision, keyPosition.y * sqrtChunkDivision);

        System.Random random = new System.Random();

        if (!grassData.ContainsKey(grassKeyPosition)) {
            MeshInfo terrainMesh = chunkData.meshInfo;
            
            int trianglePerRow = terrainMesh.triangles.Length / 4;    // the number of triangles per chunk division
            int triangleCounter = 0;
            int verticesPerSide = (int)Mathf.Sqrt(terrainMesh.vertices.Length) / 4 * 2;     // the number of vertices in the side of a chunk division

            int grassLength = (terrainMesh.triangles.Length / chunkDivision) / grassTypes * quantity;

            Vector3[][][] terrainVertices = new Vector3[chunkDivision][][];
            int[][][] terrainIndices = new int[chunkDivision][][];
            Vector3[][][] terrainNormals = new Vector3[chunkDivision][][];
            float[][][] temperatureValue = new float[chunkDivision][][];
            int[][] counters = new int[chunkDivision][];

            float[][] randomness = new float[biomeController.biomes.Length][];
            for(int i = 0; i < biomeController.biomes.Length; i++) {
                randomness[i] = new float[biomeController.biomes[i].grassModels.Length];
                for(int j = 0; j < biomeController.biomes[i].grassModels.Length; j++) {
                    randomness[i][j] = biomeController.biomes[i].grassModels[j].randomness;
                }
            }

            for (int i = 0; i < chunkDivision; i++) {
                terrainVertices[i] = new Vector3[grassTypes][];
                terrainIndices[i] = new int[grassTypes][];
                terrainNormals[i] = new Vector3[grassTypes][];
                temperatureValue[i] = new float[grassTypes][];
                counters[i] = new int[grassTypes];

                for(int j = 0; j < grassTypes; j++) {
                    terrainVertices[i][j] = new Vector3[grassLength];
                    terrainIndices[i][j] = new int[grassLength];
                    terrainNormals[i][j] = new Vector3[grassLength];
                    temperatureValue[i][j] = new float[grassLength];
                    counters[i][j] = 0;
                }
            }

            int verticesPerSideOfRow = verticesPerSide * sqrtChunkDivision;

            for (int i = 0; i < terrainMesh.triangles.Length - 3; i += 3) {
                if (triangleCounter == verticesPerSideOfRow) {
                    triangleCounter = 0;
                }

                int triangleA = terrainMesh.triangles[i];
                int triangleB = terrainMesh.triangles[i + 1];
                int triangleC = terrainMesh.triangles[i + 2];

                int yPos = Mathf.FloorToInt(triangleA / (chunkData.chunk.terrainLength - 2));
                int xPos = triangleA - (yPos * (chunkData.chunk.terrainLength - 2));

                float biomeValue = chunkData.chunk.temperatureValues[xPos, yPos];

                for (int j = 0; j < grassTypes; j++) {
                    float randomnessValue = randomness[(int)biomeValue][j];
                    for (int k = 0; k < quantity; k++) {
                        float xCoord = ((float)xPos - ((float)(chunkData.chunk.terrainLength - 2) / 2) + (chunkData.chunk.xChunk * (chunkData.chunk.terrainLength - 3))) / (chunkData.chunk.scale * 0.01f) + (2021 * j);
                        float yCoord = ((float)yPos - ((float)(chunkData.chunk.terrainLength - 2) / 2) - (chunkData.chunk.yChunk * (chunkData.chunk.terrainLength - 3))) / (chunkData.chunk.scale * 0.01f) + (2021 * j);

                        float multiplier = Mathf.PerlinNoise(xCoord, yCoord);

                        Vector3 randomC = Vector3.Lerp(terrainMesh.vertices[triangleA], terrainMesh.vertices[triangleB], Rand());
                        Vector3 randomMiddle = Vector3.Lerp(randomC, terrainMesh.vertices[triangleC], Rand());
                        int chunk;

                        if (i < trianglePerRow) {
                            if (triangleCounter < verticesPerSide) {
                                chunk = 0;
                            } else if (triangleCounter >= verticesPerSide && triangleCounter < verticesPerSide * 2) {
                                chunk = 1;
                            } else if (triangleCounter >= verticesPerSide * 2 && triangleCounter < verticesPerSide * 3) {
                                chunk = 2;
                            } else {
                                chunk = 3;
                            }
                        } else if (i >= trianglePerRow && i < trianglePerRow * 2) {
                            if (triangleCounter < verticesPerSide) {
                                chunk = 4;
                            } else if (triangleCounter >= verticesPerSide && triangleCounter < verticesPerSide * 2) {
                                chunk = 5;
                            } else if (triangleCounter >= verticesPerSide * 2 && triangleCounter < verticesPerSide * 3) {
                                chunk = 6;
                            } else {
                                chunk = 7;
                            }
                        } else if (i >= trianglePerRow * 2 && i < trianglePerRow * 3) {
                            if (triangleCounter < verticesPerSide) {
                                chunk = 8;
                            } else if (triangleCounter >= verticesPerSide && triangleCounter < verticesPerSide * 2) {
                                chunk = 9;
                            } else if (triangleCounter >= verticesPerSide * 2 && triangleCounter < verticesPerSide * 3) {
                                chunk = 10;
                            } else {
                                chunk = 11;
                            }
                        } else {
                            if (triangleCounter < verticesPerSide) {
                                chunk = 12;
                            } else if (triangleCounter >= verticesPerSide && triangleCounter < verticesPerSide * 2) {
                                chunk = 13;
                            } else if (triangleCounter >= verticesPerSide * 2 && triangleCounter < verticesPerSide * 3) {
                                chunk = 14;
                            } else {
                                chunk = 15;
                            }
                        }

                        terrainVertices[chunk][j][counters[chunk][j]] = new Vector3(randomMiddle.x + chunkData.chunk.xPos, randomMiddle.y, randomMiddle.z + chunkData.chunk.yPos);
                        terrainIndices[chunk][j][counters[chunk][j]] = counters[chunk][j];

                        Vector3 side1 = terrainMesh.vertices[triangleA] - terrainMesh.vertices[triangleB];
                        Vector3 side2 = terrainMesh.vertices[triangleB] - terrainMesh.vertices[triangleC];
                        terrainNormals[chunk][j][counters[chunk][j]] = Vector3.Cross(side1, side2).normalized;
                        if (biomeController.biomes[(int)biomeValue].grassModels.Length > 0 && multiplier <= randomnessValue) {
                            temperatureValue[chunk][j][counters[chunk][j]] = biomeValue;
                        } else {
                            temperatureValue[chunk][j][counters[chunk][j]] = -1;
                        }

                        counters[chunk][j]++;
                    }
                }
                triangleCounter++;
            }

            int counter = 0;
            for (int y = sqrtChunkDivision - 1; y >= 0; y--) {
                for (int x = 0; x < sqrtChunkDivision; x++, counter++) {
                    GrassData grass = new GrassData(terrainVertices[counter], terrainIndices[counter], terrainNormals[counter], temperatureValue[counter], chunkData.chunk.xPos, chunkData.chunk.yPos, grassTypes, chunkData.chunk.meshObject);
                    grassData.Add(new Vector2(grassKeyPosition.x + x, grassKeyPosition.y + y), grass);
                }
            }
        }
    }
}

public class GrassData {
    public Vector3[][] positions;
    public int[][] indices;
    public Vector3[][] normals;
    public float[][] temperatures;
    public float xPos;
    public float yPos;

    public GameObject[] meshObject;
    public MeshFilter[] meshFilter;
    public MeshRenderer[] meshRenderer;

    public GameObject parentGameObject;

    public GrassData(Vector3[][] positions, int[][] indices, Vector3[][] normals, float[][] temperatures, float xPos, float yPos, int grassTypes, GameObject parentGameObject) {
        this.positions = positions;
        this.indices = indices;
        this.normals = normals;
        this.temperatures = temperatures;
        this.xPos = xPos;
        this.yPos = yPos;
        this.meshObject = new GameObject[grassTypes];
        this.meshFilter = new MeshFilter[grassTypes];
        this.meshRenderer = new MeshRenderer[grassTypes];
        this.parentGameObject = parentGameObject;
    }
}
