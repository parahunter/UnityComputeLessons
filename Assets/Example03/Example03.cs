using UnityEngine;
using System.Collections;

public class Example03 : MonoBehaviour
{
	public ComputeShader shader;
	public int TexResolution = 256;

	public int boidCount = 100;
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
	ComputeBuffer indexBuffer0, indexBuffer1;
	
	bool useFirstBuffer = true;

	// Use this for initialization
	void Start()
	{
		renderTexture = new RenderTexture(TexResolution, TexResolution, 24);
		renderTexture.enableRandomWrite = true;
		renderTexture.Create();

		renderer = GetComponent<Renderer>();
		renderer.enabled = true;

		boidCount = (boidCount / 32) * 32;

		boidBuffer = new ComputeBuffer(boidCount, sizeof(float) * 8, ComputeBufferType.Default);

		BoidData zeroBoid;
		zeroBoid.position = Vector2.zero;
		zeroBoid.direction = Vector2.zero;
		zeroBoid.color = Vector4.zero;

		uint[] ConsumeIds = new uint[boidCount];
		for (uint i = 0; i < boidCount; i++)
			ConsumeIds[i] = i;

		indexBuffer0 = new ComputeBuffer(boidCount, sizeof(uint), ComputeBufferType.Append);
		indexBuffer0.SetData(ConsumeIds);
		indexBuffer0.SetCounterValue((uint)boidCount);

		indexBuffer1 = new ComputeBuffer(boidCount, sizeof(uint), ComputeBufferType.Append);

		ResetComputeSim();
	}


	void OnDestroy()
	{
		boidBuffer.Release();
		renderTexture.Release();
	}


	private void SetShaderValues()
	{
		float InvWeight = 1.0f / weight;
		shader.SetInt("NumBoids", boidCount);
		shader.SetFloats("Params", new float[4] { separationWeight, alignWeight, cohesionWeight, maxSpeed });
		shader.SetFloats("Params2", new float[4] { repelDist, alignmentDist, cohesionDist, maxForce });
		shader.SetFloats("Params3", new float[4] { Time.deltaTime * deltaModifier, InvWeight, TexResolution, 0.0f });
		shader.SetFloats("WipeColour", new float[] { 0, 0, 0, 0 });
	}

	private void ResetComputeSim()
	{
		BoidData[] boidArray = new BoidData[boidCount];
		for (int i = 0; i < boidCount; ++i)
		{
			boidArray[i].position = new Vector2(Random.value * TexResolution, Random.value * TexResolution);
			boidArray[i].direction = Random.insideUnitCircle;
			boidArray[i].color = new Vector4(Random.value, Random.value, Random.value, 1.0f);
		}

		boidBuffer.SetData(boidArray);

		SetShaderValues();
	}
	
	private void ComputeStepFrame()
	{
		SetShaderValues();

		//inBuf.SetData(ConsumeIds);
		ComputeBuffer consumeBuffer = useFirstBuffer ? indexBuffer0 : indexBuffer1;
		ComputeBuffer appendBuffer  = !useFirstBuffer ? indexBuffer0 : indexBuffer1;
		
		// Clear Texture
		int kernelHandle = shader.FindKernel("RenderBackground");
		shader.SetTexture(kernelHandle, "Result", renderTexture);
		shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

		//// Render Boids
		//kernelHandle = shader.FindKernel("RenderMain");
		//shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
		//shader.SetTexture(kernelHandle, "Result", renderTexture);
		//shader.Dispatch(kernelHandle, boidCount / 32, 1, 1);

		// Do Boid Pass
		kernelHandle = shader.FindKernel("BoidMain");
		shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
		shader.SetBuffer(kernelHandle, "ConsumeIndexBuffer", consumeBuffer);
		shader.SetBuffer(kernelHandle, "AppendIndexBuffer", appendBuffer);
		shader.SetTexture(kernelHandle, "Result", renderTexture);
		shader.Dispatch(kernelHandle, boidCount / 32, 1, 1);

		// Set Material
		renderer.material.SetTexture("_MainTex", renderTexture);

		useFirstBuffer = !useFirstBuffer;
	}
		
	void Update()
	{
		RaycastHit hit;
		Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(mr, out hit))
		{
			// AtrractPoint = hit.textureCoord * TexResolution;
		}

		ComputeStepFrame();

		if (Input.GetKeyUp(KeyCode.R))
			ResetComputeSim();
	}
}
