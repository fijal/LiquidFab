Shader "Unlit/Water"
{
    Properties
    {
        _Color ("Color Deep", Color) = (0.14902481, 0.33537576, 0.8616352, 1)
        _ColorShallow ("Color Shallow", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
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
                float3 uv3 : TEXCOORD0;     // .xz: water flow;  .y: water depth
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 uv3_and_light : TEXCOORD0;
                float2 src_vertex_xz : TEXCOORD2;
            };

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
                o.uv3_and_light.xz = flow * 0.75;
                o.uv3_and_light.y = v.uv3.y;
                o.uv3_and_light.w = dot(normal, light);
                o.src_vertex_xz = v.vertex.xz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float3 _Color, _ColorShallow;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = 1;
                col.xyz = lerp(_ColorShallow, _Color, saturate(i.uv3_and_light.y));
                col.xyz *= saturate(i.uv3_and_light.w);

                float2 pos = i.src_vertex_xz;
                float2 flow = i.uv3_and_light.xz;

                float2 dot_pos = floor(pos) + 0.5;
                float t = frac(_Time.w + dot(dot_pos, float2(1.3298139, 2.6010221)));
                dot_pos += (t - 0.5) * flow;
                float dot_strength = t * (1 - t) + 0.0001;

                float2 dot_delta = (dot_pos - pos) / dot_strength;
                float s1 = dot(dot_delta, dot_delta);
                col.xyz += (1 - saturate(s1)) * 0.025;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
