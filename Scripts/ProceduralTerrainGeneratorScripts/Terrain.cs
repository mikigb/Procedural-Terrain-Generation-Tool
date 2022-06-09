using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;

public class Terrain : MonoBehaviour {
    // chunk values
    [Range(1, 7)]
    private int terrainMaxLOD = 7;       // since the lod change, now this only affects the eq: 2^maxLOD + 1 == terrain length. MAX of 7 because of the mesh MAX number of vertices of 65535
    [Range(1, 7)]
    private int terrainMinLOD = 7;
    private int terrainLength = 129;
    private int maxTerrainLength = 129;
    private int spaceBetweenVertices;
    private float scale = 300;
    private int octaves = 8;
    private float persistance = 0.45f;
    private float lacunarity = 2;
    public int seed;
    private int terrainMultiplier = 1;
    private int renderDistance = 1500;
    private float maxHeightValue = 1.2f;
    private float minHeightValue = -1.5f;

    // terrain values
    public int chunkDistance = 7;
    public Dictionary<Vector2, ChunkData> chunkList;
    private List<Vector2> chunksInSight;
    private Material material;
    private Material material2;
    private float terrainHeight = 150;
    public bool randomizeTerrain;

    // render detail
    private bool fog = false;
    private int clipping = 1350;

    // other
    public Transform playerTransform;
    private Vector2 lastPlayerPosition;
    private Vector2 lastPlayerChunk = new Vector2(10,10);
    public Transform water;

    // grass
    [HideInInspector]
    public List<GameObject> grass;
    private Vector2 lastPlayerGrassPosition;
    public Dictionary<Vector2, GrassData> grassData;
    [HideInInspector]
    public bool canGenerateGrass = true;
    [HideInInspector]
    public Mesh grassMesh;
    private Material grassMaterial;
    [HideInInspector]
    public Material[] grassMaterialList;
    private List<Vector2> grassChunksInSight;
    private float grassMaxDistance = 800;
    private int grassChunkDivision = 16;
    private int grassQuantity = 1;


    // queue objects
    private Queue<Tree> gameObjectsQueue;
    private int minIntancesPerFrame = 10;

    private Queue<TerrainInfo> terrainInfoQueue;
    public MeshQueueInfo[] terrainMeshes;

    // biomes
    public BiomeController biomeController;


    private bool startUpdateProcess = false;
    private void Awake() {
        if(randomizeTerrain) {
            seed = UnityEngine.Random.Range(0, 10000);
            Debug.Log("Seed: " + seed);
        }
        spaceBetweenVertices = terrainLength / (int)Mathf.Pow(2, terrainMaxLOD);
        RenderOptions.UpdateTenderOptions(fog, clipping);
        grassData = new Dictionary<Vector2, GrassData>();
        chunkList = new Dictionary<Vector2, ChunkData>();

        //define materials
        material = Resources.Load("Materials/MeshMaterial") as Material;
        material2 = TextureGenerator.CreateTexturesForTerrainShader(material, maxHeightValue, terrainHeight, biomeController);

        int maxNumberOfGrassModels = 0;
        for(int i = 0; i < biomeController.biomes.Length; i++) {
            int grassModels = biomeController.biomes[i].grassModels.Length;
            if (maxNumberOfGrassModels < grassModels) {
                maxNumberOfGrassModels = grassModels;
            }
        }

        grassMaterial = Resources.Load("Materials/GrassMaterial") as Material;
        grassMaterialList = new Material[maxNumberOfGrassModels];
        for (int i = 0; i < maxNumberOfGrassModels; i++) {
            grassMaterialList[i] = new Material(grassMaterial);
            grassMaterialList[i].SetFloat("_MaxViewDistance", grassMaxDistance);
            grassMaterialList[i].SetFloat("_MaxHeight", terrainHeight * maxHeightValue);
            grassMaterialList[i].SetFloat("_MinHeight", 0);
        }

        grassMaterialList = TextureGenerator.CreateTexturesForGrassShaders(grassMaterialList, biomeController, maxNumberOfGrassModels);
        grass = new List<GameObject>();

        gameObjectsQueue = new Queue<Tree>();
        terrainInfoQueue = new Queue<TerrainInfo>();
        terrainMeshes = new MeshQueueInfo[(int)Mathf.Pow((chunkDistance + 2) * 2, 2)];
        for(int i = 0; i < terrainMeshes.Length; i++) {
            terrainMeshes[i] = new MeshQueueInfo();
            terrainMeshes[i].mesh = new Mesh();
        }

        CalculateVisibleChunksInSquare();
        PrintChunks();
        lastPlayerPosition = new Vector2(playerTransform.position.x, playerTransform.position.z);
    }

    private void FixedUpdate() {
        if (startUpdateProcess == false) {
            playerTransform.transform.position = new Vector3(0, 1000, 0);
            Vector3 origin = new Vector3(10, 300, 10);

            RaycastHit hit;

            if (Physics.Raycast(origin, Vector3.down, out hit, 300)) {
                startUpdateProcess = true;
                playerTransform.transform.position = new Vector3(hit.point.x, hit.point.y + 10, hit.point.z);
            }
        } 
    }

    private void Update() {

        if (lastPlayerPosition.x != playerTransform.position.x && lastPlayerPosition.y != playerTransform.position.z) {
            CalculateVisibleChunksInSquare();
            lastPlayerPosition = new Vector2(playerTransform.position.x, playerTransform.position.z);
            water.position = new Vector3(playerTransform.position.x, water.position.y, playerTransform.position.z);
            UpdateGrass();
        }

        PrintChunks();
        AsignMeshFromQueue();

        int objectsInQueue = gameObjectsQueue.Count;
        if (objectsInQueue > 0) {
            if (objectsInQueue < minIntancesPerFrame) {
                for (int i = 0; i < objectsInQueue; i++) {
                    InstanciateObjectInQueue();
                }
            } else {
                for (int i = 0; i < minIntancesPerFrame; i++) {
                    InstanciateObjectInQueue();
                }
            }
        }
    }

    private void CalculateVisibleChunksInSquare() {
        int playerChunkX = Mathf.FloorToInt(playerTransform.position.x / ((float)maxTerrainLength * terrainMultiplier));
        int playerChunkY = Mathf.FloorToInt(playerTransform.position.z / ((float)maxTerrainLength * terrainMultiplier));

        if (lastPlayerChunk.x != playerChunkX || lastPlayerChunk.y != playerChunkY) {
            chunksInSight = new List<Vector2>();

            int lod = terrainMaxLOD;

            for (int yChunk = playerChunkY - chunkDistance; yChunk <= playerChunkY + chunkDistance; yChunk++) {
                for (int xChunk = playerChunkX - chunkDistance; xChunk <= playerChunkX + chunkDistance; xChunk++) {
                    float chunkPositionX = xChunk * maxTerrainLength * terrainMultiplier + ((maxTerrainLength * terrainMultiplier) / 2);
                    float chunkPositionY = yChunk * maxTerrainLength * terrainMultiplier - ((maxTerrainLength * terrainMultiplier) / 2);

                    float xPos = xChunk * maxTerrainLength * terrainMultiplier - xChunk * terrainMultiplier;
                    float yPos = yChunk * maxTerrainLength * terrainMultiplier - yChunk * terrainMultiplier;

                    float chunkDistanceToCenter = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(playerTransform.position.x - chunkPositionX), 2) + Mathf.Pow(Mathf.Abs(playerTransform.position.z - chunkPositionY), 2));

                    if (chunkDistanceToCenter <= renderDistance) {
                        Vector2 keyPosition = new Vector2(xChunk, yChunk);

                        chunksInSight.Add(keyPosition);

                        if (!chunkList.ContainsKey(keyPosition)) {

                            Chunk chunk = new Chunk(terrainLength, scale, octaves, persistance, lacunarity, seed, xChunk, yChunk, maxHeightValue, minHeightValue, spaceBetweenVertices, xPos, yPos, biomeController.biomes.Length, biomeController);
                            ChunkData chunkData = new ChunkData(chunk, lod);

                            ThreadPool.QueueUserWorkItem(new WaitCallback(chunk.GenerateNoise), chunkData);

                            chunkList.Add(keyPosition, chunkData);
                        }
                    }
                }
            }

            List<Vector2> chunksNotInSight = chunkList.Keys.Except(chunksInSight).ToList();

            foreach (Vector2 keyPosition in chunksNotInSight) {
                if (chunkList.ContainsKey(keyPosition)) {
                    if (chunkList[keyPosition].chunk.meshObject != null) {
                        if (chunkList[keyPosition].chunk.meshObject.activeSelf == true) {
                            if (chunkList[keyPosition].meshQueueInfoIndex != -1) {
                                terrainMeshes[chunkList[keyPosition].meshQueueInfoIndex].isAsigned = false;
                                chunkList[keyPosition].meshQueueInfoIndex = -1;
                                chunkList[keyPosition].chunk.meshObject.SetActive(false);
                                chunkList.Remove(keyPosition);
                            }
                        }
                    }
                }
            }

            lastPlayerChunk = new Vector2(playerChunkX, playerChunkY);
        }

    }

    private void PrintChunks() {
        int playerChunkX = Mathf.FloorToInt(playerTransform.position.x / ((float)maxTerrainLength * terrainMultiplier));
        int playerChunkY = Mathf.FloorToInt(playerTransform.position.z / ((float)maxTerrainLength * terrainMultiplier));

        int lod = terrainMaxLOD;

        for (int yChunk = playerChunkY - chunkDistance; yChunk <= playerChunkY + chunkDistance; yChunk++) {
            int yDifference = playerChunkY - yChunk;

            for (int xChunk = playerChunkX - chunkDistance; xChunk <= playerChunkX + chunkDistance; xChunk++) {
                Vector2 keyPosition = new Vector2(xChunk, yChunk);

                if (chunkList.ContainsKey(keyPosition)) {
                    ChunkData chunkData = chunkList[keyPosition];
                    int xDifference = playerChunkX - xChunk;
                    if (!chunkData.isGenerated) {
                        Chunk chunk = chunkData.chunk;
                        if (chunkData.isNoiseCalculated) {
                            float chunkPositionX = xChunk * maxTerrainLength * terrainMultiplier;
                            float chunkPositionY = yChunk * maxTerrainLength * terrainMultiplier;
                            float chunkDistanceToCenter = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(playerTransform.position.x - chunkPositionX), 2) + Mathf.Pow(Mathf.Abs(playerTransform.position.z - chunkPositionY), 2));

                            lod = CalculateLOD(chunkDistanceToCenter);

                            int spaceBetweenVerticesAux = maxTerrainLength / (int)Mathf.Pow(2, lod);
                            int terrainLengthAux = (int)Mathf.Pow(2, lod) + 1;

                            chunkData.levelOfDetail = lod;

                            chunk.meshObject = new GameObject("Chunk_" + lod);
                            chunk.meshObject.layer = 8;
                            chunk.meshFilter = chunk.meshObject.AddComponent<MeshFilter>();
                            chunk.meshRenderer = chunk.meshObject.AddComponent<MeshRenderer>();
                            chunk.meshCollider = chunk.meshObject.AddComponent<MeshCollider>();

                            chunk.meshRenderer.material = material2;
                                
                            chunk.meshObject.transform.parent = this.transform;

                            chunk.meshObject.transform.position = new Vector3(chunk.xPos, 0, chunk.yPos);

                            chunkData.meshCalculated = false;
                            chunkData.meshGenerated = false;
                            GenerateMesh(chunkData, terrainLengthAux, spaceBetweenVerticesAux, new Vector2(playerChunkX, playerChunkY), lod);

                            chunk.proximityX = xDifference;
                            chunk.proximityY = yDifference;

                            chunkData.isGenerated = true;

                            chunkData.chunk = chunk;
                            chunkList[keyPosition] = chunkData;

                            return;
                        }
                    } else {
                        float chunkPositionX = xChunk * maxTerrainLength * terrainMultiplier;
                        float chunkPositionY = yChunk * maxTerrainLength * terrainMultiplier;
                        float chunkDistanceToCenter = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(playerTransform.position.x - chunkPositionX), 2) + Mathf.Pow(Mathf.Abs(playerTransform.position.z - chunkPositionY), 2));

                        if (!chunkList[keyPosition].chunk.meshObject.activeSelf) {
                            
                            if (chunkDistanceToCenter <= renderDistance) {
                                chunkList[keyPosition].chunk.meshObject.SetActive(true);
                            }
                        }

                        lod = CalculateLOD(chunkDistanceToCenter);

                        /*if (chunkData.levelOfDetail != lod) {
                            int spaceBetweenVerticesAux = maxTerrainLength / (int)Mathf.Pow(2, lod);
                            int terrainLengthAux = (int)Mathf.Pow(2, lod) + 1;

                            chunkData.levelOfDetail = lod;

                            Chunk chunk = chunkData.chunk;

                            chunkData.meshCalculated = false;
                            chunkData.meshGenerated = false;
                            GenerateMesh(chunkData, terrainLengthAux, spaceBetweenVerticesAux, new Vector2(playerChunkX, playerChunkY), lod);

                            chunkData.colliderGenerated = false;

                            chunk.proximityX = xDifference;
                            chunk.proximityY = yDifference;

                            chunkData.chunk = chunk;
                            chunkList[keyPosition] = chunkData;

                            return;
                        } */

                        if(chunkData.meshCalculated == true && chunkData.meshGenerated == false) {
                            int spaceBetweenVerticesAux = maxTerrainLength / (int)Mathf.Pow(2, lod);
                            int terrainLengthAux = (int)Mathf.Pow(2, lod) + 1;
                            AsignMeshAndCollider(chunkData, terrainLengthAux, spaceBetweenVerticesAux);
                            chunkData.meshGenerated = true;
                            chunkList[keyPosition] = chunkData;
                            return;
                        }

                        if (!chunkData.colliderGenerated && chunkData.meshQueueInfoIndex != -1) {

                            if (chunkDistanceToCenter <= 200) {
                                Chunk chunk = chunkData.chunk;

                                chunk.meshCollider.sharedMesh = terrainMeshes[chunkData.meshQueueInfoIndex].mesh;

                                chunkData.colliderGenerated = true;

                                chunkData.chunk = chunk;
                                chunkList[keyPosition] = chunkData;

                                return;
                            } 
                        }

                        if(chunkDistanceToCenter <= 1000) {
                            if (chunkData.meshGenerated == true && chunkData.grassGenerated == false && chunkData.levelOfDetail == 7) {
                                if (chunkData.meshQueueInfoIndex != -1 ) {
                                    GrassMeshGenerator grassGenerator = new GrassMeshGenerator(grassQuantity, grassChunkDivision, grassMaterialList.Length, chunkData, biomeController);
                                    chunkData.grassGenerated = true;
                                    chunkList[keyPosition] = chunkData;

                                    ThreadPool.QueueUserWorkItem(new WaitCallback(grassGenerator.CalculateGrass), grassData);
                                }
                            }
                        }
                        
                        if (chunkDistanceToCenter <= 1000) {
                            if (chunkData.meshGenerated == true && chunkData.treesCalculated == false) {
                                CalculateTrees(chunkData);
                            }
                            if (chunkData.treesCalculated == true) {
                                if(chunkData.generatingTrees == false) {
                                    chunkData.treeCounter = 0;
                                    chunkData.generatingTrees = true;
                                }

                                if(chunkData.treeCounter == chunkData.trees.Count) {
                                    chunkData.treesGenerated = true;
                                    chunkData.treeCounter = 0;
                                } else {
                                    GenerateTrees(chunkData);                                    
                                    chunkData.treeCounter++;
                                    continue;
                                }
                            }
                        } else {
                            if (chunkData.treesCalculated == true) {
                                if (chunkData.generatingTrees == true) {
                                    chunkData.treeCounter = 0;
                                    chunkData.generatingTrees = false;
                                }

                                if (chunkData.treeCounter == chunkData.trees.Count) {
                                    chunkData.treesGenerated = false;
                                    chunkData.treeCounter = 0;
                                } else {
                                    HideTrees(chunkData);
                                    chunkData.treeCounter++;
                                    continue;
                                }
                            }
                        }

                        if (chunkDistanceToCenter <= 200) {
                            if (chunkData.meshGenerated == true && chunkData.rocksCalculated == false) {
                                CalculateRocks(chunkData);
                            }
                            if (chunkData.rocksCalculated == true) {
                                if (chunkData.generatingRocks == false) {
                                    chunkData.rockCounter = 0;
                                    chunkData.generatingRocks = true;
                                }

                                if (chunkData.rockCounter == chunkData.rocks.Count) {
                                    chunkData.rocksGenerated = true;
                                    chunkData.rockCounter = 0;
                                } else {
                                    GenerateRocks(chunkData);
                                    chunkData.rockCounter++;
                                    continue;
                                }
                            }
                        } else {
                            if (chunkData.rocksCalculated == true) {
                                if (chunkData.generatingRocks == true) {
                                    chunkData.rockCounter = 0;
                                    chunkData.generatingRocks = false;
                                }

                                if (chunkData.rockCounter == chunkData.rocks.Count) {
                                    chunkData.rocksGenerated = false;
                                    chunkData.rockCounter = 0;
                                } else {
                                    HideRocks(chunkData);
                                    chunkData.rockCounter++;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

    }

    private void GenerateMesh(ChunkData chunkData, int terrainLengthAux, int spaceBetweenVerticesAux, Vector2 center, int ownLod) {
        int xBorder = 0;
        int yBorder = 0;

        /*float xChunk = chunkData.chunk.xChunk;
        float yChunk = chunkData.chunk.yChunk;

        // look at x axis neighbor
        if (xChunk > center.x) xBorder = 1;
        else if (xChunk < center.x) xBorder = -1;

        if (xBorder != 0) {
            Vector2 key = new Vector2(xChunk + xBorder, yChunk);

            float chunkPositionX = key.x * maxTerrainLength * terrainMultiplier;
            float chunkPositionY = key.y * maxTerrainLength * terrainMultiplier;
            float chunkDistanceToCenter = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(playerTransform.position.x - chunkPositionX), 2) + Mathf.Pow(Mathf.Abs(playerTransform.position.z - chunkPositionY), 2));
            int lod = CalculateLOD(chunkDistanceToCenter);

            if (lod == ownLod) {
                xBorder = 0;
            }
        }

        // look at y axis neighbor
        if (yChunk > center.y) yBorder = 1;
        else if (yChunk < center.y) yBorder = -1;

        if (yBorder != 0) {
            Vector2 key = new Vector2(xChunk, yChunk + yBorder); 

            float chunkPositionX = key.x * maxTerrainLength * terrainMultiplier;
            float chunkPositionY = key.y * maxTerrainLength * terrainMultiplier;
            float chunkDistanceToCenter = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(playerTransform.position.x - chunkPositionX), 2) + Mathf.Pow(Mathf.Abs(playerTransform.position.z - chunkPositionY), 2));
            int lod = CalculateLOD(chunkDistanceToCenter);

            if (lod == ownLod) {
                yBorder = 0;
            }
        }*/

        MeshGenerator meshGenerator = new MeshGenerator(terrainLengthAux, chunkData.chunk.heightValues, terrainHeight, maxTerrainLength, spaceBetweenVerticesAux, terrainMultiplier, xBorder, yBorder);
        
        ThreadPool.QueueUserWorkItem(new WaitCallback(meshGenerator.GenerateMesh), chunkData);
    }

    private void AsignMeshAndCollider(ChunkData chunkData, int terrainLengthAux, int spaceBetweenVerticesAux) {
        MeshInfo meshInfo = chunkData.meshInfo;

        Color[] colors = new Color[terrainLengthAux * terrainLengthAux];
        Vector2[][] colors2 = new Vector2[7][];
        if (biomeController.biomes.Length > 4) {
            int colors2Length = Mathf.FloorToInt((float)((biomeController.biomes.Length - 4) / 2.0f) - 0.5f) + 1;
            for(int i = 0; i < colors2Length; i++) {
                colors2[i] = new Vector2[terrainLengthAux * terrainLengthAux];
            }
        }

        for (int y = 0; y < terrainLengthAux + 2; y++) {
            for (int x = 0; x < terrainLengthAux + 2; x++) {
                int yCoord = y * spaceBetweenVerticesAux - (spaceBetweenVerticesAux - 1);
                int xCoord = x * spaceBetweenVerticesAux - (spaceBetweenVerticesAux - 1);
                if (y == 0 || x == 0 || y == terrainLengthAux + 1 || x == terrainLengthAux + 1) {
                    continue;
                }

                for (int i = 0; i < biomeController.biomes.Length; i++) {
                    float biomeValue = chunkData.chunk.biomeValues[i][xCoord, yCoord];
                    if (i < 4) {
                        float color1 = colors[(x - 1) + (y - 1) * terrainLengthAux].r;
                        float color2 = colors[(x - 1) + (y - 1) * terrainLengthAux].g;
                        float color3 = colors[(x - 1) + (y - 1) * terrainLengthAux].b;

                        switch (i) {
                            case 0:
                                colors[(x - 1) + (y - 1) * terrainLengthAux] = new Color(biomeValue, 0, 0, 0);
                                break;
                            case 1:
                                colors[(x - 1) + (y - 1) * terrainLengthAux] = new Color(color1, biomeValue, 0, 0);
                                break;
                            case 2:
                                colors[(x - 1) + (y - 1) * terrainLengthAux] = new Color(color1, color2, biomeValue, 0);
                                break;
                            case 3:
                                colors[(x - 1) + (y - 1) * terrainLengthAux] = new Color(color1, color2, color3, biomeValue);
                                break;
                        }
                    } else {
                        int value = Mathf.FloorToInt((i - 4) / 2);
                        if (i % 2 == 0) {
                            colors2[value][(x - 1) + (y - 1) * terrainLengthAux] = new Vector2(biomeValue, 0);
                        } else {
                            float color1 = colors2[value][(x - 1) + (y - 1) * terrainLengthAux].x;
                            colors2[value][(x - 1) + (y - 1) * terrainLengthAux] = new Vector2(color1, biomeValue);
                        }
                    }
                }
            }
        }

        TerrainInfo terrainMeshInfo = new TerrainInfo(meshInfo.vertices, meshInfo.triangles, meshInfo.uvs, meshInfo.normals, colors, colors2, chunkData);
        terrainInfoQueue.Enqueue(terrainMeshInfo);
    }

    private void AsignMeshFromQueue() {
        if (terrainInfoQueue.Count <= 0) 
            return;

        int terrainQueueCount;
        if (terrainInfoQueue.Count < 3)
            terrainQueueCount = terrainInfoQueue.Count;
        else {
            terrainQueueCount = 3;
        }

        int counter = 0;

        for (int j = 0; j < terrainMeshes.Length; j++) {
            if (terrainMeshes[j].isAsigned == false) {
                counter++;
            }
        }

        if(counter < terrainQueueCount) {
            terrainQueueCount = counter;
        }

        counter = 0;

        for (int i = 0; i < terrainQueueCount; i++) {
            TerrainInfo terrainInfo = terrainInfoQueue.Dequeue();

            for (int j = counter; j < terrainMeshes.Length; j++, counter++) {
                if (terrainMeshes[j].isAsigned == false) {
                    Mesh mesh = terrainMeshes[j].mesh;
                    mesh.vertices = terrainInfo.vertices;
                    mesh.triangles = terrainInfo.triangles;
                    mesh.uv = terrainInfo.uvs;
                    mesh.normals = terrainInfo.normals;
                    mesh.colors = terrainInfo.colors;
                    mesh.uv2 = terrainInfo.colors2[0];
                    mesh.uv3 = terrainInfo.colors2[1];
                    mesh.uv4 = terrainInfo.colors2[2];

                    Chunk chunk = terrainInfo.chunkData.chunk;

                    chunk.meshFilter.sharedMesh = mesh;
                    chunk.meshFilter.sharedMesh.RecalculateBounds();
                    chunk.meshFilter.sharedMesh.RecalculateNormals();

                    if (chunk.meshCollider.sharedMesh != null) {
                        chunk.meshCollider.sharedMesh.Clear();
                    }

                    terrainMeshes[j].isAsigned = true;

                    terrainInfo.chunkData.meshQueueInfoIndex = j;
                    terrainInfo.chunkData.chunk = chunk;

                    break;
                }
            }
        }
    }

    int SortByDistanceToMe(Tree a, Tree b) {
        float squaredRangeA = (a.position - playerTransform.position).sqrMagnitude;
        float squaredRangeB = (b.position - playerTransform.position).sqrMagnitude;
        return squaredRangeA.CompareTo(squaredRangeB);
    }

    private void CalculateTrees(ChunkData chunkData) {
        float halfTerrain = terrainLength * terrainMultiplier / 2;

        List<Tree> trees = new List<Tree>();

        for (int y = 1; y < terrainLength + 2; y++) {
            for (int x = 1; x < terrainLength + 2; x++) {
                Vector2 key = new Vector2(x, y);

                float biomeValue = chunkData.chunk.temperatureValues[x, y];
                int largeModels = biomeController.biomes[(int)biomeValue].largeModels.Length;
                if (largeModels <= 0 || chunkData.chunk.treeValues[x, y] <= 0.5f) {
                    continue;
                } else {
                    GameObject gameObject = null;
                    float treeValue = chunkData.chunk.treeValues[x, y];
                    for (int i = 0; i < largeModels; i++) {
                        if (UnityEngine.Random.Range(0, 1000) <= biomeController.biomes[(int)biomeValue].largeModels[i].randomness * 100) {
                            gameObject = biomeController.biomes[(int)biomeValue].largeModels[i].model;
                            break;
                        }
                    }

                    if(gameObject == null) {
                        continue;
                    }

                    int xCoord = (int)key.x * terrainMultiplier;
                    int yCoord = (int)key.y * terrainMultiplier;

                    Vector3 position = new Vector3(
                        chunkData.chunk.xPos - halfTerrain + xCoord,
                        chunkData.chunk.treeValues[x, y] * terrainHeight - 0.5f,
                        chunkData.chunk.yPos + halfTerrain - yCoord);

                    Tree tree = new Tree(position, chunkData, gameObject);

                    trees.Add(tree);
                }
            }
        }

        //trees.Sort(SortByDistanceToMe);

        chunkData.treesCalculated = true;
        chunkData.trees = trees;
    }

    private void CalculateRocks(ChunkData chunkData) {
        float halfTerrain = terrainLength * terrainMultiplier / 2;

        List<Tree> rocks = new List<Tree>();

        for (int y = 1; y < terrainLength + 2; y++) {
            for (int x = 1; x < terrainLength + 2; x++) {
                Vector2 key = new Vector2(x, y);

                float biomeValue = chunkData.chunk.temperatureValues[x, y];
                int smallModels = biomeController.biomes[(int)biomeValue].smallModels.Length;
                if (smallModels <= 0 || chunkData.chunk.rockValues[x, y] <= 0.3f) {
                    continue;
                } else {
                    GameObject gameObject = null;
                    for (int i = 0; i < smallModels; i++) {
                        if (UnityEngine.Random.Range(0, 1000) <= biomeController.biomes[(int)biomeValue].smallModels[i].randomness * 1000) {
                            gameObject = biomeController.biomes[(int)biomeValue].smallModels[i].model;
                            break;
                        }
                    }

                    if (gameObject == null) {
                        continue;
                    }

                    int xCoord = (int)key.x * terrainMultiplier;
                    int yCoord = (int)key.y * terrainMultiplier;

                    Vector3 position = new Vector3(
                        chunkData.chunk.xPos - halfTerrain + xCoord,
                        chunkData.chunk.treeValues[x, y] * terrainHeight - 0.5f,
                        chunkData.chunk.yPos + halfTerrain - yCoord);

                    Tree tree = new Tree(position, chunkData, gameObject);

                    rocks.Add(tree);
                }
            }
        }

        chunkData.rocksCalculated = true;
        chunkData.rocks = rocks;
    }

    private void InstanciateObjectInQueue() {
        Tree tree = gameObjectsQueue.Dequeue();

        GameObject treeGameObject = Instantiate(tree.model);
        treeGameObject.name = "tree";
        treeGameObject.transform.position = tree.position;
        treeGameObject.transform.parent = tree.chunkData.chunk.meshObject.transform;
        tree.treeGameObject = treeGameObject;

        tree.isGenerated = true;
    }

    private void GenerateTrees(ChunkData chunkData) {

        List<Tree> trees = chunkData.trees;

        Tree tree = trees[chunkData.treeCounter];

        if (tree.isGenerated == false) {
            gameObjectsQueue.Enqueue(tree);

        } else {
            if (tree.treeGameObject != null) {
                if (tree.treeGameObject.activeSelf == false) {
                    tree.treeGameObject.SetActive(true);
                }
            }
        }
    }

    private void GenerateRocks(ChunkData chunkData) {

        List<Tree> rocks = chunkData.rocks;

        Tree rock = rocks[chunkData.rockCounter];

        if (rock.isGenerated == false) {

            gameObjectsQueue.Enqueue(rock);

        } else {
            if (rock.treeGameObject != null) {
                if (rock.treeGameObject.activeSelf == false) {
                    rock.treeGameObject.SetActive(true);
                }
            }
        }
    }

    private void HideTrees(ChunkData chunkData) {

        List<Tree> trees = chunkData.trees;

        Tree tree = trees[chunkData.treeCounter];
        if (tree.treeGameObject != null) {
            if (tree.treeGameObject.activeSelf == true) {
                tree.treeGameObject.SetActive(false);
                return;
            }
        }
    }

    private void HideRocks(ChunkData chunkData) {

        List<Tree> rocks = chunkData.rocks;

        Tree rock = rocks[chunkData.rockCounter];
        if (rock.treeGameObject != null) {
            if (rock.treeGameObject.activeSelf == true) {
                rock.treeGameObject.SetActive(false);
                return;
            }
        }
    }

    private int CalculateLOD(float chunkDistanceToCenter) {
        int lod = terrainMaxLOD;

        float minLodCenterDst = renderDistance / 2;

        if(chunkDistanceToCenter < minLodCenterDst)
            chunkDistanceToCenter = minLodCenterDst;

        float oldMax = renderDistance;
        float oldMin = minLodCenterDst;
        float oldRange = (oldMax - oldMin);
        float newRange = (terrainMaxLOD - terrainMinLOD);
        lod = Mathf.RoundToInt((((chunkDistanceToCenter - oldMin) * newRange) / oldRange) + terrainMinLOD);

        lod = terrainMaxLOD - ((lod + (terrainMaxLOD - terrainMinLOD)) % terrainMaxLOD);

        return lod;
    }

    public void UpdateGrass() {
        int playerChunkX = Mathf.FloorToInt(playerTransform.position.x / ((float)maxTerrainLength * terrainMultiplier / 4) + 2);
        int playerChunkY = Mathf.FloorToInt(playerTransform.position.z / ((float)maxTerrainLength * terrainMultiplier / 4) + 2);

        if (playerChunkX != lastPlayerGrassPosition.x || playerChunkY != lastPlayerGrassPosition.y) {
            grassChunksInSight = new List<Vector2>();

            for (int yChunk = playerChunkY - chunkDistance; yChunk <= playerChunkY + chunkDistance; yChunk++) {
                for (int xChunk = playerChunkX - chunkDistance; xChunk <= playerChunkX + chunkDistance; xChunk++) {
                    if (xChunk >= playerChunkX - 3 && xChunk <= playerChunkX + 3 && yChunk >= playerChunkY - 3 && yChunk <= playerChunkY + 3) {
                        Vector2 grassKeyPosition = new Vector2(xChunk, yChunk);
                        if (grassData.ContainsKey(grassKeyPosition)) {
                            grassChunksInSight.Add(grassKeyPosition);
                            GenerateGrass(grassKeyPosition);
                        }
                    }
                }
            }

            List<Vector2> chunksNotInSight = grassData.Keys.Except(grassChunksInSight).ToList();

            foreach (Vector2 keyPosition in chunksNotInSight) {
                if (grassData.ContainsKey(keyPosition)) {
                    GrassData grass = grassData[keyPosition];
                    if (grass.meshObject != null) {
                        for (int i = 0; i < grassMaterialList.Length; i++) {
                            if (grass.meshObject[i] != null) {
                                grass.meshObject[i].SetActive(false);
                            }
                        }
                        grassData[keyPosition] = grass;
                    }
                }
            }

            lastPlayerGrassPosition.x = playerChunkX;
            lastPlayerGrassPosition.y = playerChunkY;
        }
    }

    public void GenerateGrass(Vector2 keyPosition) {
        GrassData grass = grassData[keyPosition];
        if (grass.meshObject[0] == null) {
            for(int i = 0; i < grassMaterialList.Length; i++) {
                grass.meshObject[i] = new GameObject("Grass_Object_" + "_" + i);

                Mesh grassTerrainMesh = new Mesh();

                float[] temperatures = grass.temperatures[i];
                Color[] colors = new Color[temperatures.Length];
                for (int j = 0; j < temperatures.Length; j++) {
                    float temperature = temperatures[j];
                    colors[j] = new Color(temperature, temperature, temperature);
                }

                grassTerrainMesh.SetVertices(grass.positions[i]);
                grassTerrainMesh.SetIndices(grass.indices[i], MeshTopology.Points, 0);
                grassTerrainMesh.SetNormals(grass.normals[i]);
                grassTerrainMesh.SetColors(colors);

                grass.meshFilter[i] = grass.meshObject[i].AddComponent<MeshFilter>();
                grass.meshFilter[i].mesh = grassTerrainMesh;
                grass.meshRenderer[i] = grass.meshObject[i].AddComponent<MeshRenderer>();

                grass.meshRenderer[i].material = grassMaterialList[i];

                grass.meshObject[i].transform.parent = grass.parentGameObject.transform;

                //grass.meshObject.transform.position = new Vector3(grass.xPos, 0, grass.yPos);
            }


            grassData[keyPosition] = grass;
        } else {
            for (int i = 0; i < grassMaterialList.Length; i++) {
                if (grass.meshObject[i] != null) {
                    grass.meshObject[i].SetActive(true);
                }
            }
            grassData[keyPosition] = grass;
        }
    }
}

public class Tree {
    public Vector3 position;
    public bool isGenerated;
    public GameObject model;
    public GameObject treeGameObject;
    public ChunkData chunkData;

    public Tree(Vector3 position, ChunkData chunkData, GameObject model) {
        this.position = position;
        this.isGenerated = false;
        this.model = model;
        this.treeGameObject = null;
        this.chunkData = chunkData;
    }
}

public class TerrainInfo {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] normals;
    public Color[] colors;
    public Vector2[][] colors2;

    public ChunkData chunkData;

    public TerrainInfo(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3[] normals, Color[] colors, Vector2[][] colors2, ChunkData chunkData) {
        this.vertices = vertices;
        this.uvs = uvs;
        this.triangles = triangles;
        this.normals = normals;
        this.colors = colors;
        this.colors2 = colors2;
        this.chunkData = chunkData;
    }
}

public class MeshQueueInfo {
    public Mesh mesh;
    public bool isAsigned = false;
} 

