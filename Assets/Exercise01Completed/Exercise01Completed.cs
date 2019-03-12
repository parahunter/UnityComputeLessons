using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exercise01Completed : MonoBehaviour
{
	[Range(0, 1f)]
	[SerializeField] float intensity = 1;
	[SerializeField] ComputeShader computeShader;

	[SerializeField] float cirleRadius = 200;
	[SerializeField] float circleCenterX = 512;
	[SerializeField] float circleCenterY = 512;


	RenderTexture texture;

	const int textureSize = 1024;
	const int kernelSize = 4;

	int kernelIndex;

	const int blockSize = 2;
	
	// Use this for initialization
	void Start()
	{
		texture = new RenderTexture(width: textureSize, height: textureSize, depth: 0);
		texture.enableRandomWrite = true;
		texture.filterMode = FilterMode.Point;
		texture.Create();

		kernelIndex = computeShader.FindKernel("CSMain");
		computeShader.SetTexture(kernelIndex: kernelIndex, name: "Result", texture: texture);
		
		GetComponent<Renderer>().material.mainTexture = texture;
	}

	void Update()
	{
		computeShader.SetFloat("intensity", intensity);
		computeShader.SetFloat("cirleRadius", cirleRadius);
		computeShader.SetFloat("circleCenterX", circleCenterX);
		computeShader.SetFloat("circleCenterY", circleCenterY);
		
		computeShader.Dispatch(kernelIndex, textureSize / (kernelSize * blockSize), textureSize / (kernelSize * blockSize), 1);
	}

	void OnDisable()
	{
		if (texture != null)
			texture.Release();
	}
}