using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Chunk {
    const int MAX_TRIANGLES = 21666; // 64 998 vertices max is 65 k
    const int MAX_VERTICES = 64998;
    const int SIZE = 32 * 32 * 32 * 5;
    public Vector3 wsPosLL;
    public Vector3 wsChunkDim;
    public int voxelDim;
    public int triangleCount { get; set; }
    public int vertexCount { get; set; }
    public int indexCount { get; set;}
    public int numNonemptyCells { get; set; }
    public bool isEmpty { get; set; }
    public Material chunkMatProcedural {get; set;}
    public Material chunkMatMesh { get; set; }
    public ComputeBuffer nonemptyListBuffer { get; set; }
    public ComputeBuffer triangleBuffer {get; set;}
    public ComputeBuffer vertexBuffer { get; set; }
    public ComputeBuffer indexBuffer { get; set; }
    public RenderTexture VertexIDVol { get; set; }
    public Chunk(Vector3 posLL, Vector3 chunkDim, int voxDim)
    {
        this.wsPosLL = posLL;
        this.wsChunkDim = chunkDim;
        this.voxelDim = voxDim;

        nonemptyListBuffer = new ComputeBuffer(voxDim * voxDim * voxDim, sizeof(int), ComputeBufferType.Default);
        triangleBuffer = new ComputeBuffer(SIZE, 3 * (6 * sizeof(float)), ComputeBufferType.Default);
        triangleCount = SIZE;

        int maxVertices = voxDim * voxDim * voxDim * 3; // every voxel can produce maximum 3 vertices
        vertexBuffer = new ComputeBuffer(maxVertices, 6 * sizeof(float), ComputeBufferType.Default);

        int maxIndices = voxDim * voxDim * voxDim * 15; // every voxel can produce maximum 15 indices into the vertex buffer
        indexBuffer = new ComputeBuffer(maxIndices, sizeof(int), ComputeBufferType.Default);

        // create the vertex index volume for index lookup.
        VertexIDVol = new RenderTexture(32+1, 32+1, 0, RenderTextureFormat.ARGBInt);
        VertexIDVol.enableRandomWrite = true;
        VertexIDVol.isVolume = true;
        VertexIDVol.volumeDepth = 32+1;
        VertexIDVol.Create();
        
    }

    public void Draw()
    {
        chunkMatProcedural.SetPass(0);
        chunkMatProcedural.SetBuffer("triangleBuffer", triangleBuffer);
        Graphics.DrawProcedural(MeshTopology.Points, triangleCount);
    }
    public void Release()
    {
        if(nonemptyListBuffer != null)
            nonemptyListBuffer.Release();
        if(triangleBuffer != null)
            triangleBuffer.Release();
        if (VertexIDVol != null)
            VertexIDVol.Release();
        if (vertexBuffer != null)
            vertexBuffer.Release();
        if (indexBuffer != null)
            indexBuffer.Release();
    }
    public GameObject GenerateChunkIndexed()
    {
        GameObject chunkObject = new GameObject("Chunk");
        int numTriangles = indexCount / 3;
        int numMeshes = Mathf.CeilToInt(numTriangles / MAX_TRIANGLES);
        int numTrianglesLeft = numTriangles;

        Vector3[] vertBuffer = new Vector3[vertexCount * 2];
        vertexBuffer.GetData(vertBuffer);

        int[] indices = new int[indexCount];
        indexBuffer.GetData(indices);
        int numVerticesLeft = vertexCount;
        int buffIndex = 0;
        while(numVerticesLeft > 0)
        {
            GameObject obj = new GameObject("Chunk Part");
            Mesh mesh;
            if (vertexCount <= MAX_VERTICES)
            {
                mesh = new Mesh();

                Vector3[] vertices = new Vector3[vertexCount];
                Vector3[] normals = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = vertBuffer[buffIndex];
                    normals[i] = vertBuffer[buffIndex + 1];
                    buffIndex += 2;
                }
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.triangles = indices;

                numVerticesLeft -= vertexCount;
            }
            else
            {

                mesh = GenerateMeshIndexed(ref vertBuffer, ref indices, ref buffIndex, ref numVerticesLeft);
           
            }

            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.GetComponent<Renderer>().material = chunkMatMesh;
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.transform.parent = chunkObject.transform;
            obj.transform.position = chunkObject.transform.position;
        }
        
       
        
        vertexBuffer.Release();
        indexBuffer.Release();
        return chunkObject;
    }
    public GameObject GenerateChunkObject()
    {
        GameObject chunkObject = new GameObject("Chunk");

        int numTriangles = triangleCount;
        int numMeshes = Mathf.CeilToInt(numTriangles / MAX_TRIANGLES);
        int numTrianglesLeft = numTriangles;

        Vector3[] triBuffer = new Vector3[numTriangles * 6];
        triangleBuffer.GetData(triBuffer);

        int buffIndex = 0;
        while(numTrianglesLeft > 0)
        {
            GameObject obj = new GameObject("Chunk Part");
            Mesh mesh = GenerateMesh(ref triBuffer, ref buffIndex, ref numTrianglesLeft);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.GetComponent<Renderer>().material = chunkMatMesh;
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.transform.parent = chunkObject.transform;
            obj.transform.position = chunkObject.transform.position;
        }
        triangleBuffer.Release();
        return chunkObject;
    }
    private Mesh GenerateMeshIndexed(ref Vector3[] vertBuffer, ref int[] indices, ref int startIndex, ref int numVerticesLeft)
    {
        Mesh mesh = new Mesh();
        int numVertices = Mathf.Min(numVerticesLeft, MAX_VERTICES);
        Vector3[] vertices = new Vector3[numVertices];
        Vector3[] normals= new Vector3[numVertices];
        Dictionary<int, int> vMap = new Dictionary<int, int>();
        List<int> vIndices = new List<int>();
        int numVerticesAdded = 0;
        int idx = startIndex;
        int vSize = 2; 
        while(numVerticesAdded < numVertices)
        {
            // get vertexID from indices, this is the index and ID of a vertex in the vertexBuffer
            int vID = indices[idx*vSize];
            // check if we have already added that vertex to the vertices array
            if(vMap.ContainsKey(vID))
            {
                // we have already added this vertex to the vertices array, get the index of that vertex
                vIndices.Add(vMap[vID]);
                idx++;
            }
            else
            {
                // the vertex has not been added, so do it
                vertices[numVerticesAdded] = vertBuffer[vID];
                normals[numVerticesAdded] = vertBuffer[vID + 1];
                // add the index to this vertex into the dictionary, the key is the vertexID and the value is the index into the vertices array
                vMap.Add(vID, numVerticesAdded);
                // add the index to the vertex into the vIndices array
                vIndices.Add(numVerticesAdded);
                numVerticesAdded++;
                idx++;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = vIndices.ToArray();
        startIndex += vIndices.Count;
        numVerticesLeft -= numVerticesAdded;
        return mesh;
    }
    private Mesh GenerateMesh(ref Vector3[] triBuffer, ref int buffIndex, ref int numTrianglesLeft)
    {
        Mesh mesh = new Mesh();
        int triCount = Mathf.Min(numTrianglesLeft, MAX_TRIANGLES);
        Vector3[] vertices = new Vector3[3 * triCount];
        Vector3[] normals = new Vector3[3 * triCount];
        int[] indices = new int[3 * triCount];
        int vertIndex = 0;
        // loop through each triangle in the triangle buffer
        for (int i = 0; i < triCount; i++)
        {
            // 1 triangle consists of 3 vertices so do 3 loops to extract 1 triangle
            // advances the buffIndex by one since vertices and normals are interleaved
            // each loop advancess the vertexIndex by one.
            for (int j = 0; j < 3; j++)
            {
                vertices[vertIndex] = triBuffer[buffIndex++];
                normals[vertIndex] = triBuffer[buffIndex++];
                indices[vertIndex] = vertIndex;
                vertIndex++;
            }

        }
        // decrease the number of triangles left by the amount consumed by this mesh.
        numTrianglesLeft -= triCount;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = indices;

        return mesh;

    }
}
