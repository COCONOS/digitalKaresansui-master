using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

namespace KaresansuiParticleSystem
{
    // パーティクルデータの構造体
    public struct ParticleData
    {
        public Vector3 Velocity; // 速度
        public Vector3 Position; // 位置
        public float Mass; // 質量
        public Vector4 Color;
    };

    public class KaresansuiParticleSystem : MonoBehaviour
    {
        public int NUM_PARTICLES = 32768; // 生成するパーティクルの数

        const int NUM_THREAD_X = 32; // スレッドグループのX成分のスレッド数
        const int NUM_THREAD_Y = 1; // スレッドグループのY成分のスレッド数
        const int NUM_THREAD_Z = 1; // スレッドグループのZ成分のスレッド数

        public ComputeShader SimpleParticleComputeShader; // パーティクルの動きを計算するコンピュートシェーダ
        public Shader SimpleParticleRenderShader;  // パーティクルをレンダリングするシェーダ

        public FitCamera fc;

        public Vector3 Gravity = new Vector3(0.0f, -1.0f, 0.0f); // 重力
        public Vector3 AreaSize = Vector3.one * 10.0f;            // パーティクルが存在するエリアのサイズ
        //public Vector3 AreaSize = new Vector3(0.0f, 0.0f, 0.0f);

        public Texture2D ParticleTex;          // パーティクルのテクスチャ
        public float ParticleSize = 0.005f; // パーティクルのサイズ
        [SerializeField]
        public float maxSpeed = 0.3f;

        public Camera RenderCam; // パーティクルをレンダリングするカメラ（ビルボードのための逆ビュー行列計算に使用）

        ComputeBuffer particleBuffer;     // パーティクルのデータを格納するコンピュートバッファ 
        Material particleRenderMat;  // パーティクルをレンダリングするマテリアル

        [SerializeField]
        public VectorGrid vectorGrid; //vectorGrid

        [SerializeField]
        [Range(0.1f, 10.0f)]
        public float massMin = 1.0f;
        [SerializeField]
        [Range(0.1f, 10.0f)]
        public float massMax = 5.0f;

        void Start()
        {

            //AreaSize.x = fc.AreaSize.x;
            //AreaSize.y = 1.0f;
            //AreaSize.z = fc.AreaSize.z;

            // パーティクルのコンピュートバッファを作成
            particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleData)));
            // パーティクルの初期値を設定
            var pData = new ParticleData[NUM_PARTICLES];
            for (int i = 0; i < pData.Length; i++)
            {
                //pData[i].Velocity.x = 0.0f;
                //pData[i].Velocity.x = -Math.Abs(UnityEngine.Random.insideUnitSphere.x * 0.5f);
                pData[i].Velocity.x = UnityEngine.Random.Range(-0.5f, -0.01f);
                pData[i].Velocity.y = 0.0f;
                pData[i].Velocity.z = 0.0f;

                pData[i].Position.x = UnityEngine.Random.Range(-AreaSize.x / 2, AreaSize.x / 2);
                pData[i].Position.y = 0.0f;
                pData[i].Position.z = UnityEngine.Random.Range(-AreaSize.z / 2, AreaSize.z / 2);

                pData[i].Mass = UnityEngine.Random.Range(massMin, massMax);
                //pData[i].Radius = Random.Range(0.03f, 0.06f);
            }
            // コンピュートバッファに初期値データをセット
            particleBuffer.SetData(pData);

            pData = null;

            // パーティクルをレンダリングするマテリアルを作成
            particleRenderMat = new Material(SimpleParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;
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
            cs.SetVector("_Gravity", Gravity);
            cs.SetFloats("_AreaSize", new float[3] { AreaSize.x, AreaSize.y, AreaSize.z });
            cs.SetFloat("_MassMax", massMax);
            cs.SetFloat("_MassMin", massMin);
            // コンピュートバッファをセット
            cs.SetBuffer(kernelId, "_ParticleBuffer", particleBuffer);

            cs.SetFloats("_RenderTexture", new float[3] { vectorGrid.renderTexture_A.width, vectorGrid.renderTexture_A.height, 0 });

            cs.SetFloat("_MaxSpeed", maxSpeed);

            //cs.SetFloats("_Random", new float[3] { Random.insideUnitSphere.x, Random.insideUnitSphere.y, Random.insideUnitSphere.z });

            cs.SetTexture(kernelId, "VectorGrid", vectorGrid.renderTexture_A);

            // コンピュートシェーダを実行
            cs.Dispatch(kernelId, numThreadGroup, 1, 1);
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
    }
}