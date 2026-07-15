Shader "Unlit/arrow"
{
    Properties
    {
        _MainColor ("Main color", Color) = (0.0, 0.0, 0.0, 1.0)
        _Speed ("Speed", Float) = 20
        _Width ("Width", Float) = 0.4
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Overlay+1" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ZTest Always
            ZWrite off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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

            float4 _MainColor;
            sampler2D _MainTex;
            float _Speed;
            float _Width;
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
                //fixed4 col = tex2D(_MainTex, i.uv);
                float f;
                if (i.uv.y > 0.5) {
                    f = ((i.uv.x + i.uv.y) - _Time.x * _Speed - 2) % _Width;
                    if (abs(f) > _Width / 2) {
                        clip(-1);
                    }
                }
                else {
                    f = ((i.uv.x - i.uv.y) - _Time.x * _Speed - 2) % _Width;
                    if (abs(f) < _Width / 2) {
                        clip(-1);
                    }
                }
                //fixed4 col = (_MainColor.x, _MainColor.y, _MainColor.z, 1.0);
                return _MainColor;
            }
            ENDCG
        }
    }
}
