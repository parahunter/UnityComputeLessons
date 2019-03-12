using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exercise02Base : MonoBehaviour
{
	public ComputeShader computeShader;
	public int textureResolution = 256;
	public int particleAmount = 200;
	public float attractionRadius = 50;
	public float attractionForce = 100;

	Vector4 attractionPoint;
	
	new Renderer renderer;
	RenderTexture renderTexture;
	
	struct Particle
	{
		public Vector2 position;
		public Vector2 direction;
		public Vector3 color;
		public float aliveTime;
	}

	const int positionOffset = 10;
	const int directionMaxAmplitude = 50;

	ComputeBuffer particleBuffer;

	// Use this for initialization
	void Start()
	{
		renderTexture = new RenderTexture(textureResolution, textureResolution, 24);
		renderTexture.enableRandomWrite = true;
		renderTexture.filterMode = FilterMode.Point;
		renderTexture.Create();

		// Round particles UP to nearest number
		if ((particleAmount % 256) > 0)
		{
			particleAmount += 256 - (particleAmount % 256);
		}

		particleBuffer = new ComputeBuffer(particleAmount, sizeof(float) * 8, ComputeBufferType.Default);

		renderer = GetComponent<Renderer>();
		renderer.enabled = true;

		ResetComputeSim();
	}

	void OnDestroy()
	{
		renderTexture.Release();
		particleBuffer.Release();
	}

	private void ResetComputeSim()
	{
		Particle[] pArray = new Particle[particleAmount];
		
		for (int i = 0; i < particleAmount; i++)
		{
			Particle p = new Particle();
			p.position = new Vector2(Random.Range(positionOffset, textureResolution - positionOffset), Random.Range(positionOffset, textureResolution - positionOffset));
			p.direction = new Vector2(Random.Range(-directionMaxAmplitude, directionMaxAmplitude), Random.Range(-directionMaxAmplitude, directionMaxAmplitude));
			Color c = Random.ColorHSV(0, 1.0f, 0.5f, 1.0f, 0.5f, 1.0f);
			p.color = new Vector4(c.r, c.g, c.b, 0.0f);
			pArray[i] = p;
		}

		particleBuffer.SetData(pArray);
		ComputeStepFrame();
	}
	
	private void ComputeStepFrame()
	{
		//NOTE in production code you should only set uniform values when they change
		computeShader.SetInt("TexSize", textureResolution - 1);
		computeShader.SetFloat("DeltaTime", Time.deltaTime);
		computeShader.SetVector("AttractionPoint", attractionPoint);

		int kernelHandle = computeShader.FindKernel("RenderBackground");
		computeShader.SetTexture(kernelHandle, "Result", renderTexture);
		computeShader.Dispatch(kernelHandle, textureResolution / 8, textureResolution / 8, 1);

		kernelHandle = computeShader.FindKernel("SimulateParticles");
		computeShader.SetTexture(kernelHandle, "Result", renderTexture);
		computeShader.SetBuffer(kernelHandle, "PartBuffer", particleBuffer);
		computeShader.Dispatch(kernelHandle, particleAmount / 256, 1, 1);

		renderer.material.SetTexture("_MainTex", renderTexture);
	}


	void Update()
	{
		attractionPoint = Vector4.zero;

		if (Input.GetMouseButton(0))
		{
			RaycastHit hit;
			Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(mr, out hit))
			{
				attractionPoint = hit.textureCoord * textureResolution;

				//Use extra components to send this data over
				attractionPoint.z = attractionRadius;
				attractionPoint.w = attractionForce;
			}
		}
		
		ComputeStepFrame();
	}
}
