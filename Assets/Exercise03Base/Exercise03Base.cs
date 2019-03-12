﻿using UnityEngine;
using System.Collections;

public class Exercise03Base : MonoBehaviour
{
	public ComputeShader shader;
	public int TexResolution = 256;

	public int boidMaxCount = 500;
	public int boidStartCount = 200;
	public float deltaModifier = 22;
	public float weight = 10;

	public float separationWeight = 600;
	public float alignWeight = 200;
	public float cohesionWeight = 20;
	public float maxSpeed = 10;
	public float repelDist = 2;
	public float alignmentDist = 15;
	public float cohesionDist = 50;
	public float maxForce = 10;

	new Renderer renderer;
	RenderTexture renderTexture;
	
	struct BoidData
	{
		public Vector2 position;
		public Vector2 direction;
		public Vector4 color;
	}

	ComputeBuffer boidBuffer;
	ComputeBuffer deadIndexBuffer;
	ComputeBuffer indexBuffer0, indexBuffer1;
	ComputeBuffer countBuffer;

	bool useFirstBuffer = true;

	// Use this for initialization
	void Start()
	{
		renderTexture = new RenderTexture(TexResolution, TexResolution, 24);
		renderTexture.enableRandomWrite = true;
		renderTexture.Create();

		renderer = GetComponent<Renderer>();
		renderer.enabled = true;

		boidMaxCount = 32 + (boidMaxCount / 32) * 32;
		
		boidBuffer = new ComputeBuffer(boidMaxCount, sizeof(float) * 8, ComputeBufferType.Default);
		
		indexBuffer0 = new ComputeBuffer(boidMaxCount, sizeof(uint), ComputeBufferType.Append);
		indexBuffer1 = new ComputeBuffer(boidMaxCount, sizeof(uint), ComputeBufferType.Append);
		deadIndexBuffer = new ComputeBuffer(boidMaxCount, sizeof(uint), ComputeBufferType.Append);
		
		countBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

		ResetComputeSim();
	}

	void OnDestroy()
	{
		boidBuffer.Release();
		renderTexture.Release();
		indexBuffer0.Release();
		indexBuffer1.Release();
		countBuffer.Release();

		deadIndexBuffer.Release();
	}

	private void SetShaderValues()
	{
		float InvWeight = 1.0f / weight;
		shader.SetInt("NumBoids", boidMaxCount);
		shader.SetFloats("Params", new float[4] { separationWeight, alignWeight, cohesionWeight, maxSpeed });
		shader.SetFloats("Params2", new float[4] { repelDist, alignmentDist, cohesionDist, maxForce });
		shader.SetFloats("Params3", new float[4] { Time.deltaTime * deltaModifier, InvWeight, TexResolution, 0.0f });
		shader.SetFloats("WipeColour", new float[] { 0, 0, 0, 0 });
	}

	private void ResetComputeSim()
	{
		BoidData[] boidArray = new BoidData[boidMaxCount];
		for (int i = 0; i < boidStartCount; ++i)
		{
			boidArray[i].position = new Vector2(Random.value * TexResolution, Random.value * TexResolution);
			boidArray[i].direction = Random.insideUnitCircle;
			boidArray[i].color = new Vector4(Random.value, Random.value, Random.value, 1.0f);
		}

		boidBuffer.SetData(boidArray);

		uint[] ConsumeIds = new uint[boidMaxCount];
		for (uint i = 0; i < boidMaxCount; i++)
			ConsumeIds[i] = i;
		
		indexBuffer0.SetCounterValue(0);
		indexBuffer1.SetCounterValue(0);
		
		deadIndexBuffer.SetData(ConsumeIds);
		deadIndexBuffer.SetCounterValue((uint)(boidMaxCount));
		
		useFirstBuffer = true;

		SetShaderValues();
	}
	
	private void ComputeStepFrame()
	{
		SetShaderValues();

		// Clear Texture
		int kernelHandle = shader.FindKernel("RenderBackground");
		shader.SetTexture(kernelHandle, "Result", renderTexture);
		shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);
		
		int[] values = new int[4];
		ComputeBuffer.CopyCount(indexBuffer0, countBuffer, 0);
		countBuffer.GetData(values);
		int currentBoidCount = values[0];
		
		shader.SetInt("NumBoids", currentBoidCount);
		
		// Do Boid Pass
		kernelHandle = shader.FindKernel("SimulateBoids");
		shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
		shader.SetBuffer(kernelHandle, "IndexBuffer", indexBuffer0);
		shader.SetTexture(kernelHandle, "Result", renderTexture);
		shader.Dispatch(kernelHandle, 1 + ((boidMaxCount - 32) / 32), 1, 1);

		// Set Material
		renderer.material.SetTexture("_MainTex", renderTexture);

		useFirstBuffer = !useFirstBuffer;
	}
		
	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			RaycastHit hit;
			Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(mr, out hit))
			{
				int kernelIndex = shader.FindKernel("AddBoids");
				
				shader.SetBuffer(kernelIndex, "ConsumeIndexBuffer", deadIndexBuffer);
				shader.SetBuffer(kernelIndex, "AppendIndexBuffer", indexBuffer0);
				shader.SetBuffer(kernelIndex, "BoidBuffer", boidBuffer);

				Vector2 texCoord = new Vector2(1, 1) - hit.textureCoord;
				Vector4 spawnPoint = texCoord * TexResolution;
				shader.SetVector("SpawnPoint", spawnPoint);

				Vector4 spawnDirection = Random.insideUnitCircle;
				shader.SetVector("SpawnDirection", spawnDirection);

				Vector4 spawnColor = new Vector4(Random.value, Random.value, Random.value, 1.0f);
				shader.SetVector("SpawnColor", spawnColor);

				shader.Dispatch(kernelIndex, 1, 1, 1);
			}
		}

		ComputeStepFrame();

		if (Input.GetKeyUp(KeyCode.R))
			ResetComputeSim();
	}
}
