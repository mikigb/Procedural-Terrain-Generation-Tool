using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshGenerator {
    public int terrainLength;
    public float[,] noiseValues;
    public float terrainHeight;
    public int maxTerrainLength;
    public int spaceBetweenVertices;
    public int terrainMultiplier;
    public int xBorder;
    public int yBorder;

    public MeshGenerator(int terrainLength, float[,] noiseValues, float terrainHeight, int maxTerrainLength, int spaceBetweenVertices, int terrainMultiplier, int xBorder, int yBorder) {
        this.terrainLength = terrainLength;
        this.noiseValues = noiseValues;
        this.terrainHeight = terrainHeight;
        this.maxTerrainLength = maxTerrainLength;
        this.spaceBetweenVertices = spaceBetweenVertices;
        this.terrainMultiplier = terrainMultiplier;
        this.xBorder = xBorder;
        this.yBorder = yBorder;
    }

    public void GenerateMesh(object callback) {
        ChunkData chunkData = (ChunkData)callback;
        MeshInfo meshInfo = new MeshInfo(terrainLength);

        float mapCenterX = ((maxTerrainLength - 1) / -2f) * terrainMultiplier;
        float mapCenterY = ((maxTerrainLength - 1) / 2f) * terrainMultiplier;

        int edgeVertexCounter = -1;
        int meshVertexCounter = 0;
        int[,] vertexPositions = new int[terrainLength + 2, terrainLength + 2];

        for (int y = 0; y < terrainLength + 2; y++) {
            for (int x = 0; x < terrainLength + 2; x++) {
                if (y == 0 || x == 0 || y == terrainLength + 1 || x == terrainLength + 1) {
                    vertexPositions[x, y] = edgeVertexCounter;
                    edgeVertexCounter--;
                } else {
                    vertexPositions[x, y] = meshVertexCounter;
                    meshVertexCounter++;
                }
            }
        }
        for (int y = 0; y < terrainLength + 2; y++) {
            for (int x = 0; x < terrainLength + 2; x++) {
                float noiseValue = 0;
                bool noiseBorder = false;
                int yCoord = y * spaceBetweenVertices - (spaceBetweenVertices - 1);
                int xCoord = x * spaceBetweenVertices - (spaceBetweenVertices - 1);
                if (y == 0 || x == 0 || y == terrainLength + 1 || x == terrainLength + 1) {
                    if (y == 0) {
                        yCoord = 0;
                    } else if (y == terrainLength + 1) {
                        yCoord = y * spaceBetweenVertices - ((spaceBetweenVertices - 1) * 2);
                    }
                    if (x == 0) {
                        xCoord = 0;
                    } else if (x == terrainLength + 1) {
                        xCoord = x * spaceBetweenVertices - ((spaceBetweenVertices - 1) * 2);
                    }
                } else {
                    /*if (y == 1 || x == 1 || y == terrainLength || x == terrainLength) {
                        if ((y == 1 && yBorder == 1) || (y == terrainLength && yBorder == -1)) {
                            if (x != 0 && x != terrainLength + 1) {
                                if (x % 2 == 0) {
                                    float xNoiseNeg = noiseValues[xCoord - spaceBetweenVertices, yCoord];
                                    float xNoisePos = noiseValues[xCoord + spaceBetweenVertices, yCoord];

                                    noiseValue = (xNoiseNeg + xNoisePos) / 2;
                                    noiseBorder = true;
                                }
                            }
                        } 
                        if ((x == 1 && xBorder == -1) || (x == terrainLength && xBorder == 1)) {
                            if (y != 0 && y != terrainLength + 1) {
                                if (y % 2 == 0) {
                                    float yNoiseNeg = noiseValues[xCoord, yCoord - spaceBetweenVertices];
                                    float yNoisePos = noiseValues[xCoord, yCoord + spaceBetweenVertices];

                                    noiseValue = (yNoiseNeg + yNoisePos) / 2;
                                    noiseBorder = true;
                                }
                            }
                        } 
                    }*/
                }

                if(noiseBorder == false)
                    noiseValue = noiseValues[xCoord, yCoord];


                Vector3 vertex = new Vector3(mapCenterX + (xCoord * terrainMultiplier), noiseValue * terrainHeight, mapCenterY - (yCoord * terrainMultiplier));
                Vector2 uv = new Vector2((x - spaceBetweenVertices) / (float)terrainLength, (y - spaceBetweenVertices) / (float)terrainLength);
                meshInfo.AddVertex(vertex, uv, vertexPositions[x, y]);

                if (x < terrainLength + 1 && y < terrainLength + 1) {
                    int vertex1 = vertexPositions[x, y];
                    int vertex2 = vertexPositions[x + 1, y];
                    int vertex3 = vertexPositions[x + 1, y + 1];
                    int vertex4 = vertexPositions[x, y + 1];

                    meshInfo.AddTriangle(vertex1, vertex2, vertex3);
                    meshInfo.AddTriangle(vertex1, vertex3, vertex4);
                }
            }
        }

        meshInfo.UpdateNormals();

        chunkData.meshInfo = meshInfo;
        chunkData.meshCalculated = true;
    }
}

public class MeshInfo {
    public Vector3[] vertices;
    public int[] indices;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleCount = 0;

    public Vector3[] edgeVertices;
    public int[] edgeTriangles;
    private int edgeTriangleCount = 0;

    public Vector3[] normals;

    public MeshInfo(int terrainLength) {
        vertices = new Vector3[terrainLength * terrainLength];
        triangles = new int[(terrainLength - 1) * (terrainLength - 1) * 6];
        indices = new int[terrainLength * terrainLength];
        uvs = new Vector2[terrainLength * terrainLength];
        edgeVertices = new Vector3[terrainLength * 4 + 4];
        edgeTriangles = new int[terrainLength * 24];
    }

    public void AddVertex(Vector3 vertex, Vector2 uv, int vertexPosition) {
        if(vertexPosition < 0) {
            edgeVertices[-vertexPosition - 1] = vertex;
        } else {
            vertices[vertexPosition] = vertex;
            indices[vertexPosition] = vertexPosition;
            uvs[vertexPosition] = uv;
        }
    }

    public void AddTriangle(int v1, int v2, int v3) {
        if(v1 < 0 || v2 < 0 || v3 < 0) {
            edgeTriangles[edgeTriangleCount] = v1;
            edgeTriangles[edgeTriangleCount + 1] = v2;
            edgeTriangles[edgeTriangleCount + 2] = v3;
            edgeTriangleCount += 3;
        } else {
            triangles[triangleCount] = v1;
            triangles[triangleCount + 1] = v2;
            triangles[triangleCount + 2] = v3;
            triangleCount += 3;
        }
    }

    public Vector3 CalculateTriangleNormal(int vertexPosition1, int vertexPosition2, int vertexPosition3) {
        Vector3 vertex1;
        Vector3 vertex2;
        Vector3 vertex3;

        if (vertexPosition1 < 0)
            vertex1 = edgeVertices[-vertexPosition1 - 1]; 
        else
            vertex1 = vertices[vertexPosition1];

        if (vertexPosition2 < 0)
            vertex2 = edgeVertices[-vertexPosition2 - 1];
        else
            vertex2 = vertices[vertexPosition2];

        if (vertexPosition3 < 0)
            vertex3 = edgeVertices[-vertexPosition3 - 1];
        else
            vertex3 = vertices[vertexPosition3];

        Vector3 side1 = vertex2 - vertex1;
        Vector3 side2 = vertex3 - vertex1;

        return Vector3.Cross(side1, side2).normalized;
    }

    public void UpdateNormals() {
        Vector3[] normals = new Vector3[vertices.Length];

        for(int i = 0; i < triangles.Length / 3; i++) {
            int trianglePosition = i * 3;
            int vertexPosition1 = triangles[trianglePosition];
            int vertexPosition2 = triangles[trianglePosition + 1];
            int vertexPosition3 = triangles[trianglePosition + 2];

            Vector3 triangleNormal = CalculateTriangleNormal(vertexPosition1, vertexPosition2, vertexPosition3);
            normals[vertexPosition1] += triangleNormal;
            normals[vertexPosition2] += triangleNormal;
            normals[vertexPosition3] += triangleNormal;
        }

        for (int i = 0; i < edgeTriangles.Length / 3; i++) {
            int trianglePosition = i * 3;
            int vertexPosition1 = edgeTriangles[trianglePosition];
            int vertexPosition2 = edgeTriangles[trianglePosition + 1];
            int vertexPosition3 = edgeTriangles[trianglePosition + 2];

            Vector3 triangleNormal = CalculateTriangleNormal(vertexPosition1, vertexPosition2, vertexPosition3);
            if(vertexPosition1 >= 0) 
                normals[vertexPosition1] += triangleNormal;
            if(vertexPosition2 >= 0)
                normals[vertexPosition2] += triangleNormal;
            if(vertexPosition3 >= 0)
                normals[vertexPosition3] += triangleNormal;
        }

        for (int i = 0; i < normals.Length; i++) {
            normals[i].Normalize();
        }

        this.normals = normals;
    }
}

