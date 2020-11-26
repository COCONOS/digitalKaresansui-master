using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace MossParticleSystem
{
    // パーティクルデータの構造体
    public struct MossParticleData
    {
        public Vector3 Position; // 位置
        public int isActive;
        public Color Color;
        //float RandomValue;
        //public float Duration;
        //public float Scale;
    };

    public class MossParticleSystem : MonoBehaviour
    {
        //public int NUM_PARTICLES = 32; // 生成するパーティクルの数
        public int NUM_PARTICLES = 32768; // 生成するパーティクルの数

        const int NUM_THREAD_X = 32; // スレッドグループのX成分のスレッド数
        const int NUM_THREAD_Y = 1; // スレッドグループのY成分のスレッド数
        const int NUM_THREAD_Z = 1; // スレッドグループのZ成分のスレッド数

        public ComputeShader SimpleParticleComputeShader; // パーティクルの動きを計算するコンピュートシェーダ
        public Shader SimpleParticleRenderShader;  // パーティクルをレンダリングするシェーダ

        public FitCamera fc;

        public Vector3 AreaSize = Vector3.one * 10.0f;            // パーティクルが存在するエリアのサイズ
        public float bloomTime = 10000.0f;

        public Texture2D ParticleTex;          // パーティクルのテクスチャ
        public float ParticleSize = 0.005f; // パーティクルのサイズ

        public Camera RenderCam; // パーティクルをレンダリングするカメラ（ビルボードのための逆ビュー行列計算に使用）

        //public ComputeShader cs;
        protected ComputeBuffer particleBuffer;     // パーティクルのデータを格納するコンピュートバッファ 
        //protected ComputeBuffer particlePoolBuffer;
        //protected ComputeBuffer particleCountBuffer;

        //public int emitNum = 0;
        //int[] particleCounts = { 1, 1, 0, 0 };

        //static public int initKernel = -1;
        //static public int emitKernel = -1;
        //static public int updateKernel = -1;

        //int particlePoolNum = 0;

        //public float lifeTime;
        //public float scaleMin;
        //public float scaleMax;

        Material particleRenderMat;  // パーティクルをレンダリングするマテリアル

        [SerializeField]
        public VectorGrid vectorGrid; //vectorGrid

        public int NUM_CIRCLES;

        void Start()
        {
            // パーティクルのコンピュートバッファを作成
            particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(MossParticleData)));
            // パーティクルの初期値を設定
            var pData = new MossParticleData[NUM_PARTICLES];
            for (int i = 0; i < pData.Length; i++)
            {
                pData[i].Position.x = UnityEngine.Random.Range(-AreaSize.x / 2.0f, AreaSize.x / 2.0f);
                pData[i].Position.y = 0.0f;
                pData[i].Position.z = UnityEngine.Random.Range(-AreaSize.z / 2.0f, AreaSize.z / 2.0f);

                pData[i].Color.r = UnityEngine.Random.Range(0.0f, 1.0f);
                pData[i].Color.g = UnityEngine.Random.Range(0.8f, 1.0f);
                pData[i].isActive = -1;
                //pData[i].Scale = 0.1f;
            }
            // コンピュートバッファに初期値データをセット
            particleBuffer.SetData(pData);

            pData = null;

            // パーティクルをレンダリングするマテリアルを作成
            particleRenderMat = new Material(SimpleParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;

            //------------------------
            //Initialize();
        }

        private void Update()
        {
            ComputeShader cs = SimpleParticleComputeShader;
            // スレッドグループ数を計算
            int numThreadGroup = NUM_PARTICLES / NUM_THREAD_X;
            // カーネルIDを取得
            int kernelId = cs.FindKernel("CSMain");
            // 各パラメータをセット
            cs.SetFloat("_TimeStep", Time.deltaTime);
            cs.SetFloat("_Time", Time.time);
            cs.SetFloat("_BloomTime", bloomTime);
            cs.SetFloats("_AreaSize", new float[3] { AreaSize.x, AreaSize.y, AreaSize.z });
            // コンピュートバッファをセット
            cs.SetBuffer(kernelId, "_ParticleBuffer", particleBuffer);

            cs.SetFloats("_RenderTexture", new float[3] { vectorGrid.renderTexture_A.width, vectorGrid.renderTexture_A.height, 0 });

            cs.SetTexture(kernelId, "VectorGrid", vectorGrid.renderTexture_A);

            // コンピュートシェーダを実行
            cs.Dispatch(kernelId, numThreadGroup, 1, 1);

            if (Input.GetMouseButtonDown(0))
            {
                NUM_CIRCLES += 1;
                NUM_CIRCLES = NUM_CIRCLES % 10;
                //NUM_PARTICLES = 32 * 8 * NUM_CIRCLES;
                Debug.Log("NUM_CIRCLES: " + NUM_CIRCLES);
                Debug.Log("NUM_PARTICLES: " + NUM_PARTICLES);

                //initParticle();
            }
        }

        void OnRenderObject()
        {
            // 逆ビュー行列を計算
            var inverseViewMatrix = RenderCam.worldToCameraMatrix.inverse;

            Material m = particleRenderMat;
            m.SetPass(0); // レンダリングのためのシェーダパスをセット
            // 各パラメータをセット
            m.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            m.SetTexture("_MainTex", ParticleTex);
            m.SetFloat("_ParticleSize", ParticleSize);
            // コンピュートバッファをセット
            m.SetBuffer("_ParticleBuffer", particleBuffer);
            // パーティクルをレンダリング
            Graphics.DrawProceduralNow(MeshTopology.Points, NUM_PARTICLES);
        }

        void OnDestroy()
        {
            if (particleBuffer != null)
            {
                // バッファをリリース（忘れずに！）
                particleBuffer.Release();
            }

            if (particleRenderMat != null)
            {
                // レンダリングのためのマテリアルを削除
                DestroyImmediate(particleRenderMat);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, AreaSize);
        }
        /*
        private void initParticle()
        {
            // パーティクルのコンピュートバッファを作成
            //particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(MossParticleData)));
            // パーティクルの初期値を設定
            var pData = new MossParticleData[NUM_PARTICLES];
            for (int i = NUM_PARTICLES / 256 - 1; i < pData.Length; i++)
            {
                pData[i].Position.x = UnityEngine.Random.Range(-AreaSize.x / 2, AreaSize.x / 2);
                pData[i].Position.y = 0.0f;
                pData[i].Position.z = UnityEngine.Random.Range(-AreaSize.z / 2, AreaSize.z / 2);
            }
            // コンピュートバッファに初期値データをセット
            particleBuffer.SetData(pData);

            pData = null;

            // パーティクルをレンダリングするマテリアルを作成
            particleRenderMat = new Material(SimpleParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;
        }
        */
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /*
        private void Initialize()
        {
            //ComputeShader cs = (ComputeShader)Resources.Load("MossParticleComputeShader");
            cs = SimpleParticleComputeShader;

            particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(MossParticleData)), ComputeBufferType.Default);

            particlePoolBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
            particlePoolBuffer.SetCounterValue(0);

            particleCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
            particleCounts = new int[] { 0, 1, 0, 0 };
            particleCountBuffer.SetData(particleCounts);

            initKernel = cs.FindKernel("Init");
            emitKernel = cs.FindKernel("Emit");
            updateKernel = cs.FindKernel("Update");

            InitParticle();
        }

        private void InitParticle()
        {
            cs.SetBuffer(initKernel, "_ParticleBuffer", particleBuffer);
            cs.SetBuffer(initKernel, "_DeadList", particlePoolBuffer);
            cs.Dispatch(initKernel, NUM_PARTICLES / NUM_THREAD_X, NUM_PARTICLES / NUM_THREAD_Y, NUM_PARTICLES / NUM_THREAD_Z);
        }

        private void UpdateParticle()
        {
            cs.SetFloat("_DT", Time.deltaTime);
            cs.SetFloat("_LifeTime", lifeTime);
            cs.SetBuffer(updateKernel, "_ParticleBuffer", particleBuffer);
            cs.SetBuffer(updateKernel, "_DeadList", particlePoolBuffer);
            cs.SetFloats("_RenderTexture", new float[3] { vectorGrid.renderTexture_A.width, vectorGrid.renderTexture_A.height, 0 });
            cs.SetTexture(updateKernel, "VectorGrid", vectorGrid.renderTexture_A);

            cs.Dispatch(updateKernel, NUM_PARTICLES / NUM_THREAD_X, NUM_PARTICLES / NUM_THREAD_Y, NUM_PARTICLES / NUM_THREAD_Z);
        }

        void EmitParticle()
        {
            // ConsumeStructuredBuffer内のパーティクル数の残数を取得する
            particleCountBuffer.SetData(particleCounts);
            ComputeBuffer.CopyCount(particlePoolBuffer, particleCountBuffer, 0);
            particleCountBuffer.GetData(particleCounts);

            particlePoolNum = particleCounts[0];

            //if (particleCounts[0] < emitNum) return;   // 残数がemitNum未満なら発生させない

            cs.SetFloat("_LifeTime", lifeTime);
            cs.SetFloat("_ScaleMin", scaleMin);
            cs.SetFloat("_ScaleMax", scaleMax);
            cs.SetFloat("_Time", Time.time);
            cs.SetBuffer(emitKernel, "_ParticlePool", particlePoolBuffer);
            cs.SetBuffer(emitKernel, "_ParticleBuffer", particleBuffer);

            cs.Dispatch(emitKernel, emitNum / NUM_THREAD_X, emitNum / NUM_THREAD_Y, emitNum / NUM_THREAD_Z);   // emitNumの数だけ発生
        }


        void Start()
        {
            particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(MossParticleData)), ComputeBufferType.Default);

            particlePoolBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
            particlePoolBuffer.SetCounterValue(0);

            particleCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
            particleCounts = new int[] { 0, 1, 0, 0 };
            particleCountBuffer.SetData(particleCounts);
        }

        private void Update()
        {
            ComputeShader cs = SimpleParticleComputeShader;

            initKernel = cs.FindKernel("Init");
            emitKernel = cs.FindKernel("Emit");
            updateKernel = cs.FindKernel("Update");

            cs.SetBuffer(initKernel, "_ParticleBuffer", particleBuffer);
            cs.SetBuffer(initKernel, "_DeadList", particlePoolBuffer);
            cs.Dispatch(initKernel, NUM_PARTICLES / NUM_THREAD_X, NUM_PARTICLES / NUM_THREAD_Y, NUM_PARTICLES / NUM_THREAD_Z);

            if (Input.GetMouseButtonDown(0))
            {
                NUM_CIRCLES += 1;
                NUM_CIRCLES = NUM_CIRCLES % 10;
                NUM_PARTICLES = 32 * 8 * NUM_CIRCLES;
                Debug.Log("NUM_CIRCLES: " + NUM_CIRCLES);
                Debug.Log("NUM_PARTICLES: " + NUM_PARTICLES);

                // EmitParticle();
                // ConsumeStructuredBuffer内のパーティクル数の残数を取得する
                particleCountBuffer.SetData(particleCounts);
                ComputeBuffer.CopyCount(particlePoolBuffer, particleCountBuffer, 0);
                particleCountBuffer.GetData(particleCounts);

                particlePoolNum = particleCounts[0];

                //if (particleCounts[0] < emitNum) return;   // 残数がemitNum未満なら発生させない

                cs.SetFloat("_LifeTime", lifeTime);
                cs.SetFloat("_ScaleMin", scaleMin);
                cs.SetFloat("_ScaleMax", scaleMax);
                cs.SetFloat("_Time", Time.time);
                cs.SetBuffer(emitKernel, "_ParticlePool", particlePoolBuffer);
                cs.SetBuffer(emitKernel, "_ParticleBuffer", particleBuffer);

                cs.Dispatch(emitKernel, emitNum / NUM_THREAD_X, emitNum / NUM_THREAD_Y, emitNum / NUM_THREAD_Z);   // emitNumの数だけ発生
            }

            // UpdateParticle();
            cs.SetFloat("_DT", Time.deltaTime);
            cs.SetFloat("_LifeTime", lifeTime);
            cs.SetBuffer(updateKernel, "_ParticleBuffer", particleBuffer);
            cs.SetBuffer(updateKernel, "_DeadList", particlePoolBuffer);
            cs.SetFloats("_RenderTexture", new float[3] { vectorGrid.renderTexture_A.width, vectorGrid.renderTexture_A.height, 0 });
            cs.SetTexture(updateKernel, "VectorGrid", vectorGrid.renderTexture_A);

            cs.Dispatch(updateKernel, NUM_PARTICLES / NUM_THREAD_X, NUM_PARTICLES / NUM_THREAD_Y, NUM_PARTICLES / NUM_THREAD_Z);
        }

        private void ReleaseBuffer()
        {
            if (particlePoolBuffer != null)
            {
                particlePoolBuffer.Release();
            }
            if(particleBuffer != null)
            {
                particleBuffer.Release();
            }
            if(particleCountBuffer != null)
            {
                particleCountBuffer.Release();
            }
        }

        private void OnDestroy()
        {
            ReleaseBuffer();
        }

        private void OnRenderObject()
        {
            // パーティクルをレンダリングするマテリアルを作成
            particleRenderMat = new Material(SimpleParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;

            // 逆ビュー行列を計算
            var inverseViewMatrix = RenderCam.worldToCameraMatrix.inverse;

            Material m = particleRenderMat;
            m.SetPass(0); // レンダリングのためのシェーダパスをセット
            // 各パラメータをセット
            m.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            m.SetTexture("_MainTex", ParticleTex);
            m.SetFloat("_ParticleSize", ParticleSize);
            // コンピュートバッファをセット
            m.SetBuffer("_ParticleBuffer", particleBuffer);
            // パーティクルをレンダリング
            Graphics.DrawProceduralNow(MeshTopology.Points, NUM_PARTICLES);
        }
        */

    }
}