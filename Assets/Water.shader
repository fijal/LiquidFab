Shader "Unlit/Water"
{
    Properties
    {
        _Color ("Color Deep", Color) = (0.14902481, 0.33537576, 0.8616352, 1) // 1440B9
        _ColorShallow ("Color Shallow", Color) = (1, 1, 1, 1)                 // 604C94
        _ColorMud ("Color Mud", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        // this is an early transparent shader that still updates the z buffer
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv3 : TEXCOORD0;     // .xz: water flow;  .y: water depth w: color percentage
                float3 color : COLOR0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 misc : TEXCOORD0;
                float3 src_vertex_xz : TEXCOORD2; // z is used for color percentage
                float3 color : TEXCOORD3;
            };
            #define m_flow         misc.xy
            #define m_waterdepth   misc.z
            #define m_light        misc.w
            #define src_color_perc src_vertex_xz.z

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 light = _WorldSpaceLightPos0.xyz;

                float2 flow = v.uv3.xz * 12;
                float sqr_mag = dot(flow, flow);
                if (sqr_mag > 1)
                    flow *= rsqrt(sqr_mag);
                o.m_flow = flow * 0.75;
                o.m_waterdepth = v.uv3.y;
                o.m_light = dot(normal, light);
                o.color = v.color;
                o.src_vertex_xz.xy = v.vertex.xz;
                o.src_vertex_xz.z = v.uv3.w;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float3 _Color, _ColorShallow, _ColorMud;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col;
                col.rgb = lerp(_Color, i.color, saturate(i.src_vertex_xz.z));
                col.rgb *= saturate(i.m_light);

                float2 pos = i.src_vertex_xz.xy;
                float2 flow = i.m_flow;

                float2 dot_pos = floor(pos) + 0.5;
                float t = frac(_Time.w + dot(dot_pos, float2(1.3298139, 2.6010221)));
                dot_pos += (t - 0.5) * flow;
                float dot_strength = t * (1 - t) + 0.0001;

                float2 dot_delta = (dot_pos - pos) / dot_strength;
                float s1 = dot(dot_delta, dot_delta);
                col.rgb += (1 - saturate(s1)) * 0.025;
                col.a = saturate(i.m_waterdepth * 60);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
