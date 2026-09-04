using UnityEngine;
using UnityEngine.UI;

public class Thinker : MonoBehaviour
{
    public ComputeShader shader;
    public RenderTexture tex, tex2;

    static int _ResultID = Shader.PropertyToID("Result");
    static int _InputID = Shader.PropertyToID("Input");

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //tex = new RenderTexture(2000, 2000, 24);
        //tex.enableRandomWrite = true;
        tex = GetComponent<RawImage>().mainTexture as RenderTexture;
        tex2 = new RenderTexture(tex);
        //shader.SetBuffer(0, _ResultID, tex);
        //Debug.Log()
    }

    // Update is called once per frame
    void Update()
    {
        RenderTexture b;

        b = tex2;
        tex2 = tex;
        tex = b;
        shader.SetVector("_Time", Shader.GetGlobalVector("_Time"));
        shader.SetTexture(0, _ResultID, tex);
        shader.SetTexture(0, _InputID, tex2);
        shader.Dispatch(0, 2048 / 8, 2048 / 8, 1);
        GetComponent<RawImage>().material.SetTexture("_MainTex", tex);
    }
}
