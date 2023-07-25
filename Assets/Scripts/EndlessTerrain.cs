using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{   
    
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public static float maxViewDistance = 1000;
    public LODInfo[] detailLevels;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDistance)/chunkSize;
        UpdateVisibleChunks();
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude>sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
        
    }

    void UpdateVisibleChunks () {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt((viewerPosition.y/chunkSize));

        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX+xOffset, currentChunkCoordY+yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible()) {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                } else {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        MapData mapData;
        bool mapDataReceived;
        int prevLODIndex = -1;


        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position,Vector2.one*size);
            Vector3 positionV3 = new Vector3(position.x,0,position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;//nextline//meshObject.transform.localScale = Vector3.one*size/10f;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int  i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider) {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
           this.mapData = mapData;
           mapDataReceived = true;

           UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if (mapDataReceived) {
            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistFromNearestEdge <= maxViewDistance;

            if (visible) {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length -1; i++) {
                    if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold) {
                        lodIndex = i+1;
                    } else {
                        break;
                    }
                }
                if (lodIndex != prevLODIndex) {
                    LODMesh lODMesh = lodMeshes[lodIndex];
                    if (lODMesh.hasMesh) {
                        prevLODIndex = lodIndex;
                        meshFilter.mesh = lODMesh.mesh;
                    }
                    else if (!lODMesh.hasRequestedMesh) {
                        lODMesh.RequestMesh(mapData);
                    }
                }

                if (lodIndex == 0) {
                    if (collisionLODMesh.hasMesh) {
                        meshCollider.sharedMesh = collisionLODMesh.mesh;
                    } else if (!collisionLODMesh.hasRequestedMesh) {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }

            SetVisible (visible);
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }

    class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived (MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDistThreshold;
        public bool useForCollider;
    }
}

