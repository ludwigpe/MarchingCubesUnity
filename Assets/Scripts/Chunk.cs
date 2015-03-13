using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Chunk {
    const int MAX_TRIANGLES = 21666; // 64 998 vertices max is 65 k
    public Vector3 wsPosLL;
    public Vector3 wsChunkDim;
    public int voxelDim;
    public Material chunkMatProcedural {get; set;}
    public Material chunkMatMesh { get; set; }
    public ComputeBuffer triangleBuffer {get; set;}
    public ComputeBuffer argumentBuffer{get; set;}

    public Chunk(Vector3 posLL, Vector3 chunkDim, int voxDim)
    {
        this.wsPosLL = posLL;
        this.wsChunkDim = chunkDim;
        this.voxelDim = voxDim;
        triangleBuffer = Helper.GetNewTriangleBuffer(voxDim);
        argumentBuffer = Helper.GetArgumentBuffer();
        
    }
    public GameObject GenerateChunkObject()
    {
        GameObject chunkObject = new GameObject("Chunk");
        int[] args = new int[]{ 0, 1, 0, 0 };
        argumentBuffer.GetData(args);
        
        int numTriangles = args[0];
        int numMeshes = Mathf.CeilToInt( MAX_TRIANGLES / numTriangles);
        int numTrianglesLeft = numTriangles;
        
        Vector3[] triBuffer = new Vector3(numTriangles * 6);
        triangleBuffer.GetData(triBuffer);

        int buffIndex = 0;
        for(int meshNum = 0; meshNum < numMeshes; meshNum++)
        {
            GameObject obj = new GameObject("Chunk Part");
            Mesh mesh = GenerateMesh(ref triBuffer, ref buffIndex, ref numTrianglesLeft);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.renderer.material = chunkMatMesh;
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.transform.parent = chunkObject.transform;
            obj.transform.position = chunkObject.transform.position;

        }
        return chunkObject;
    }

    private Mesh GenerateMesh(ref Vector3[] triBuffer, ref int buffIndex, ref int numTrianglesLeft)
    {
        Mesh mesh= new Mesh();
        int triCount = Mathf.Min(numTrianglesLeft, MAX_TRIANGLES);
        Vector3[] vertices = new Vector3[3 * triCount];
        Vector3[] normals = new Vector3[3 * triCount];
        int[] indices = new int[3 * triCount];
        int vertIndex = 0;
        // loop through each triangle in the triangle buffer
        for(int i = 0; i < triCount; i++)
        {
            // 1 triangle consists of 3 vertices so do 3 loops to extract 1 triangle
            // advances the buffIndex by one since vertices and normals are interleaved
            // each loop advancess the vertexIndex by one.
            for (int j = 0; j < 3; j++ )
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
    public void Draw()
    {
        chunkMatProcedural.SetPass(0);
        chunkMatProcedural.SetBuffer("triangleBuffer", triangleBuffer);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, argumentBuffer, 0);
    }
    public void Release()
    {
        triangleBuffer.Release();
        argumentBuffer.Release();
    }
    ~Chunk()
    {
        triangleBuffer.Release();
        argumentBuffer.Release();
    }
}
