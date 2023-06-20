using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour {
    // map properties
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;
    public float persistence;
    public float lacunarity;

    public bool autoUpdate;

    public void GenerateMap() {
        float[,] noiseMap = Noise.GenerateNoiseMap (mapWidth, mapHeight, noiseScale, octaves, persistence, lacunarity);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }
}