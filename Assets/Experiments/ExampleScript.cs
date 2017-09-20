using UnityEngine;
using System.Collections;

public class ExampleScript : MonoBehaviour
{
	public int instanceCount = 5000;
	public Mesh instanceMesh;
	public Material instanceMaterial;

	private int cachedInstanceCount = -1;
	private ComputeBuffer positionBuffer;
	private ComputeBuffer argsBuffer;
	private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

	void Start()
	{

		argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		UpdateBuffers();
	}

	void Update()
	{
		// Update starting position buffer
		if (cachedInstanceCount != instanceCount)
			UpdateBuffers();
		
		// Render
		Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
	}

	void OnGUI()
	{
		GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + instanceCount.ToString());
		instanceCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)instanceCount, 1.0f, 5000);
	}

	void UpdateBuffers()
	{
		// positions
		if (positionBuffer != null)
			positionBuffer.Release();

		positionBuffer = new ComputeBuffer(instanceCount, 16);
		Vector4[] positions = new Vector4[instanceCount];
		for (int i = 0; i < instanceCount; i++)
		{
			float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
			float distance = Random.Range(20.0f, 100.0f);
			float height = Random.Range(-2.0f, 2.0f);
			float size = Random.Range(0.05f, 0.25f);
			positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
		}
		positionBuffer.SetData(positions);
		instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

		// indirect args
		uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
		args[0] = numIndices;
		args[1] = (uint)instanceCount;
		argsBuffer.SetData(args);

		cachedInstanceCount = instanceCount;
	}

	void OnDisable()
	{

		if (positionBuffer != null)
			positionBuffer.Release();
		positionBuffer = null;

		if (argsBuffer != null)
			argsBuffer.Release();
		argsBuffer = null;
	}
}