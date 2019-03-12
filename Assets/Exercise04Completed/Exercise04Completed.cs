using UnityEngine;
using System.Collections;

public class Exercise04Completed : MonoBehaviour
{
	public ComputeShader shader;

	RenderTexture outputTex;

	const int blockWidth = 40;
	const int blockHeight = 25;

	const int cellWidth = 8;
	const int cellHeight = 8;

	public int mode = 0;
	const int num_stages = 4;
	string[] kernalStage = new string[num_stages]{
		"HiRes_OnlyRes",
		"HiRes_ResCol",
		"HiRes",
		"MultiColor"
	};

	bool hasCreatedResources = false;

	// Use this for initialization
	void CreateResources()
	{
		outputTex = new RenderTexture(blockWidth * cellWidth, blockHeight * cellHeight, 0);
		outputTex.enableRandomWrite = true;
		outputTex.filterMode = FilterMode.Point;

		outputTex.Create();
		hasCreatedResources = true;
	}

	void OnDestroy()
	{
		outputTex.Release();
		hasCreatedResources = false;
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!hasCreatedResources)
			CreateResources();

		int kernelHandle = shader.FindKernel(kernalStage[mode]);
		shader.SetTexture(kernelHandle, "SourceTex", source);
		shader.SetTexture(kernelHandle, "Result", outputTex);
		shader.Dispatch(kernelHandle, blockWidth, blockHeight, 1);

		//A bit of a waste but only way Unity allows us to 
		Graphics.Blit(outputTex, destination);
	}

}
