using UnityEngine;
using System.Collections;

public class BuildDensityMedicalPass : BasePass {
    private Texture3D m_densityVolume;
	public BuildDensityMedicalPass()
    {
        base.LoadComputeShader("Assets/Shaders/build_density_medical.compute");
        m_densityVolume = Helper.LoadVolumeFromFile("Assets/Textures/mri_ventricles.raw", TextureFormat.Alpha8, sizeof(byte), 256, 256, 128);
    }

    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {

        // set medical dataset
        computeShader.SetTexture(0, "dataSet", m_densityVolume);
        // set the density texture where the comp shader will write to
        computeShader.SetTexture(0, "densityTexture", densityTexture);

        // set extra values for computation
        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
        computeShader.SetVector("wsChunkDim", chunk.wsChunkDim);

        computeShader.Dispatch(0, 1, 33, 33);
        return true;
    }
}
