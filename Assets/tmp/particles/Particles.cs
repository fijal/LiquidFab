using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Particles : MonoBehaviour
{
    public ComputeShader shader, clear, simulationStep;
    public TextAsset terrainData;
    Vector4[] colors;
    float[] attraction;

    static int nParticles = 32; // 32;//1024 * 20;
    static int nColors = 6;
    static int textureSize = 2048;
    ComputeBuffer partBuf, colorBuf, attrBuf, fieldBuf;
    float[] field;

    static int _ResultID = Shader.PropertyToID("Result");
    static int _ParticlesID = Shader.PropertyToID("Particles");
    static int _colorsID = Shader.PropertyToID("colors");
    static int _TimeID = Shader.PropertyToID("_Time");
    static int _attractionID = Shader.PropertyToID("attraction");
    static int _textureSizeID = Shader.PropertyToID("textureSize");
    static int _nColorsID = Shader.PropertyToID("nColors");
    static int _spawnPointID = Shader.PropertyToID("spawnPoint");
    static int _fieldID = Shader.PropertyToID("field");
    
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public Vector2 position;
        public Vector2 speed;
        public int bond1, bond2;
        public int kind;
    }

    Particle[] particles;

    public void explode()
    {
        partBuf.GetData(particles);
        for (int i = 0; i < nParticles; i++)
        {
            particles[i].speed = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }
        partBuf.SetData(particles);
    }

    public void LoadTerrainData()
    {
        var terrainDataBytes = terrainData.bytes;
        var TERRAIN_SIZE = 256;
        var terrainHeight = new float[TERRAIN_SIZE * TERRAIN_SIZE];
        var index = 0;
        for (int y = 0; y < TERRAIN_SIZE; y++)
            for (int x = 0; x < TERRAIN_SIZE; x++)
            {
                //terrainHeight[x + y * TERRAIN_SIZE] = 0;
                terrainHeight[x + y * TERRAIN_SIZE] = (
                    (float)((terrainDataBytes[index + 1] << 8) | terrainDataBytes[index]) / (1 << 16));
                index += 2;
            }
        field = new float[textureSize * textureSize * 2];
        var factor = textureSize / TERRAIN_SIZE;
        float c = 0.2f;
        for (int y = factor; y < textureSize; y++)
            for (int x = factor; x < textureSize; x++)
            {
                field[2 * (x + y * textureSize)] = (terrainHeight[(x / factor) + (y / factor) * TERRAIN_SIZE] -
                                                    terrainHeight[x / factor - 1 + y / factor * TERRAIN_SIZE]) * c;
                field[2 * (x + y * textureSize) + 1] = (terrainHeight[(x / factor) + (y / factor) * TERRAIN_SIZE] -
                                                        terrainHeight[x / factor + (y / factor - 1) * TERRAIN_SIZE]) * c;
            }
        fieldBuf = new ComputeBuffer(textureSize * textureSize * 2, 4);
        fieldBuf.SetData(field);
        shader.SetBuffer(0, _fieldID, fieldBuf);
    }

    void Start()
    {
        LoadTerrainData();

        colors = new Vector4[6] { new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1), new Vector4(0, 0, 1, 1), new Vector4(1, 1, 0, 1),
                                  new Vector4(0, 1, 1, 1), new Vector4(1, 0, 1, 1)};
        particles = new Particle[nParticles];
        attraction = new float[nColors * nColors];

        for (int i = 0; i < nColors * nColors; i++)
        {
            attraction[i] = Random.Range(-1.0f, 1.0f);
        }
        /*attraction[0] = 1.0f;
        attraction[1 + 4] = 1.0f;
        attraction[2 + 2 * 4] = 1.0f;
        attraction[3 + 3 * 4] = 1.0f;*/

        for (int i = 0; i < nParticles; i++)
        {
            particles[i].position = new Vector2(
                Random.Range(1000f, 1200f),//(float)textureSize),
                Random.Range(1000f, 1200f));//(float)textureSize));
            particles[i].kind = 0;// Random.Range(0, nColors);
            if (i != nParticles - 1)
                particles[i].bond1 = i + 1;
            else
                particles[i].bond1 = -1;
            if (i != 0)
                particles[i].bond2 = i - 1;
            else
                particles[i].bond2 = -1;
            //particles[i].speed = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }

        partBuf = new ComputeBuffer(nParticles, 4 * (2 + 2 + 3));
        colorBuf = new ComputeBuffer(colors.Length, 4 * 4);
        attrBuf = new ComputeBuffer(nColors * nColors, 4);

        partBuf.SetData(particles);
        colorBuf.SetData(colors);
        attrBuf.SetData(attraction);
        shader.SetBuffer(0, _ParticlesID, partBuf);
        shader.SetInt(_textureSizeID, textureSize);
        shader.SetTexture(0, _ResultID, GetComponent<RawImage>().mainTexture);
        shader.SetBuffer(0, _colorsID, colorBuf);
        shader.SetInt(_nColorsID, nColors);
        shader.SetVector(_spawnPointID, new Vector2(1200, 1200));
        clear.SetTexture(0, _ResultID, GetComponent<RawImage>().mainTexture);
        clear.Dispatch(0, textureSize / 8, textureSize / 8, 1);
        simulationStep.SetBuffer(0, _ParticlesID, partBuf);
        simulationStep.SetBuffer(0, _attractionID, attrBuf);
        simulationStep.SetInt(_textureSizeID, textureSize);
        simulationStep.SetInt(_nColorsID, nColors);
    }

    void Update()
    {
        clear.Dispatch(0, textureSize / 8, textureSize / 8, 1);
        simulationStep.Dispatch(0, nParticles / 8, nParticles / 8, 1);
        shader.SetVector(_TimeID, Shader.GetGlobalVector("_Time"));
        shader.Dispatch(0, nParticles / 32, 1, 1);
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
        if (Input.GetKeyDown(KeyCode.Space))
            explode();
    }
}
