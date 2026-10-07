using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Particles : MonoBehaviour
{
    public ComputeShader shader, clear, simulationStep, counter;
    public TextAsset terrainData, tuneData;
    public AudioSource audio;
    Vector4[] colors;
    float[] attraction;
    float[] soundLevel;

    static int nParticles = 32 * 390; // 10 * 1024;
    static int nColors = 6;
    static int textureSize = 2048;
    ComputeBuffer partBuf, colorBuf, attrBuf, fieldBuf, counterBuf;
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
    static int _dtID = Shader.PropertyToID("dt");
    
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public Vector2 position;
        public int speedx, speedy;
        public int bond1, bond2;
        public int kind;
        public float mass;
    }

    Particle[] particles;
    bool tick = false;

    public void explode()
    {
        partBuf.GetData(particles);
        for (int i = 0; i < nParticles; i++)
        {
            particles[i].speedx += (int)(Random.Range(-0.5f, 0.5f) * 32768);
            particles[i].speedy += (int)(Random.Range(-0.5f, 0.5f) * 32768);
        }
        partBuf.SetData(particles);
    }

    public void interrupt()
    {
        float v;
        if (tick)
        {
            tick = false;
            v = 2.0f;
        } else
        {
            tick = true;
            v = 0.5f;
        }
        partBuf.GetData(particles);
        for (int i = 0; i < nParticles; i++)
        {
            particles[i].speedx = (int)(v * particles[i].speedx);
            particles[i].speedy = (int)(v * particles[i].speedy);
        }
        partBuf.SetData(particles);
    }

    public void stop()
    {
        partBuf.GetData(particles);
        for (int i = 0; i < nParticles; i++)
        {
            particles[i].speedx = 0;// (int)(Random.Range(-1f, 1f) * 32768);
            particles[i].speedy = 0;//  (int)(Random.Range(-1f, 1f) * 32768);
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

    void LoadTuneData()
    {
        int sampleNo = 13782;
        soundLevel = new float[sampleNo];
        var soundLevelBytes = tuneData.bytes;
        System.Buffer.BlockCopy(soundLevelBytes, 0, soundLevel, 0, sampleNo);
        
        /*for (int i = 0; i < sampleNo * 4; i += 4)
        {
            soundLevel[i] = soundLevelBytes[i];
        }*/
    }

    void Start()
    {
        LoadTerrainData();
        LoadTuneData();

        colors = new Vector4[6] { new Vector4(1, 0, 0, 1), new Vector4(0, 0.6f, 0, 1), new Vector4(0, 0, 1, 1), new Vector4(1, 1, 0, 1),
                                  new Vector4(0, 1, 1, 1), new Vector4(1, 0, 1, 1)};
        particles = new Particle[nParticles];
        attraction = new float[nColors * nColors];

        for (int i = 0; i < nColors * nColors; i++)
        {
            attraction[i] = Random.Range(-1.0f, 1.0f);
        }
        /*attraction[0 + 0 * nColors] = 0f;
        attraction[1 + 1 * nColors] = 0f;
        attraction[2 + 2 * nColors] = 0f;
        attraction[3 + 3 * nColors] = 0f;
        attraction[4 + 4 * nColors] = 0f;
        attraction[5 + 5 * nColors] = 0f;*/


        /*attraction[2 + 0 * nColors] = 0.2f;
        attraction[0 + 2 * nColors] = 0.2f;
        attraction[0 + 0 * nColors] = -0.4f;
        attraction[2 + 2 * nColors] = -0.1f;
        attraction[3 + 3 * nColors] = 0.8f;
        attraction[3 + 0 * nColors] = -0.1f;
        attraction[0 + 3 * nColors] = -0.1f;
        attraction[3 + 2 * nColors] = 0.05f;
        attraction[2 + 3 * nColors] = 0.05f;
        */
        /*attraction[0] = 1.0f;
        attraction[1 + 4] = 1.0f;
        attraction[2 + 2 * 4] = 1.0f;
        attraction[3 + 3 * 4] = 1.0f;*/

        int blues = 0;// 210 * 32;
        for (int i = 0; i < blues; i++)
        {
            if (i % 3 == 0)
            {
                particles[i].position = new Vector2(
                    Random.Range(200f, 1800f),//(float)textureSize),
                    Random.Range(200f, 1800f));// (float)textureSize); ;);
                particles[i + 1].position = new Vector2(particles[i].position.x + Random.Range(-10f, 10f), particles[i].position.y + Random.Range(-10f, 10f));
                particles[i + 2].position = new Vector2(particles[i].position.x + Random.Range(-10f, 10f), particles[i].position.y + Random.Range(-10f, 10f));

                particles[i].kind = 0;// Random.Range(0, nColors);
                particles[i].bond1 = i + 1;
                particles[i].bond2 = i + 2;
                particles[i + 1].bond2 = i;
                particles[i + 2].bond1 = i;
                particles[i + 1].bond1 = i + 2;
                particles[i + 2].bond2 = i + 1;
                particles[i].mass = 14.0f;
                particles[i + 1].mass = 1.0f;
                particles[i + 2].mass = 1.0f;
            }
            else
                particles[i].kind = 2;
            //particles[i].speed = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }
        /*if (blues > 1)
        {
            particles[0].bond2 = nParticles - 1;
            particles[blues - 1].bond1 = 0;
        }*/

        for (int i = blues; i < nParticles; i++)
        {
            particles[i].position = new Vector2(
                Random.Range(0f, (float)textureSize),
                Random.Range(0f, (float)textureSize));
            particles[i].kind = Random.Range(0, nColors);
            particles[i].bond1 = -1;
            particles[i].bond2 = -1;
            particles[i].mass = 1;// 22f;
        }

        partBuf = new ComputeBuffer(nParticles, 4 * (2 + 2 + 4));
        colorBuf = new ComputeBuffer(colors.Length, 4 * 4);
        attrBuf = new ComputeBuffer(nColors * nColors, 4);
        counterBuf = new ComputeBuffer(1, 4);

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
        float dt = (soundLevel[(int)(audio.time * 43.06)] + 80) / 80;
        Debug.Log(dt);
        shader.SetFloat(_dtID, dt * dt * dt * 6);
        shader.SetVector(_TimeID, Shader.GetGlobalVector("_Time"));
        shader.Dispatch(0, nParticles / 32, 1, 1);
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
        if (Input.GetKeyDown(KeyCode.Space))
            explode();
        if (Input.GetKeyDown(KeyCode.C))
            stop();
        if (Input.GetKeyDown(KeyCode.S))
        {
            interrupt();
        }
        //var c = new int[1];
        //c[0] = 0;
        //counterBuf.SetData(c);
        //counter.SetBuffer(0, _ParticlesID, partBuf);
        //counter.SetBuffer(0, _ResultID, counterBuf);
        //counter.Dispatch(0, nParticles / 32, nParticles, 1);
        //counterBuf.GetData(c);
        //Debug.Log(c[0]);
    }
}
