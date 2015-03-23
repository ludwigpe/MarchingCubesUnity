using UnityEngine;
using System.Collections;

public class NonemptyGenVerticesPass : BasePass {

    ComputeBuffer indexCounterBuffer;
    ComputeBuffer nonemptyLeft; 
    public NonemptyGenVerticesPass()
    {
        base.LoadComputeShader("Assets/Shaders/gen_vertices_from_nonemptylist.compute");
        indexCounterBuffer = new ComputeBuffer(1, sizeof(int));
        nonemptyLeft = new ComputeBuffer(1, sizeof(int));
    }

    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {
        int[] data = new int[1];
        data[0] = 0;
        indexCounterBuffer.SetData(data);

        int[] lastIndex = new int[1];
        lastIndex[0] = chunk.numNonemptyCells - 1;  // set last index of nonemptyList
        nonemptyLeft.SetData(lastIndex);

        computeShader.SetBuffer(0, "triangleBuffer", chunk.triangleBuffer);
        computeShader.SetBuffer(0, "nonemptyList", chunk.nonemptyListBuffer);
        computeShader.SetBuffer(0, "nonemptyLeft", nonemptyLeft);
        computeShader.SetBuffer(0, "indexCounter", indexCounterBuffer);

        computeShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        computeShader.SetBuffer(0, "edge_connect_list", Helper.GetTriangleConnectionTable());

        computeShader.SetTexture(0, "densityTexture", densityTexture);

        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
        computeShader.SetVector("wsChunkDim", chunk.wsChunkDim);

        float numTG = Mathf.Sqrt(chunk.numNonemptyCells / 64.0f);
        int numTGInt = Mathf.RoundToInt(numTG);
        int extra = Mathf.CeilToInt(numTG - numTGInt);

        computeShader.Dispatch(0, numTGInt + extra, numTGInt, 1);

        indexCounterBuffer.GetData(data);
        chunk.triangleCount = data[0];

        return true;
    }
    public override void Release()
    {
        indexCounterBuffer.Release();
        nonemptyLeft.Release();
    }

}
