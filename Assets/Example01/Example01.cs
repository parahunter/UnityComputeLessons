using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example01 : MonoBehaviour
{
	[Range(0, 1f)]
	[SerializeField] float intensity = 1;
	[SerializeField] ComputeShader computeShader;

	RenderTexture texture;

	const int textureSize = 512;
	const int kernelSize = 8;

	int kernelIndex;

	// Use this for initialization
	void Start ()
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
		computeShader.Dispatch(kernelIndex, textureSize / kernelSize, textureSize / kernelSize, 1);
	}

	void OnDisable()
	{
		if (texture != null)
			texture.Release();
	}
}
