using UnityEngine;
using System.Collections;

public class BoidMeshScript: MonoBehaviour {

    public ComputeShader shader;
    public Material mat;

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
    
    bool bDoUpdate = false;

    struct BoidData
    {
        public Vector2 position;
        public Vector2 direction;
        public Vector4 color;
    }

    ComputeBuffer boidBuffer;
    ComputeBuffer boidAltBuffer;
    uint[] ConsumeIds;
    bool isAltFrame;

    BoxCollider boxCollider;

    // Use this for initialization
    void Start () {
        boxCollider = GetComponent<BoxCollider>();

        NumBoids = (NumBoids / 10) * 10;

        boidBuffer = new ComputeBuffer(NumBoids, sizeof(float)*8, ComputeBufferType.Append);
        boidAltBuffer = new ComputeBuffer(NumBoids, sizeof(float) * 8, ComputeBufferType.Append);

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
        boidAltBuffer.Release();
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
            tempArray[i].direction = Random.insideUnitCircle * MaxSpeed;
            tempArray[i].color = new Vector4(Random.value, Random.value, Random.value, 1.0f);
        }

        boidBuffer.SetData(tempArray);
        boidBuffer.SetCounterValue((uint)NumBoids);
        isAltFrame = false;

        bDoUpdate = true;
        ComputeStepFrame();
        bDoUpdate = false;
    }


    private void ComputeStepFrame()
    {
        int kernelHandle;

        SetShaderValues();

        if (bDoUpdate)
        {

            ComputeBuffer inBuf = new ComputeBuffer(NumBoids, sizeof(uint), ComputeBufferType.Append);

            inBuf.SetData(ConsumeIds);
            inBuf.SetCounterValue((uint)NumBoids);

            kernelHandle = shader.FindKernel("CSBoidMain");

            // Do Boid Pass            
            if (isAltFrame)
            {
                boidBuffer.SetCounterValue(0);
                shader.SetBuffer(kernelHandle, "BoidBuffer", boidAltBuffer);
                shader.SetBuffer(kernelHandle, "OutBoidBuffer", boidBuffer);
            } else
            {
                boidAltBuffer.SetCounterValue(0);
                shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
                shader.SetBuffer(kernelHandle, "OutBoidBuffer", boidAltBuffer);
            }

            shader.SetBuffer(kernelHandle, "InBoidBuffer", inBuf);            
            shader.Dispatch(kernelHandle, NumBoids / 10, 1, 1);

            inBuf.Dispose();

            isAltFrame = !isAltFrame;
        }
    }

    void OnRenderObject()
    {
        mat.SetPass(0);
        mat.SetMatrix("My_Object2World", transform.localToWorldMatrix);

        if (isAltFrame)
            mat.SetBuffer("BoidBuffer", boidBuffer);
        else
            mat.SetBuffer("BoidBuffer", boidAltBuffer);

        mat.SetVector("Bounds", new Vector4(boxCollider.size.x, boxCollider.size.y, boxCollider.size.z, 0));
        mat.SetVector("InvBounds", new Vector4(
            boxCollider.size.x / TexResolution,
            boxCollider.size.y / TexResolution,
            boxCollider.size.z / TexResolution,
            1.0f));

        Graphics.DrawProcedural(MeshTopology.Triangles, 4 * 3, NumBoids);
    }


    void Update () {

        RaycastHit hit;
        Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);        
        if (Physics.Raycast(mr, out hit))
        {
            // AtrractPoint = hit.textureCoord * TexResolution;
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            bDoUpdate = !bDoUpdate;
        }

        ComputeStepFrame();

        if (Input.GetKeyUp(KeyCode.E))
            ResetComputeSim();
    }
    
}
