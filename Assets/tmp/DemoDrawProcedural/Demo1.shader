Shader "Unlit/Demo1"
{
    Properties
    {
        MQuadSize("half-size of each quad, as a fraction of the whole render texture", float) = 0.01
    }
    SubShader
    {
        Pass
        {
            ColorMask R     /* write only the R component from the final frag() call */
            Blend One One   /* in the final texture, add that component instead of replacing it */
            ZTest Always
            ZWrite Off


            CGPROGRAM
            #pragma warning (default : 3206)  // enable implicit truncation warnings
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag


            /* ========== Input ========== */

            struct Particle
            {
                float2 position;
                float intensity;
            };

            StructuredBuffer<Particle> MParticles;


            /* ========== Vertex shader ========== */
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float intensity : TEXCOORD0;   /* for example */
                float2 delta_unit : TEXCOORD1;
            };

            float MQuadSize;

            v2f vert(uint vertex_id : SV_VertexID)
            {
                Particle p = MParticles[vertex_id / 4];
                uint i = vertex_id % 4;
                float2 delta_unit = float2(i & 1 ? 1.0 : -1.0,
                                           i & 2 ? 1.0 : -1.0);
                float2 pos = p.position + delta_unit * MQuadSize;

                v2f o;
                o.vertex = float4(pos, 0, 1);
                o.intensity = p.intensity;
                o.delta_unit = delta_unit;
                return o;
            }


            /* ========== Fragment shader ========== */

            float frag(v2f i) : SV_Target
            {
                float distance = saturate(length(i.delta_unit));  /* 0 to 1 */

                /* maps both 0 and 1 to 0, and maps 0.5 to 1 */
                float f = distance * (1 - distance) * 4;

                return i.intensity * f;
            }
            ENDCG
        }
    }
}
