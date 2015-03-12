using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class BuildDensityMap : MonoBehaviour
{
    const int SIZE = 32 * 32 * 32 * 5 * 3;
	Chunk testChunk;

    public ComputeShader m_buildDensityShader, m_genVerticesShader;
    public ComputeShader m_genVertices2;
	public Material m_chunkMat;
	public bool DEBUG = true;
	private Texture3D[] m_noiseVols = new Texture3D[4];
    private RenderTexture m_densityTexture;
	private ComputeBuffer m_caseToNumpolys, m_triangleConnectionTable, m_argBuffer;
    private ComputeBuffer m_triangleBuffer;

	// Use this for initialization
	void Start () 
    {

		testChunk = new Chunk(new Vector3 (0, 0, 0), new Vector3 (10, 10, 10), 32 );

		// Load all volume files into Texture3D format for use in shaders
        m_noiseVols[0] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol1.vol");
        m_noiseVols[1] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol2.vol");
        m_noiseVols[2] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol3.vol");
        m_noiseVols[3] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol4.vol");

		// create the compute buffer for case lookup
        m_caseToNumpolys = Helper.GetCaseToNumPolyBuffer();

		// create the compute buffer for triangle connection lookup
        m_triangleConnectionTable = Helper.GetTriangleConnectionTable();

        m_argBuffer = Helper.GetArgumentBuffer();
		m_argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);
		int[] args = new int[]{ 0, 1, 0, 0 };
		m_argBuffer.SetData(args);

		BuildChunk (ref m_triangleBuffer, testChunk);
	
	}

    // Update is called once per frame
    void Update()
    {
        
    }
	void OnPostRender ()
	{     

        m_chunkMat.SetPass(0);
        m_chunkMat.SetBuffer("triangleBuffer", m_triangleBuffer);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, m_argBuffer, 0);
	}


	/// <summary>
	/// Builds the chunk given a chunk object and a vertex buffer to put the result in.
	/// </summary>
	/// <param name="vertexBuffer">Vertex buffer.</param>
	/// <param name="chunk">Chunk.</param>
	void BuildChunk(ref ComputeBuffer triangleBuffer, Chunk chunk)
	{

        int maxTriangleCount = chunk.voxelDim * chunk.voxelDim * chunk.voxelDim * 5;
        int vSize = (sizeof(float) * 3 + sizeof(float) * 3);
        int triangleSize = 3 * vSize;
        triangleBuffer = new ComputeBuffer(maxTriangleCount, triangleSize, ComputeBufferType.Append);
        //m_vertexBuffer = new ComputeBuffer (maxVertices, vertexSize, ComputeBufferType.Append);
		BuildDensity (chunk);
		BuildVertices (ref triangleBuffer, chunk);

	}

	/// <summary>
	/// Builds the density texture for a chunk in world space.
	/// </summary>
	/// <param name="chunk">Chunk.</param>
	void BuildDensity(Chunk chunk)
	{
		// create the density texture
        m_densityTexture = Helper.CreateDensityTexture(chunk.voxelDim);
		//CreateDensityTexture (chunk.voxelDim);

		// set all noiseTextures 
		m_buildDensityShader.SetTexture(0, "NoiseVol1", m_noiseVols[0]);
		m_buildDensityShader.SetTexture(0, "NoiseVol2", m_noiseVols[1]);
		m_buildDensityShader.SetTexture(0, "NoiseVol3", m_noiseVols[2]);
		m_buildDensityShader.SetTexture(0, "NoiseVol4", m_noiseVols[3]);

		// set the density texture where the comp shader will write to
		m_buildDensityShader.SetTexture (0, "densityTexture", m_densityTexture);

		// set extra values for computation
        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        m_buildDensityShader.SetFloat("invVoxelDim", invVoxelDim);
		m_buildDensityShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
		m_buildDensityShader.SetVector("wsChunkDim", chunk.wsChunkDim);

		m_buildDensityShader.Dispatch(0, 1, 1, 32);
        print("Dispatched Build Density");

	}


	void BuildVertices(ref ComputeBuffer triangleBuffer, Chunk chunk)
	{
        
		m_genVerticesShader.SetTexture (0, "densityTexture", m_densityTexture);
		m_genVerticesShader.SetBuffer (0, "case_to_numpolys", m_caseToNumpolys);
		m_genVerticesShader.SetBuffer (0, "edge_connect_list", m_triangleConnectionTable);
        m_genVerticesShader.SetBuffer(0, "triBuffer", triangleBuffer);

		float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
		m_genVerticesShader.SetFloat ("invVoxelDim", invVoxelDim);
		m_genVerticesShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
		m_genVerticesShader.SetVector("wsChunkDim", chunk.wsChunkDim);
        m_genVerticesShader.SetInt("voxelDim", chunk.voxelDim);
		int N = chunk.voxelDim;
		m_genVerticesShader.Dispatch (0, N/4, N/4, N/4);

        ComputeBuffer.CopyCount(triangleBuffer, m_argBuffer, 0);

		if (DEBUG) 
		{
			int[] args = new int[]{ 0, 1, 0, 0 };
			m_argBuffer.GetData(args);
			Debug.Log("vertex count " + args[0]);
			Debug.Log("instance count " + args[1]);
			Debug.Log("start vertex " + args[2]);
			Debug.Log("start instance " + args[3]);
            
		}



	}

	void PrintColors(Color[] colors)
	{
		foreach (Color c in colors) 
		{
			print(c);
		}
	}


    void OnDestroy()
    {
        m_caseToNumpolys.Release();
        m_triangleConnectionTable.Release();
        m_triangleBuffer.Release();
        m_argBuffer.Release();
    }
	


}
