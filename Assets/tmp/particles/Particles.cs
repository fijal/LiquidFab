using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Particles : MonoBehaviour
{
    public ComputeShader shader, clear, simulationStep;
    Vector4[] colors;
    float[] attraction;

    static int nParticles = 1024 * 20;
    static int nColors = 6;
    static int textureSize = 2048;
    ComputeBuffer partBuf, colorBuf, attrBuf;

    static int _ResultID = Shader.PropertyToID("Result");
    static int _ParticlesID = Shader.PropertyToID("Particles");
    static int _colorsID = Shader.PropertyToID("colors");
    static int _TimeID = Shader.PropertyToID("_Time");
    static int _attractionID = Shader.PropertyToID("attraction");
    static int _textureSizeID = Shader.PropertyToID("textureSize");
    static int _nColorsID = Shader.PropertyToID("nColors");
    
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public Vector2 position;
        public Vector2 speed;
        public int kind;
    }

    Particle[] particles;

    void Start()
    {
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
                Random.Range(0f, (float)textureSize),
                Random.Range(0f, (float)textureSize));
            particles[i].kind = Random.Range(0, nColors);
            particles[i].speed = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }

        partBuf = new ComputeBuffer(nParticles, 4 * (2 + 2 + 1));
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
    }
}
