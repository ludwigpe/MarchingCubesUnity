using UnityEngine;
using System.Collections;

public class ChunkCreator : MonoBehaviour {
    public ComputeShader m_densityShader;
    public ComputeShader m_buildVerticesShader;
    public Material m_chunkMat;
    private RenderTexture m_densityTexture;
    private Texture3D[] m_noiseVols = new Texture3D[4];
    private Chunk m_chunk;
    private ComputeBuffer m_vertexBuffer, m_argBuff;
    private ComputeBuffer m_triangleBuffer;


    const int SIZE = 32 * 32 * 32 * 5 * 3;
	// Use this for initialization
	void Start () {
        m_noiseVols[0] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol1.vol");
        m_noiseVols[1] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol2.vol");
        m_noiseVols[2] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol3.vol");
        m_noiseVols[3] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol4.vol");

        Chunk m_chunk = new Chunk(new Vector3(0, 0, 0), new Vector3(10, 10, 10), 32);

        m_densityTexture = Helper.CreateDensityTexture(m_chunk.voxelDim);

        int kernelIndex = m_densityShader.FindKernel("ComputeDens1");
        SetDensityShaderParameters(kernelIndex);
        // dispatch first set of thread groups that wi
        m_densityShader.Dispatch(kernelIndex, 1, 1, 33);

        kernelIndex = m_densityShader.FindKernel("ComputeDens2");
        SetDensityShaderParameters(kernelIndex);
        m_densityShader.Dispatch(kernelIndex, 1, 1, 33);
        int maxTriangleCount = m_chunk.voxelDim * m_chunk.voxelDim * m_chunk.voxelDim * 5;
        int vertexSize = (sizeof(float) * 3 + sizeof(float) * 3);
        int triangleSize = 3 * vertexSize ;
        
        m_vertexBuffer = new ComputeBuffer(SIZE, vertexSize, ComputeBufferType.Append);
        m_triangleBuffer = new ComputeBuffer(maxTriangleCount, triangleSize, ComputeBufferType.Append);
        SetVerticesShaderParameter(0);
        m_buildVerticesShader.Dispatch(0, 16, 16, 16);

        m_argBuff = Helper.GetArgumentBuffer();
        //m_argBuff = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);
        ComputeBuffer.CopyCount(m_triangleBuffer, m_argBuff, 0);

        int[] args = new int[] { 0, 1, 0, 0 };
        m_argBuff.GetData(args);
        Debug.Log("vertex count " + args[0]);
        Debug.Log("instance count " + args[1]);
        Debug.Log("start vertex " + args[2]);
        Debug.Log("start instance " + args[3]);

        Vector3[] data = new Vector3[500];
        m_vertexBuffer.GetData(data);

        for(int i = 0; i < 30*4; i++)
        {
            Vector3 v = data[i];
            print( v);

        }


        
	}
    void OnPostRender()
    {
        m_chunkMat.SetPass(0);
        m_chunkMat.SetBuffer("triangleBuffer", m_triangleBuffer);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, m_argBuff, 0);
        //Graphics.DrawProcedural(MeshTopology.Triangles, SIZE);
    }

    void SetDensityShaderParameters(int kernelIndex)
    {
        m_densityShader.SetTexture(kernelIndex, "densityTexture", m_densityTexture);
        m_densityShader.SetTexture(kernelIndex, "NoiseVol1", m_noiseVols[0]);
        m_densityShader.SetTexture(kernelIndex, "NoiseVol2", m_noiseVols[1]);
        m_densityShader.SetTexture(kernelIndex, "NoiseVol3", m_noiseVols[2]);
        m_densityShader.SetTexture(kernelIndex, "NoiseVol4", m_noiseVols[3]);

        float invVoxelDim = 1.0f / m_chunk.voxelDim;
        m_densityShader.SetFloat("invVoxelDim", invVoxelDim);
        m_densityShader.SetVector("wsChunkPosLL", m_chunk.wsPosLL);
        m_densityShader.SetVector("wsChunkDim", m_chunk.wsChunkDim);
    }

    void SetVerticesShaderParameter(int kernelIndex)
    {
        float invVoxelDim = 1.0f / m_chunk.voxelDim;

        m_buildVerticesShader.SetTexture(kernelIndex, "densityTexture", m_densityTexture);
        m_buildVerticesShader.SetBuffer(kernelIndex, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        m_buildVerticesShader.SetBuffer(kernelIndex, "edge_connect_list", Helper.GetTriangleConnectionTable());
        m_buildVerticesShader.SetBuffer(kernelIndex, "vBuffer", m_vertexBuffer);
        m_buildVerticesShader.SetBuffer(kernelIndex, "triBuffer", m_triangleBuffer);
        m_buildVerticesShader.SetFloat("invVoxelDim", invVoxelDim);
        m_buildVerticesShader.SetVector("wsChunkPosLL", m_chunk.wsPosLL);
        m_buildVerticesShader.SetVector("wsChunkDim", m_chunk.wsChunkDim);
        
    }
	// Update is called once per frame
	void Update () {

	}

    void OnDestroy()
    {
        m_vertexBuffer.Release();
        m_densityTexture.Release();

    }

}
