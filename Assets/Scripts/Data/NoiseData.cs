using System.Collections;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData {
    public Noise.NormalizeMode normalizeMode;
    public float noiseScale;

    [Range(0, 10)]
    public int octaves;//nextline//
    [Range(0,1)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    protected override void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        if (octaves < 0) {
            octaves = 0;
        }

        base.OnValidate();
    }
}
