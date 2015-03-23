using UnityEngine;
using System.Collections;

public class ListNonemptyCellsPass : BasePass {

    public ComputeBuffer indexCounterBuffer;
    public ListNonemptyCellsPass()
    {
        base.LoadComputeShader("Assets/Shaders/list_nonempty_voxels.compute");
        indexCounterBuffer = new ComputeBuffer(1, sizeof(int));
    }

    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {
        int[] data = new int[1];
        data[0] = 0;

        indexCounterBuffer.SetData(data);

        computeShader.SetBuffer(0, "indexCounter", indexCounterBuffer);
        computeShader.SetTexture(0, "densityTexture", densityTexture);
        computeShader.SetBuffer(0, "nonemptyList", chunk.nonemptyListBuffer);

        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.Dispatch(0, 1, 33, 33);
        
        
        indexCounterBuffer.GetData(data);
        int numNonemptyCells = data[0];
        chunk.numNonemptyCells = numNonemptyCells;
        if (numNonemptyCells == 0)
        {
            chunk.isEmpty = true;
            return false;
        }

        return true;
    }
    public override void Release()
    {
        indexCounterBuffer.Release();
    }
}
