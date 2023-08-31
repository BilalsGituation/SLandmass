using UnityEngine;
using System.Collections;

public class MeshGenerator 
{

    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int LevelOfDetail) {

        int skipIncrement = (LevelOfDetail==0)?1:LevelOfDetail * 2;

        int numVerticesPerLine = meshSettings.numVerticesPerLine;

        Vector2 topLeft = new Vector2(-1,1)* meshSettings.meshWorldSize/2f;


        MeshData meshData = new MeshData(numVerticesPerLine, skipIncrement, meshSettings.useFlatShading);
        
        
        int[,] vertexIndicesMap = new int[numVerticesPerLine,numVerticesPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex =-1;

        for (int y = 0; y < numVerticesPerLine; y ++) {
            for (int x = 0; x < numVerticesPerLine; x++) {
                bool isOutOfMeshVertex = y == 0 || y == numVerticesPerLine - 1 || x == 0 || x == numVerticesPerLine - 1;
                bool isSkippedVertex = x>2 && x < numVerticesPerLine-3 && y >2 && y < numVerticesPerLine-3 &&((x-2)%skipIncrement!=0||(y-2) % skipIncrement!=0);
                if (isOutOfMeshVertex) {
                    vertexIndicesMap[x,y]=outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                } else if (!isSkippedVertex) {
                    vertexIndicesMap[x,y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numVerticesPerLine; y++) {
            for (int x = 0; x < numVerticesPerLine; x++) {
                bool isSkippedVertex = x>2 && x < numVerticesPerLine-3 && y >2 && y < numVerticesPerLine-3 &&((x-2)%skipIncrement!=0||(y-2) % skipIncrement!=0);
                if (!isSkippedVertex) {

                    bool isOutOfMeshVertex = y == 0 || y == numVerticesPerLine - 1 || x == 0 || x == numVerticesPerLine - 1;
                    bool isMeshEdgeVertex = (y ==1 || y == numVerticesPerLine -2 || x== 1 || x == numVerticesPerLine-2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x-2)%skipIncrement ==0 && (y-2)%skipIncrement ==0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y==2 || y == numVerticesPerLine -3||x==2||x==numVerticesPerLine-3)&& !isOutOfMeshVertex && !isMainVertex && !isMeshEdgeVertex;   

                    int vertexIndex = vertexIndicesMap[x,y];
                    Vector2 percent = new Vector2(x-1,y-1)/(numVerticesPerLine-3);
                    
                    Vector2 vertexPosition2D = topLeft + new Vector2 (percent.x,-percent.y) * meshSettings.meshWorldSize;
                    float height = heightMap[x,y];
                    
                    meshData.AddVertex(new Vector3(vertexPosition2D.x,height,vertexPosition2D.y),percent, vertexIndex);
                    
                    bool createTriangle = x < numVerticesPerLine-1 && y < numVerticesPerLine-1 &&(!isEdgeConnectionVertex || (x!=2&&y!=2));

                    if (createTriangle) {
                        int currentIncrement = (isMainVertex && x != numVerticesPerLine-3 && y != numVerticesPerLine-3)?skipIncrement:1;

                        int a = vertexIndicesMap[x,y];
                        int b = vertexIndicesMap[x+currentIncrement,y];
                        int c = vertexIndicesMap[x,y+currentIncrement];
                        int d = vertexIndicesMap[x+currentIncrement,y+currentIncrement];
                        meshData.AddTriangle(a,d,c);
                        meshData.AddTriangle(d,a,b);
                    }

                    vertexIndex++;
                }
            }
        }
        meshData.ProcessMesh();
        return meshData;

    }
}

public class MeshData {
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] outOfMeshVertices;
    int[] outOfMeshTriangles;

    int triangleIndex;
    int outOfMeshTriangleIndex;

    bool useFlatShading;

    public MeshData(int numVerticesPerLine, int skipIncrement, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        int numMeshEdgeVerts = (numVerticesPerLine-2)*4-4;
        int numEdgeConnectionVerts = (skipIncrement-1)*((numVerticesPerLine-5)/skipIncrement*4);
        int numMainVertsPerLine = (numVerticesPerLine-5)/skipIncrement+1;
        int numMainVerts = numMainVertsPerLine*numMainVertsPerLine;

        vertices = new Vector3[numMeshEdgeVerts+numEdgeConnectionVerts+numMainVerts];
        uvs = new Vector2[vertices.Length];

        int numMeshEdgeTriangles = 8*(numVerticesPerLine-4);
        int numMainTriangles = (numMainVertsPerLine-1)*(numMainVertsPerLine-1)*2;
        triangles = new int[(numMeshEdgeTriangles+numMainTriangles)*3];

        outOfMeshVertices = new Vector3[numVerticesPerLine*4-4];
        outOfMeshTriangles = new int[24*(numVerticesPerLine-2)]; //numVerticesPerLine-1)*4-4)*6

    }

    public void AddVertex (Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex<0) {
            outOfMeshVertices[-vertexIndex-1] = vertexPosition;
        } else {
            vertices [vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if (a<0||b<0||c<0) {
            outOfMeshTriangles [outOfMeshTriangleIndex] = a;
            outOfMeshTriangles [outOfMeshTriangleIndex+1] = b;
            outOfMeshTriangles [outOfMeshTriangleIndex+2] = c;
            outOfMeshTriangleIndex += 3;
        } else{
            triangles [triangleIndex] = a;
            triangles [triangleIndex+1] = b;
            triangles [triangleIndex+2] = c;
            triangleIndex += 3;
        }
        
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
        int outOfMeshTriangleCount = outOfMeshTriangles.Length/3;
        for (int i = 0; i < outOfMeshTriangleCount; i++) {
            int normalTriangleIndex = i*3;
            int vertexIndexA = triangles [normalTriangleIndex];
            int vertexIndexB = triangles [normalTriangleIndex+1];
            int vertexIndexC = triangles [normalTriangleIndex+2];

            Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB,vertexIndexC);
            if (vertexIndexA>=0) {
                vertexNormals[vertexIndexA]+=triangleNormal;
            }
            if (vertexIndexB>=0) {
                vertexNormals[vertexIndexB]+=triangleNormal;
            }
            if (vertexIndexC>=0) {
                vertexNormals[vertexIndexC]+=triangleNormal;
            }

        }
        
        for (int i = 0; i<vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }
    return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA<0)? outOfMeshVertices[-indexA-1] : vertices [indexA];
        Vector3 pointB = (indexB<0)? outOfMeshVertices[-indexB-1] : vertices [indexB];
        Vector3 pointC = (indexC<0)? outOfMeshVertices[-indexC-1] : vertices [indexC];

        Vector3 sideAB = pointB-pointA;
        Vector3 sideAC = pointC-pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh() {
        if (useFlatShading) {
            FlatShading();
        } else {
            BakeNormals();
        }
    }

    private void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    void FlatShading() {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }
        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading) {
            mesh.RecalculateNormals();
        } else {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }
}

