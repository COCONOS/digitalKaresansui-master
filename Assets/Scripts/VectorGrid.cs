using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VectorGrid : MonoBehaviour
{
    [SerializeField]
    int gridSize = 10;

    [SerializeField]
    public Vector3 centerPos = new Vector3(96.0f, 0.0f, 54.0f);

    //[SerializeField]
    //public float circleSize = 25.0f;

    [SerializeField]
    public float circleWaveFreq = 1.0f;

    [SerializeField]
    public float circleFreqOffset = 0.0f;

    //[SerializeField]
    //public float circlePower = 1.5f;

    [SerializeField]
    [Range(0, 1)]
    public float circlePowerMax = 1.0f;
    [SerializeField]
    [Range(0, 1)]
    public float circlePowerMin = 0.0f;

    [SerializeField]
    public float waveFreq = 40.0f;

    [SerializeField]
    public float flowIntensity = 0.5f;

    [SerializeField]
    public float stripeIntensity = 0.1f;

    [SerializeField]
    [Range(0f, 0.1f)]
    public float noiseScale = 0.01f;

    [SerializeField]
    [Range(0f, 1f)]
    public float noiseIntensity = 0.9f;

    [SerializeField]
    [Range(0f, 1f)]
    public float timeScale = 1.0f;

    [Range(-2f, 2f)]
    public float preCompMaxX;
    [Range(-2f, 2f)]
    public float preCompMinX;
    [Range(-4f, 4f)]
    public float preCompMaxY;
    [Range(-4f, 4f)]
    public float preCompMinY;

    public Vector2 staticMossCenter = new Vector2(0.0f, 0.0f);
    [Range(0f, 1000f)]
    public float staticMossRadius;

    public RenderTexture renderTexture_A;
    public ComputeShader computeShader;

    ComputeBuffer circleBuffer;

    //ComputeBuffer vgBuffer;

    [SerializeField]
    [Range(0, 10)]
    public int NUM_CIRCLES = 1;

    int NUM_LATEST_CIRCLE = 0;

    CircleData[] cData = new CircleData[10]; //波紋のデータの配列

    Vector2 mouse;

    int kernelMain; //karnelのインデックス
    uint threadSizeX = 128;
    uint threadSizeY = 1;
    uint threadSizeZ = 1;

    // 波紋データの構造体
    public struct CircleData
    {
        public int isActive;
        public Vector3 Position; // 位置
        public float Radius; // 半径
        public float StartTime; // 継続時間
    };

    // Start is called before the first frame update
    void Start()
    {
        int w = Screen.width / gridSize;
        int h = Screen.height / gridSize;

        //RenderTextureの初期化
        this.renderTexture_A = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
        //テクスチャへの書き込みを有効化
        this.renderTexture_A.enableRandomWrite = true;
        this.renderTexture_A.Create();

        //"CSMain"というComputeShader内のカーネルを持ってくる。
        kernelMain = computeShader.FindKernel("CSMain");

        //kernelMainの各スレッドサイズを取得
        computeShader.GetKernelThreadGroupSizes(kernelMain, out threadSizeX, out threadSizeY, out threadSizeZ);

        //_MainTexにrenderTexture_Aを設定
        var mat = GetComponent<Renderer>().material;
        mat.SetTexture("_MainTex", renderTexture_A);

        // 波紋のコンピュートバッファを作成
        circleBuffer = new ComputeBuffer(10, Marshal.SizeOf(typeof(CircleData)));
        // 波紋の初期値を設定
        //var cData = new CircleData[NUM_CIRCLES];
        //var cData = new CircleData[10];
        for (int i = 0; i < cData.Length; i++)
        {
            cData[i].isActive = -1;
            cData[i].Radius = 0.0f;
            cData[i].Position.x = 0.0f;
            cData[i].Position.y = 0.0f;
            cData[i].Position.z = 0.0f;
            cData[i].StartTime = 0.0f;

            //Debug.Log("i + 10: " + (i + 10));
            //cData[i + 10].isActive = -1;
            //cData[i + 10].Radius = 0.0f;
            //cData[i + 10].Position.x = 0.0f;
            //cData[i + 10].Position.y = 0.0f;
            //cData[i + 10].Position.z = 0.0f;
            //cData[i + 10].StartTime = 0.0f;

            Debug.Log(cData[i].Position.x + ", " + cData[i].Position.z);
            Debug.Log(cData.Length);
        }
        // コンピュートバッファに初期値データをセット
        circleBuffer.SetData(cData);

        //vgBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(Texture2D)));

        //cData = null;

    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetTexture(kernelMain, "Result", renderTexture_A);
        //ComputeShaderの実行（CeilToIntは切り上げ、第２３４引数はグループ数の三次元）
        //computeShader.Dispatch(kernelMain, Mathf.CeilToInt((float)renderTexture_A.width / (int)threadSizeX), Mathf.CeilToInt((float)renderTexture_A.height / (int)threadSizeY), (int)threadSizeZ);
        computeShader.SetFloat("_TimeStep", Time.deltaTime);

        //波紋関係の値渡し
        computeShader.SetFloat("_Time", Time.time);
        computeShader.SetFloat("_TimeScale", timeScale);
        computeShader.SetFloats("_CenterPos", new float[3] { centerPos.x, centerPos.y, centerPos.z });
        //computeShader.SetFloat("_CircleSize", circleSize);
        computeShader.SetFloat("_CircleWaveFreq", circleWaveFreq);
        computeShader.SetFloat("_CircleFreqOffset", circleFreqOffset);
        //computeShader.SetFloat("_CirclePower", circlePower);
        computeShader.SetFloat("_CirclePowerMax", circlePowerMax);
        computeShader.SetFloat("_CirclePowerMin", circlePowerMin);
        computeShader.SetFloat("_WaveFreq", waveFreq);
        computeShader.SetFloat("_FlowIntensity", flowIntensity);
        computeShader.SetFloat("_StripeIntensity", stripeIntensity);
        computeShader.SetFloat("_NoiseScale", noiseScale);
        computeShader.SetFloat("_NoiseIntensity", noiseIntensity);

        computeShader.SetInt("_NUM_CIRCLES", NUM_CIRCLES);

        // カーネルIDを取得
        int kernelId = computeShader.FindKernel("CSMain");

        // コンピュートバッファをセット
        computeShader.SetBuffer(kernelId, "_CircleBuffer", circleBuffer);

        computeShader.Dispatch(kernelMain, Mathf.CeilToInt((float)renderTexture_A.width / (int)threadSizeX), Mathf.CeilToInt((float)renderTexture_A.height / (int)threadSizeY), (int)threadSizeZ);

        float Player1PosX = (renderTexture_A.width / (preCompMaxX - preCompMinX)) * (KinectManager.player1Pos.x - preCompMinX);
        float Player1PosY = (renderTexture_A.height / (preCompMaxY - preCompMinY)) * (KinectManager.player1Pos.z - preCompMinY);

        computeShader.SetFloat("_Player1PosX", Player1PosX);
        computeShader.SetFloat("_Player1PosY", Player1PosY);

        Debug.Log(Player1PosX);
        Debug.Log(Player1PosY);

        computeShader.SetFloats("staticMossCenter", new float[2] { staticMossCenter.x, staticMossCenter.y });
        computeShader.SetFloat("staticMossRadius", staticMossRadius);

        //OnHumanBeing();
        //if (KinectManager.NingenPos.x < 1.8f && KinectManager.NingenPos.x > -1.8f && KinectManager.NingenPos.z < 1.0f && KinectManager.NingenPos.z > -1.0f)
        //if (KinectManager.player1Pos.x < 1.8f && KinectManager.player1Pos.x > -1.8f && KinectManager.player1Pos.z < 1.0f && KinectManager.player1Pos.z > -1.0f)
        //{
        //    OnHumanBeing();
        //}

        /*
        foreach (Touch t in Input.touches)
        {
            var id = t.fingerId;

            
            switch (t.phase)
            {
                case TouchPhase.Began:
                    Debug.LogFormat("{0}:いまタッチした", id);
                    NUM_CIRCLES += 1;
                    cData[id].isActive = 1;
                    cData[id].Position.x = Input.touches[id].position.x;
                    cData[id].Position.y = 0.0f;
                    cData[id].Position.z = Input.touches[id].position.y;
                    cData[id].Radius = Random.Range(100.0f, 250.0f);
                    cData[id].StartTime = Time.time;

                    //cData[id + 10].isActive = 1;
                    //cData[id + 10].Position.x = Input.touches[id].position.x;
                    //cData[id + 10].Position.y = 0.0f;
                    //cData[id + 10].Position.z = Input.touches[id].position.y;
                    //cData[id + 10].Radius = Random.Range(100.0f, 250.0f);
                    //cData[id + 10].StartTime = Time.time;

                    circleBuffer.SetData(cData);
                    break;

                //case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    Debug.LogFormat("{0}:タッチしている", id);
                    break;

                case TouchPhase.Ended:

                    Debug.LogFormat("{0}:いま離された", id);
                    NUM_CIRCLES -= 1;
                    cData[id].isActive = -1;
                    cData[id].Position.x = 0.0f;
                    cData[id].Position.y = 0.0f;
                    cData[id].Position.z = 0.0f;
                    cData[id].Radius = 0.0f;
                    cData[id].StartTime = 0.0f;

                    //cData[id + 10].isActive = -1;
                    //cData[id + 10].Position.x = 0.0f;
                    //cData[id + 10].Position.y = 0.0f;
                    //cData[id + 10].Position.z = 0.0f;
                    //cData[id + 10].Radius = 0.0f;
                    //cData[id + 10].StartTime = 0.0f;

                    circleBuffer.SetData(cData);
                    break;
            }

        }

        
        foreach (Touch t in Input.touches)
        {
            var id = t.fingerId;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    Debug.LogFormat("{0}:いまタッチした", id);
                    NUM_CIRCLES = NUM_CIRCLES % 10;
                    cData[NUM_CIRCLES].isActive = 1;
                    cData[NUM_CIRCLES].Position.x = Input.touches[id].position.x;
                    cData[NUM_CIRCLES].Position.y = 0.0f;
                    cData[NUM_CIRCLES].Position.z = Input.touches[id].position.y;
                    cData[NUM_CIRCLES].Radius = Random.Range(100.0f, 250.0f);
                    cData[NUM_CIRCLES].StartTime = Time.time;

                    NUM_CIRCLES += 1;
                    circleBuffer.SetData(cData);
                    break;

                case TouchPhase.Stationary:
                    Debug.LogFormat("{0}:タッチしている", id);
                    break;
            }

        }
        */

    }
    void OnDestroy()
    {
        if (circleBuffer != null)
        {
            // バッファをリリース（忘れずに！）
            circleBuffer.Release();
        }

    }

    /*private void OnMouseDown()
    {
        NUM_CIRCLES = NUM_CIRCLES % 10;
        cData[NUM_CIRCLES].isActive = 1;
        cData[NUM_CIRCLES].Position.x = Input.mousePosition.x;
        cData[NUM_CIRCLES].Position.y = 0.0f;
        cData[NUM_CIRCLES].Position.z = Input.mousePosition.y;
        cData[NUM_CIRCLES].Radius = Random.Range(105.0f, 230.0f);
        //cData[NUM_CIRCLES].StartTime = Time.time;

        NUM_CIRCLES += 1;
        circleBuffer.SetData(cData);
    }*/

    private void OnMouseDown()
    {
        if(NUM_CIRCLES == 0)
        {
            NUM_CIRCLES = NUM_CIRCLES % 10;
            cData[NUM_CIRCLES].isActive = 1;
            cData[NUM_CIRCLES].Position.x = 900.0f;
            //cData[NUM_CIRCLES].Position.x = (renderTexture_A.width / (preCompMaxX - preCompMinX)) * (KinectManager.player1Pos.x - preCompMinX);
            cData[NUM_CIRCLES].Position.y = 0.0f;
            cData[NUM_CIRCLES].Position.z = 600.0f;
            //cData[NUM_CIRCLES].Position.z = (rendTexture_A.height / (preCompMaxY - preCompMinY)) * (KinectManager.player1Pos.z - preCompMinY);
            cData[NUM_CIRCLES].Radius = Random.Range(100.0f, 230.0f);
            cData[NUM_CIRCLES].StartTime = Time.time;

            NUM_CIRCLES = 1;
            circleBuffer.SetData(cData);
        }
        else if(NUM_CIRCLES == 1)
        {
            NUM_CIRCLES = NUM_CIRCLES % 10;
            cData[NUM_CIRCLES].isActive = 1;
            cData[NUM_CIRCLES].Position.x = 1100.0f;
            //cData[NUM_CIRCLES].Position.x = (renderTexture_A.width / (preCompMaxX - preCompMinX)) * (KinectManager.player1Pos.x - preCompMinX);
            cData[NUM_CIRCLES].Position.y = 0.0f;
            cData[NUM_CIRCLES].Position.z = 500.0f;
            //cData[NUM_CIRCLES].Position.z = (rendTexture_A.height / (preCompMaxY - preCompMinY)) * (KinectManager.player1Pos.z - preCompMinY);
            cData[NUM_CIRCLES].Radius = Random.Range(100.0f, 230.0f);
            cData[NUM_CIRCLES].StartTime = Time.time;

            NUM_CIRCLES = 1;
            circleBuffer.SetData(cData);
        }
        
    }
    /*
    void OnHumanBeing()
    {
        Debug.Log("KinectManager.player1Pos.x: " + KinectManager.player1Pos.x);
        Debug.Log("KinectManager.player1Pos.z: " + KinectManager.player1Pos.z);

        NUM_CIRCLES = NUM_CIRCLES % 10;
        cData[NUM_CIRCLES].isActive = 1;
        cData[NUM_CIRCLES].Position.x = renderTexture_A.width * KinectManager.player1Pos.x;
        cData[NUM_CIRCLES].Position.y = 0.0f;
        cData[NUM_CIRCLES].Position.z = renderTexture_A.height * KinectManager.player1Pos.z;
        cData[NUM_CIRCLES].Radius = Random.Range(100.0f, 230.0f);
        //cData[NUM_CIRCLES].StartTime = Time.time;

        NUM_CIRCLES = 1;
        circleBuffer.SetData(cData);
    }
    */
}