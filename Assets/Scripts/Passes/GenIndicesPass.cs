using UnityEngine;
using System.Collections;

public class GenIndicesPass : BasePass {
    ComputeBuffer indexCounterBuffer;
    ComputeBuffer nonemptyLeft; 
    public GenIndicesPass()
    {
        base.LoadComputeShader("Assets/Shaders/gen_indices.compute");
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

        computeShader.SetTexture(0, "VertexIDVol", chunk.VertexIDVol);
        computeShader.SetBuffer(0, "nonemptyList", chunk.nonemptyListBuffer);
        computeShader.SetBuffer(0, "nonemptyLeft", nonemptyLeft);
        computeShader.SetBuffer(0, "indexBuffer", chunk.indexBuffer);
        computeShader.SetBuffer(0, "indexCounter", indexCounterBuffer);
        
        computeShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        computeShader.SetBuffer(0, "edge_connect_list", Helper.GetTriangleConnectionTable());


        float numTG = Mathf.Sqrt(chunk.numNonemptyCells / 64.0f);
        int numTGInt = Mathf.RoundToInt(numTG);
        int extra = Mathf.CeilToInt(numTG - numTGInt);

        computeShader.Dispatch(0, numTGInt + extra, numTGInt, 1);

        indexCounterBuffer.GetData(data);
        chunk.indexCount = data[0];
       // Debug.Log("IndexCount: " + chunk.indexCount);
        //int[] indices = new int[chunk.indexCount];
        //chunk.indexBuffer.GetData(indices);
        //foreach(int i in indices)
        //{
        //    Debug.Log(i);
        //}
        return true;
    }

    public override void Release()
    {
        indexCounterBuffer.Release();
        nonemptyLeft.Release();
    }
}
