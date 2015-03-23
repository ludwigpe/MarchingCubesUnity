using UnityEngine;
using System.Collections;

public class AppendGenVerticesPass : BasePass {

    ComputeBuffer indexCounterBuffer; 
	public AppendGenVerticesPass()
    {
        base.LoadComputeShader("Assets/Shaders/gen_vertices_append.compute");
        indexCounterBuffer = new ComputeBuffer(1, sizeof(int));
    }

    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {
     
        int[] data = new int[1];
        data[0] = 0;
        indexCounterBuffer.SetData(data);
        computeShader.SetBuffer(0, "indexCounter", indexCounterBuffer);
        computeShader.SetTexture(0, "densityTexture", densityTexture);
        computeShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        computeShader.SetBuffer(0, "edge_connect_list", Helper.GetTriangleConnectionTable());
        computeShader.SetBuffer(0, "triangleBuffer", chunk.triangleBuffer);

        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
        computeShader.SetVector("wsChunkDim", chunk.wsChunkDim);
        computeShader.SetInt("voxelDim", chunk.voxelDim);
        int N = chunk.voxelDim;
        computeShader.Dispatch(0, N / 4, N / 4, N / 4);

        indexCounterBuffer.GetData(data);
        int numTriangles = data[0];
        chunk.triangleCount = numTriangles;


        return true;
    }

    public override void Release()
    {
 	    indexCounterBuffer.Release();
    }
}
