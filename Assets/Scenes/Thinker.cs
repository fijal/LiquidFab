using UnityEngine;
using UnityEngine.UI;

public class SwappingBuffer
{
    public ComputeBuffer buf, buf2;

    public SwappingBuffer(int size)
    {
        buf = new ComputeBuffer(size * size, 4 * 4);
        buf2 = new ComputeBuffer(size * size, 4 * 4);
    }

    public void swap()
    {
        ComputeBuffer b;

        b = buf2;
        buf2 = buf;
        buf = b;
    }

    public void Dispose()
    {
        buf.Release();
        buf2.Release();
    }
}

public class Thinker : MonoBehaviour
{
    public ComputeShader shader;
    public ComputeShader clear;
    public RenderTexture tex, tex2;

    SwappingBuffer particles;

    static int _ResultID = Shader.PropertyToID("Result");
    static int _InputID = Shader.PropertyToID("Input");

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particles = new SwappingBuffer(2048);
        //tex = new RenderTexture(2000, 2000, 24);
        //tex.enableRandomWrite = true;
        tex = GetComponent<RawImage>().mainTexture as RenderTexture;
        tex2 = new RenderTexture(tex);
        //shader.SetBuffer(0, _ResultID, tex);
        //Debug.Log()
        //Debug.Log(shader.FindKernel("CSMain"));
        //Debug.Log(shader.FindKernel("processParticles"));
    }

    // Update is called once per frame
    void Update()
    {
        RenderTexture b;
        float scalex = (float)Screen.width / 2048;
        float scaley = (float)Screen.height / 2048;

        b = tex2;
        tex2 = tex;
        tex = b;

        clear.SetTexture(0, "Result", tex);
        clear.Dispatch(0, 2048 / 8, 2048 / 8, 1);

        shader.SetVector("_Time", Shader.GetGlobalVector("_Time"));
        if (Input.GetMouseButton(0))
        {
            shader.SetInts("Cursor", new int[] { (int)(Input.mousePosition.x / scalex), (int)(Input.mousePosition.y / scaley), 0, 0 });
        } else
        {
            shader.SetInts("Cursor", new int[] { 2048, 2048, 0, 0 });
        }
        shader.SetTexture(0, _ResultID, tex);
        shader.SetTexture(0, _InputID, tex2);
        shader.SetBuffer(0, "particlesIn", particles.buf);
        shader.SetBuffer(0, "particlesOut", particles.buf2);
        shader.Dispatch(0, 2048 / 8, 2048 / 8, 1);
        particles.swap();
        //Debug.Log(shader)
        GetComponent<RawImage>().material.SetTexture("_MainTex", tex);
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}
