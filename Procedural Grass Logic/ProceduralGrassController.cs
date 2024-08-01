using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGrassController : MonoBehaviour
{
    public int resolution = 16;
    public int scale = 1;

    public Material grassMaterial;
    public Mesh grassMesh;

    public bool updateGrass;

    public ComputeShader grassCompShader;
    private ComputeBuffer grassDataBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public float windSpeed = 1.0f;
    public float frequency = 20.0f;
    public float windStrength = 1.0f;
    private RenderTexture windTex;

    struct Cube
    {
        public Vector4 position;
        public Vector2 uv;
    }

    void Start()
    {
        resolution *= scale;
        grassDataBuffer = new ComputeBuffer(resolution * resolution, 6 * 4);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        windTex = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        windTex.enableRandomWrite = true;
        windTex.Create();

        Graphics.Blit(GenerateTexture(), windTex);

        UpdateGrasses();
    }

    void UpdateGrasses()
    {
        grassCompShader.SetInt("_Resolution", resolution);
        grassCompShader.SetInt("_Scale", scale);

        int focusKernel = grassCompShader.FindKernel("GrassMain");
        grassCompShader.SetBuffer(focusKernel, "_GrassBuffer", grassDataBuffer);

        int groups = Mathf.CeilToInt(resolution / 8.0f);
        grassCompShader.Dispatch(focusKernel, groups, groups, 1);

        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)grassDataBuffer.count;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);

        grassMaterial.SetBuffer("grassBuffer", grassDataBuffer);
        grassMaterial.SetTexture("_WindTex", windTex);
        grassMaterial.SetFloat("_Speed", windSpeed);
    }

    Texture2D GenerateTexture()
    {
        Texture2D tex = new Texture2D(resolution, resolution);

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                Color color = CalculateColor(x, y);
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return tex;
    }

    Color CalculateColor(int x, int y)
    {
        float xCoord = (float)x / resolution * frequency;
        float yCoord = (float)y / resolution * frequency;

        float sample = Mathf.PerlinNoise(xCoord, yCoord) * windStrength;
        return new Color(sample, sample, sample);
    }

    void Update()
    {
        grassMaterial.SetBuffer("grassBuffer", grassDataBuffer);
        grassMaterial.SetFloat("_Speed", windSpeed);

        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, new Bounds(Vector3.zero, new Vector3(-resolution, 200.0f, resolution)), argsBuffer);

        if (updateGrass)
        {
            UpdateGrasses();
            updateGrass = false;
        }
    }

    void OnDisable()
    {
        grassDataBuffer.Dispose();
        argsBuffer.Dispose();
        grassDataBuffer = null;
        argsBuffer = null;
        windTex.Release();
        windTex = null;
    }
}
