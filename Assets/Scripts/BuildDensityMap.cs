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
	public Material m_chunkMat;
	public bool DEBUG = true;
	private Texture3D[] m_noiseVols = new Texture3D[4];
    private RenderTexture m_densityTexture;

    private List<Chunk> m_chunkList;
    public Vector3 m_numberOfChunks = new Vector3(1,1,1);
	// Use this for initialization
	void Start () 
    {
        int numChunksTotal = (int)(m_numberOfChunks.x * m_numberOfChunks.y * m_numberOfChunks.z);
        m_chunkList = new List<Chunk>(numChunksTotal);

		//testChunk = new Chunk(new Vector3 (0, 0, 0), new Vector3 (10, 10, 10), 32 );
        //testChunk.chunkMat = m_chunkMat;

		// Load all volume files into Texture3D format for use in shaders
        m_noiseVols[0] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol1.vol");
        m_noiseVols[1] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol2.vol");
        m_noiseVols[2] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol3.vol");
        m_noiseVols[3] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol4.vol");
        m_densityTexture = Helper.CreateDensityTexture(32);
        CreateChunks();
		//BuildChunk ( testChunk);
	
	}

    // Update is called once per frame
    void Update()
    {
        
    }
	void OnPostRender ()
	{
        foreach(Chunk c in m_chunkList)
        {
            c.Draw();
        }
           
	}

    void CreateChunks ()
    {
        for(int z = 0; z < m_numberOfChunks.z; z++)
            for(int y = 0; y < m_numberOfChunks.y; y++)
                for(int x = 0; x < m_numberOfChunks.x; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    Vector3 chunkDim = new Vector3(2, 2, 2);
                    pos *= chunkDim.x;
                    Chunk c = new Chunk(pos, chunkDim, 32);
                    c.chunkMatProcedural = m_chunkMat;
                    BuildChunk(c);
                    m_chunkList.Add(c);
                }
    }
	/// <summary>
	/// Builds the chunk given a chunk object and a vertex buffer to put the result in.
	/// </summary>
	/// <param name="vertexBuffer">Vertex buffer.</param>
	/// <param name="chunk">Chunk.</param>
	void BuildChunk(Chunk chunk)
	{

		BuildDensity (chunk);
		BuildVertices (chunk);
       // m_densityTexture.Release();

	}

	/// <summary>
	/// Builds the density texture for a chunk in world space.
	/// </summary>
	/// <param name="chunk">Chunk.</param>
	void BuildDensity(Chunk chunk)
	{
		// create the density texture
        
        

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

        m_buildDensityShader.Dispatch(0, 1, 33, 33);

	}


	void BuildVertices( Chunk chunk)
	{

		m_genVerticesShader.SetTexture (0, "densityTexture", m_densityTexture);
        m_genVerticesShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        m_genVerticesShader.SetBuffer(0, "edge_connect_list", Helper.GetTriangleConnectionTable());
        m_genVerticesShader.SetBuffer(0, "triBuffer", chunk.triangleBuffer);

		float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
		m_genVerticesShader.SetFloat ("invVoxelDim", invVoxelDim);
		m_genVerticesShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
		m_genVerticesShader.SetVector("wsChunkDim", chunk.wsChunkDim);
        m_genVerticesShader.SetInt("voxelDim", chunk.voxelDim);
		int N = chunk.voxelDim;
		m_genVerticesShader.Dispatch (0, N/4, N/4, N/4);

        ComputeBuffer.CopyCount(chunk.triangleBuffer, chunk.argumentBuffer, 0);

		if (DEBUG) 
		{
			int[] args = new int[]{ 0, 1, 0, 0 };
            chunk.argumentBuffer.GetData(args);
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

    void OnApplicationQuit()
    {
        Helper.Finalize();
    }
    void OnDestroy()
    {
        //testChunk.Release();
        foreach (Chunk c in m_chunkList)
        {
            c.Release();
        }
        m_densityTexture.Release();
    }
	


}
