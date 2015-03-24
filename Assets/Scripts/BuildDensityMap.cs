using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class BuildDensityMap : MonoBehaviour
{
    public enum Passes { NAIVE, APPENDING, NONEMPTY, INDICES, INDICES_MEDICAL, NONEMPTY_MEDICAL};
    public bool DEBUG = true;
    public Passes m_chosenPass;
    const int SIZE = 32 * 32 * 32 * 5 * 3;
	public Material m_chunkMat;
    public Material m_chunkMeshMat;
    public Vector3 m_numberOfChunks = new Vector3(1, 1, 1);
    public int m_chunkDim;

    private Texture3D m_medicalVol;
    private RenderTexture m_densityTexture;

    private List<Chunk> m_chunkList;
    private BasePass[] m_passes;
    
	// Use this for initialization
	void Start () 
    {
      
        int numChunksTotal = (int)(m_numberOfChunks.x * m_numberOfChunks.y * m_numberOfChunks.z);
        m_chunkList = new List<Chunk>(numChunksTotal);
        switch(m_chosenPass)
        {
            case Passes.NAIVE:
                m_passes = new BasePass[2];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new NaiveGenVerticesPass();
                break;
            case Passes.APPENDING:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new AppendGenVerticesPass();
                break;
            case Passes.NONEMPTY:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new NonemptyGenVerticesPass();
                break;
            case Passes.NONEMPTY_MEDICAL:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityMedicalPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new AppendGenVerticesPass();
                break;
            case Passes.INDICES:
                m_passes = new BasePass[4];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new SplatGenVerticesPass();
                m_passes[3] = new GenIndicesPass();
                break;
            case Passes.INDICES_MEDICAL:
                m_passes = new BasePass[4];
                m_passes[0] = new BuildDensityMedicalPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new SplatGenVerticesPass();
                m_passes[3] = new GenIndicesPass();
                break;
        }

        m_densityTexture = Helper.CreateDensityTexture(32);

        float startTime = Time.realtimeSinceStartup;
        CreateChunks();
        //if ((m_chosenPass == Passes.APPENDING || m_chosenPass == Passes.NONEMPTY) && m_chunkList.Count > 1)
        //{
        //    int prev = m_chunkList[0].triangleCount;
        //    for (int i = 1; i < m_chunkList.Count; i++)
        //    {
        //        int temp = m_chunkList[i].triangleCount;
        //        m_chunkList[i].triangleCount = prev;
        //        prev = temp;
        //    }
        //    m_chunkList[0].triangleCount = prev;
        //}
        float endTime = Time.realtimeSinceStartup;
        print("Creation time: " + (float)(endTime - startTime));    
        if (DEBUG)
        {
            print("Number of chunks generated: " + m_chunkList.Count);
        }
        
	
	}

	void OnPostRender ()
	{
        
        //foreach (Chunk c in m_chunkList)
        //{

        //    c.Draw();
        //}
           
	}

    void CreateChunks ()
    {
        for(int z = 0; z < m_numberOfChunks.z; z++)
            for(int y = 0; y < m_numberOfChunks.y; y++)
                for(int x = 0; x < m_numberOfChunks.x; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    Vector3 chunkDim = new Vector3(m_chunkDim, m_chunkDim, m_chunkDim);
                    pos *= m_chunkDim;
                    Chunk c = new Chunk(pos, chunkDim, 32);
                    c.chunkMatProcedural = m_chunkMat;
                    c.chunkMatMesh = m_chunkMeshMat;
                    if (BuildChunk(ref c))
                        m_chunkList.Add(c);
                    else
                        c.Release();
                    
                }
    }
	/// <summary>
	/// Builds the chunk given a chunk object and a vertex buffer to put the result in.
	/// </summary>
	/// <param name="vertexBuffer">Vertex buffer.</param>
	/// <param name="chunk">Chunk.</param>
	bool BuildChunk(ref Chunk chunk)
	{

        foreach (BasePass pass in m_passes)
        {
            if (!(pass.DoPass(ref chunk, ref m_densityTexture)))
                return false;
        }
        if (m_chosenPass == Passes.INDICES || m_chosenPass == Passes.INDICES_MEDICAL)
            chunk.GenerateChunkIndexed();
        else
            chunk.GenerateChunkObject();
        return true ;

	}

	/// <summary>
	/// Builds the density texture for a chunk in world space.
	/// </summary>
	/// <param name="chunk">Chunk.</param>

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
        foreach (BasePass pass in m_passes)
            pass.Release();
        foreach (Chunk c in m_chunkList)
        {
            c.Release();
        }
        if(m_densityTexture != null)
            m_densityTexture.Release();
    }
    void OnDestroy()
    {
        foreach (BasePass pass in m_passes)
            pass.Release();
        foreach (Chunk c in m_chunkList)
        {
            c.Release();
        }
        if (m_densityTexture != null)
            m_densityTexture.Release();
    }
	


}
