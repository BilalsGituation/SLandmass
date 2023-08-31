using System.Collections;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData {
    
    public const int numSupportedLODs =5;
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatShadedChunkSizes = 3;
    
    public static readonly int[ ] supportedChunkSizes = {48,72,96,120,144,168,192,216,240};
    
    
    public float meshScale=2f;
    public bool useFlatShading;

    [Range(0,numSupportedChunkSizes-1)]
    public int chunkSizeIndex;
    [Range(0,numSupportedFlatShadedChunkSizes-1)]
    public int flatShadedChunkSizeIndex;

    // of mesh with LOD=0. Includes 2 vertices excluded in final mesh used in normal calc
    public int numVerticesPerLine {
        get {
            
            return supportedChunkSizes[(useFlatShading)?flatShadedChunkSizeIndex:chunkSizeIndex]+5;
            
        }
    }
    
    public float meshWorldSize {
        get {
            return (numVerticesPerLine-3) * meshScale;
            }
    }
    
}
