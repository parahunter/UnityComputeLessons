using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exercise01Base : MonoBehaviour
{
	[Range(0, 1f)]
	[SerializeField] float intensity = 1;
	[SerializeField] ComputeShader computeShader;

	RenderTexture texture;

	const int textureSize = 512;
	const int kernelSize = 8;

	int kernelHandle;

	//Initialization of resources needed for the compute shader to run
	void Start ()
	{
		texture = new RenderTexture(width: textureSize, height: textureSize, depth: 0);
		texture.enableRandomWrite = true;
		texture.filterMode = FilterMode.Point;
		texture.Create();

		kernelHandle = computeShader.FindKernel("CSMain");

		computeShader.SetTexture(kernelIndex: kernelHandle, name: "Result", texture: texture);
		
		GetComponent<Renderer>().material.mainTexture = texture;
    }

	void Update()
	{
		//Ideally in this case you would only dispatch the compute shader 
		computeShader.SetFloat("intensity", intensity);
		computeShader.Dispatch(kernelHandle, textureSize / kernelSize, textureSize / kernelSize, 1);
	}

	void OnDisable()
	{
		if (texture != null)
			texture.Release();
	}
}
