using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { noiseMap, colourMap, Mesh };
    public DrawMode drawMode;

    // map properties
    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int LevelOfDetail;
    public float noiseScale;

    [Range(0, 10)]
    public int octaves;
    //[Range(0,1)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;
    public TerrainType[] regions;

    public void DrawMapInEditor() {
        MapData mapData= GenerateMapData();

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.noiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.colourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, LevelOfDetail), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
    }


    MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colourMap);
        
    }
    void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string Name;
    public float height;
    public Color colour;

}

public struct MapData {
    public float[,] heightMap;
    public Color[] colourMap;
    public MapData(float[,] heightMap, Color[] colourMap) {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}