using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Painter : MonoBehaviour
{
    public Texture2D tex;
    int WIDTH, HEIGHT;
    //Color32[] pixels;
    NativeArray<Color32> pixels;

    private void Start()
    {
        WIDTH = Screen.width;
        HEIGHT = Screen.height;
        tex = new Texture2D(Screen.width, Screen.height);
        //arr = new float[Screen.width * Screen.height];
        pixels = tex.GetPixelData<Color32>(0);
        /*for (int x = 0; x < Screen.width; x++)
            for (int y = 0; y < Screen.height; y++)
            {
                pixels[x + y * Screen.width].a = 255;
                pixels[x + y * Screen.width].r = 0x14;
                pixels[x + y * Screen.width].g = 0x40;
                pixels[x + y * Screen.width].b = 0xB9;
            }
        tex.SetPixels32(pixels);
        tex.Apply();*/
        GetComponent<RawImage>().material.SetTexture("_MainTex", tex);
        /*Debug.Log(pixels[32]);
        pixels[32] = new Color32(100, 100, 100, 100);
        Debug.Log(pixels[32]);
        tex.SetPixels32(pixels);
        Debug.Log(tex.GetPixels32()[32]);*/
    }

    void FixedUpdate()
    {
        var c = new Color32(0x14, 0x40, 0xB9, 255);
        for (int x = 0; x < WIDTH; x++)
            for (int y = 0; y < HEIGHT; y++)
            {
                pixels[x + y * WIDTH] = c;
                //pixels[x + y * Screen.width].a = 255;
                //pixels[x + y * Screen.width].r = 0x14;
                //pixels[x + y * Screen.width].g = 0x40;
                //pixels[x + y * Screen.width].b = 0xB9;
            }
        tex.Apply();
    }
}
