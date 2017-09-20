using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exercise01 : MonoBehaviour
{
	[SerializeField] ComputeShader computeShader;

	RenderTexture texture;

	const int textureSize = 512;
	const int kernelSize = 8;

	// Use this for initialization
	void Start()
	{
		texture = new RenderTexture(width: textureSize, height: textureSize, depth: 0);
		texture.enableRandomWrite = true;
		texture.filterMode = FilterMode.Point;
		texture.Create();

		int kernelIndex = computeShader.FindKernel("CSMain");

		computeShader.SetTexture(kernelIndex: kernelIndex, name: "Result", texture: texture);

		computeShader.Dispatch(kernelIndex, textureSize / kernelSize, textureSize / kernelSize, 1);

		GetComponent<Renderer>().material.mainTexture = texture;
	}

	void OnDisable()
	{
		if (texture != null)
			texture.Release();
	}
}
