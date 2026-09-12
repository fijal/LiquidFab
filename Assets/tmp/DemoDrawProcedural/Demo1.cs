using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class Demo1 : MonoBehaviour
{
    public Material demo1Material;
    public int totalParticlesCount = 1000;
    public RenderTexture outputTexture;    /* should be single component (red only), maybe with 32
                                            * bits for this component, or maybe less to save memory */


    static class Prop
    {
        public static int MParticles = Shader.PropertyToID("MParticles");
        public static int MQuadSize = Shader.PropertyToID("MQuadSize");
    }

    GraphicsBuffer gb_index;
    ComputeBuffer cb_particles;
    UnityEngine.Rendering.CommandBuffer cmdbuf;

    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public Vector2 position;
        public float intensity;
    }
    
    void Start()
    {
        /* precompute an index buffer.  The idea is that we're calling DrawProcedural to render
         * quads, but each quad is two triangles.  We still want the vert() shader to be called
         * four times per quad, instead of six times (for two triangles). */

        var ibuf = new int[totalParticlesCount * 6];
        for (int i = 0; i < totalParticlesCount; i++)
        {
            ibuf[i * 6 + 0] = i * 4 + 0;
            ibuf[i * 6 + 1] = i * 4 + 1;
            ibuf[i * 6 + 2] = i * 4 + 2;
            ibuf[i * 6 + 3] = i * 4 + 1;
            ibuf[i * 6 + 4] = i * 4 + 3;
            ibuf[i * 6 + 5] = i * 4 + 2;
        }
        gb_index = new GraphicsBuffer(GraphicsBuffer.Target.Index, ibuf.Length, 4);
        gb_index.SetData(ibuf);

        /* populate the MParticles array (the content could change every frame) */
        var pbuf = new Particle[totalParticlesCount];
        for (int i = 0; i < pbuf.Length; i++)
        {
            /* place particles anywhere from -1 to +1 in both X and Y */
            pbuf[i].position = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f));
            
            /* we use 'intensity' from 0.2 to 0.8 to get a visually nice result in the RenderTexture,
             * but it stores full floats, so it can contain values that are not clamped in [0, 1] */
            pbuf[i].intensity = Random.Range(0.2f, 0.8f);
        }
        cb_particles = new ComputeBuffer(totalParticlesCount, 3 * 4);
        cb_particles.SetData(pbuf);

        /* prepare a command buffer that clears the RenderTexture and then renders the particles */
        cmdbuf = new UnityEngine.Rendering.CommandBuffer();
        cmdbuf.SetRenderTarget(outputTexture);
        cmdbuf.ClearRenderTarget(false, true, Color.clear);

        cmdbuf.SetGlobalBuffer(Prop.MParticles, cb_particles);
        cmdbuf.DrawProcedural(gb_index, 
                              Matrix4x4.identity, /* not used here */
                              demo1Material, 
                              0,     /* pass number inside the shader */
                              MeshTopology.Triangles, 
                              totalParticlesCount * 6);
    }

    private void OnDestroy()
    {
        if (gb_index != null)
            gb_index.Release();
        if (cb_particles != null)
            cb_particles.Release();
    }

    void Update()
    {
        Graphics.ExecuteCommandBuffer(cmdbuf);
    }
}
