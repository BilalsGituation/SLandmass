using UnityEngine;
using System.Collections;

public class MeshGenerator 
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int LevelOfDetail) {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize -2;
        float topLeftX = (meshSize -1)/-2f;
        float topLeftZ = (meshSize -1)/2f;

        int meshSimplificationIncrement = (LevelOfDetail==0)?1:LevelOfDetail * 2;
        int verticesPerLine = (meshSize - 1)/meshSimplificationIncrement+1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        
        
        int[,] vertexIndicesMap = new int[borderedSize,borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex =-1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x+=meshSimplificationIncrement) {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex) {
                    vertexIndicesMap[x,y]=borderVertexIndex;
                    borderVertexIndex--;
                } else {
                    vertexIndicesMap[x,y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y+=meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x+=meshSimplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x,y];
                Vector2 percent = new Vector2((x- meshSimplificationIncrement)/(float)meshSize, (y-meshSimplificationIncrement)/(float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x,y])*heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX+percent.x*meshSize, height, topLeftZ-percent.y*meshSize);
                

                if (x < borderedSize-1 && y < borderedSize -1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine+1, vertexIndex+verticesPerLine);
                    meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex, vertexIndex+1);
                }

                vertexIndex++;

            }
        }
        return meshData;

    }
}

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshborderedSize, int meshHeight) {
        vertices = new Vector3[meshborderedSize * meshHeight];
        uvs = new Vector2[meshborderedSize * meshHeight];
        triangles = new int[((meshborderedSize-1) * (meshHeight-1)) * 6];
    }

    public void AddTriangle(int a, int b, int c) {
        triangles [triangleIndex] = a;
        triangles [triangleIndex+1] = b;
        triangles [triangleIndex+2] = c;
        triangleIndex += 3;
    }

    Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length/3;
        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i*3;
            int vertexIndexA = triangles [normalTriangleIndex];
            int vertexIndexB = triangles [normalTriangleIndex+1];
            int vertexIndexC = triangles [normalTriangleIndex+2];

            Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB,vertexIndexC);
            vertexNormals[vertexIndexA]+=triangleNormal;
            vertexNormals[vertexIndexB]+=triangleNormal;
            vertexNormals[vertexIndexC]+=triangleNormal;

        }
        for (int i = 0; i<vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }
    return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = vertices [indexA];
        Vector3 pointB = vertices [indexB];
        Vector3 pointC = vertices [indexC];

        Vector3 sideAB = pointB-pointA;
        Vector3 sideAC = pointC-pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = CalculateNormals();
        return mesh;
    }
}

