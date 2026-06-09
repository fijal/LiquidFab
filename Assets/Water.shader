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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 light_and_depth : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 light = _WorldSpaceLightPos0.xyz;
                o.light_and_depth.x = dot(normal, light);
                o.light_and_depth.y = v.uv.y;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float3 _Color, _ColorShallow;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = 1;
                col.xyz = lerp(_ColorShallow, _Color, saturate(i.light_and_depth.y));
                col.xyz *= saturate(i.light_and_depth.x);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
