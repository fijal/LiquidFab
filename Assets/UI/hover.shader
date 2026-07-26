Shader "LiquidFab/hover"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline color", Color) = (0.0, 0.0, 0.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
    
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float pi = 3.1415;
                float x = i.uv.x - 0.5;
                float y = i.uv.y - 0.5;
                float v = x * x + y * y;
                float angle = (_Time.w / 3.0 + atan2(x, y) + pi) % (pi / 8);
                if (v > 0.35 * 0.35 || v < 0.2 * 0.2) {
                    clip(-1);
                }
                if (angle < pi / 8 / 2) {
                    clip(-1);
                }
                float c = sqrt(v - 0.15 * 0.15);
                fixed4 col = fixed4(c, 1, c, 0.5); //tex2D(_MainTex, i.uv);
                //clip(i.uv.x > 0.5);
                // apply fog
                return col;
            }
            ENDCG
        }
    }
}
