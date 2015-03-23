using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class BuildDensityPass : BasePass {

    private Texture3D[] m_noiseVolumes;
    public BuildDensityPass()
    {
        base.LoadComputeShader("Assets/Shaders/build_density.compute");
        Debug.Log("BuildDensityPass Created!");
        m_noiseVolumes = new Texture3D[4];
        m_noiseVolumes[0] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol1.vol", TextureFormat.RGBAHalf, sizeof(UInt16) * 4, 16, 16, 16);
        m_noiseVolumes[1] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol2.vol", TextureFormat.RGBAHalf, sizeof(UInt16) * 4, 16, 16, 16);
        m_noiseVolumes[2] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol3.vol", TextureFormat.RGBAHalf, sizeof(UInt16) * 4, 16, 16, 16);
        m_noiseVolumes[3] = Helper.LoadVolumeFromFile("Assets/Textures/noiseVol4.vol", TextureFormat.RGBAHalf, sizeof(UInt16) * 4, 16, 16, 16);
    }

    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {
        
        // set all noiseTextures 
        computeShader.SetTexture(0, "NoiseVol1", m_noiseVolumes[0]);
        computeShader.SetTexture(0, "NoiseVol2", m_noiseVolumes[1]);
        computeShader.SetTexture(0, "NoiseVol3", m_noiseVolumes[2]);
        computeShader.SetTexture(0, "NoiseVol4", m_noiseVolumes[3]);

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
