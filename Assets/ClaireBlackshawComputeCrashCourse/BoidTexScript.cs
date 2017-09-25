using UnityEngine;
using System.Collections;

public class BoidTexScript: MonoBehaviour {

    public ComputeShader shader;
    public int TexResolution = 256;

    public int NumBoids = 100;

    public float DeltaMod = 1.0f;
    public float Weight = 10.0f;

    public float RepelWeight = 1.0f;
    public float AlignWeight = 1.0f;
    public float CohesionWeight = 1.0f;
    public float MaxSpeed = 1.0f;
    public float RepelDist = 1.0f;
    public float AlignDist = 1.0f;
    public float CohesionDist = 1.0f;
    public float MaxForce = 1.0f;
    
    Renderer rend;
    RenderTexture myRt;
    bool bDoUpdate = false;

    struct BoidData
    {
        public Vector2 position;
        public Vector2 direction;
        public Vector4 color;
    }

    ComputeBuffer boidBuffer;
    uint[] ConsumeIds;

    // Use this for initialization
    void Start () {
        myRt = new RenderTexture(TexResolution, TexResolution, 24);
        myRt.enableRandomWrite = true;
        myRt.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        NumBoids = (NumBoids / 10) * 10;

        boidBuffer = new ComputeBuffer(NumBoids, sizeof(float)*8, ComputeBufferType.Append);

        BoidData zeroBoid;
        zeroBoid.position = Vector2.zero;
        zeroBoid.direction = Vector2.zero;
        zeroBoid.color = Vector4.zero;

        ConsumeIds = new uint[NumBoids];
        for (uint i = 0; i < NumBoids; i++)
            ConsumeIds[i] = i;
        
        ResetComputeSim();
    }


    void OnDestroy()
    {
        boidBuffer.Release();
        myRt.Release();
    }


    private void SetShaderValues()
    {
        float InvWeight = 1.0f / Weight;
        shader.SetInt("NumBoids", NumBoids);
        shader.SetFloats("Params", new float[4] { RepelWeight, AlignWeight, CohesionWeight, MaxSpeed });
        shader.SetFloats("Params2", new float[4] { RepelDist, AlignDist, CohesionDist, MaxForce });
        shader.SetFloats("Params3", new float[4] { Time.deltaTime * DeltaMod, InvWeight, TexResolution, 0.0f });
        shader.SetFloats("WipeColour", new float[] { 0, 0, 0, 0 });
    }

    private void ResetComputeSim()
    {
        BoidData[] tempArray = new BoidData[NumBoids];
        for(int i=0; i < NumBoids; ++i)
        {
            tempArray[i].position = new Vector2(Random.value * TexResolution, Random.value * TexResolution);
            tempArray[i].direction = Random.insideUnitCircle;
            tempArray[i].color = new Vector4(Random.value, Random.value, Random.value, 1.0f);
        }

        boidBuffer.SetData(tempArray);
        boidBuffer.SetCounterValue((uint)NumBoids);


        /* RENDER */
        int kernelHandle;
        SetShaderValues();

        kernelHandle = shader.FindKernel("CSRenderWipe");
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        // Render Boids
        kernelHandle = shader.FindKernel("CSRenderMain");
        shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, NumBoids / 8, 1, 1);

        rend.material.SetTexture("_MainTex", myRt);
    }


    private void ComputeStepFrame()
    {
        int kernelHandle;

        SetShaderValues();

        if (bDoUpdate)
        {

            ComputeBuffer inBuf = new ComputeBuffer(NumBoids, sizeof(uint), ComputeBufferType.Append);
            ComputeBuffer outBuf = new ComputeBuffer(NumBoids, sizeof(float) * 8, ComputeBufferType.Append);
            inBuf.SetData(ConsumeIds);
            inBuf.SetCounterValue((uint)NumBoids);
            outBuf.SetCounterValue(0);

            // Do Boid Pass
            kernelHandle = shader.FindKernel("CSBoidMain");
            shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
            shader.SetBuffer(kernelHandle, "InBoidBuffer", inBuf);
            shader.SetBuffer(kernelHandle, "OutBoidBuffer", outBuf);
            shader.Dispatch(kernelHandle, NumBoids / 10, 1, 1);

            boidBuffer.Dispose();
            boidBuffer = outBuf;
            inBuf.Dispose();
        }

        // Clear Texture
        kernelHandle = shader.FindKernel("CSRenderWipe");
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        // Render Boids
        kernelHandle = shader.FindKernel("CSRenderMain");
        shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, NumBoids / 8, 1, 1);

        // Set Material
        rend.material.SetTexture("_MainTex", myRt);

    }


    void Update () {

        RaycastHit hit;
        Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);        
        if (Physics.Raycast(mr, out hit))
        {
            // AtrractPoint = hit.textureCoord * TexResolution;
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            bDoUpdate = !bDoUpdate;
        }

        ComputeStepFrame();

        if (Input.GetKeyUp(KeyCode.W))
            ResetComputeSim();
    }
    
}
