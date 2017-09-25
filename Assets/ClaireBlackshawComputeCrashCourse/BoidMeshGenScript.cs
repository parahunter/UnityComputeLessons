using UnityEngine;
using System.Collections;

public class BoidMeshGenScript: MonoBehaviour {

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
    
    bool bDoUpdate = false;

    struct BoidData
    {
        public Vector2 position;
        public Vector2 direction;
        public Vector4 color;
    }

    ComputeBuffer boidBuffer;
    ComputeBuffer boidAltBuffer;
    ComputeBuffer vertBuffer;
    ComputeBuffer colBuffer;
    ComputeBuffer indexBuffer;
    uint[] ConsumeIds;
    bool isAltFrame;

    MeshFilter mf;

    // Use this for initialization
    void Start () {
        mf = GetComponent<MeshFilter>();

        NumBoids = (NumBoids / 10) * 10;

        boidBuffer = new ComputeBuffer(NumBoids, sizeof(float)*8, ComputeBufferType.Append);
        boidAltBuffer = new ComputeBuffer(NumBoids, sizeof(float) * 8, ComputeBufferType.Append);

        vertBuffer = new ComputeBuffer(NumBoids * 4, sizeof(float) * 3, ComputeBufferType.Default);
        colBuffer = new ComputeBuffer(NumBoids * 4, sizeof(float) * 4, ComputeBufferType.Default);
        indexBuffer = new ComputeBuffer(NumBoids * 12, sizeof(int), ComputeBufferType.Default);

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
        vertBuffer.Release();
        colBuffer.Release();
        indexBuffer.Release(); 
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

            if(isAltFrame)
                boidBuffer.SetCounterValue(0); 
            else
                boidAltBuffer.SetCounterValue(0);

            // Do Boid Pass
            kernelHandle = shader.FindKernel("CSBoidMain");
            if (isAltFrame)
            {
                shader.SetBuffer(kernelHandle, "BoidBuffer", boidAltBuffer);
                shader.SetBuffer(kernelHandle, "OutBoidBuffer", boidBuffer);
            } else
            {
                shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);
                shader.SetBuffer(kernelHandle, "OutBoidBuffer", boidAltBuffer);
            }

            shader.SetBuffer(kernelHandle, "InBoidBuffer", inBuf);            
            shader.Dispatch(kernelHandle, NumBoids / 10, 1, 1);

            inBuf.Dispose();

            // Build Mesh
            kernelHandle = shader.FindKernel("CSGenerateMesh");
            if (isAltFrame)
                shader.SetBuffer(kernelHandle, "BoidBuffer", boidAltBuffer);
            else
                shader.SetBuffer(kernelHandle, "BoidBuffer", boidBuffer);

            shader.SetBuffer(kernelHandle, "BoidMeshVert", vertBuffer);
            shader.SetBuffer(kernelHandle, "BoidMeshCol", colBuffer);
            shader.SetBuffer(kernelHandle, "BoidMeshIndexes", indexBuffer);
            shader.Dispatch(kernelHandle, NumBoids / 10, 1, 1);

            // Not the best and you would want a platform dive
            // To check how to avoid copying this off GPU memory
            Vector3[] vData = new Vector3[NumBoids * 4];
            vertBuffer.GetData(vData);
            mf.mesh.vertices = vData;

            Color[] cData = new Color[NumBoids * 4];
            colBuffer.GetData(cData);
            mf.mesh.colors = cData;

            int[] iData = new int[NumBoids * 12];
            indexBuffer.GetData(iData);
            mf.mesh.SetIndices(iData, MeshTopology.Triangles, 0);
            
            //


            isAltFrame = !isAltFrame;
        }
    }
    
    void Update () {

        RaycastHit hit;
        Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);        
        if (Physics.Raycast(mr, out hit))
        {
            // AtrractPoint = hit.textureCoord * TexResolution;
        }

        if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            bDoUpdate = !bDoUpdate;
        }

        ComputeStepFrame();

        if (Input.GetKeyUp(KeyCode.R))
            ResetComputeSim();
    }
    
}
