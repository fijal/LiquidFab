Shader "Custom/Terrain"
{
    Properties
    {
        _Color ("Color Base Terrain", Color) = (1,1,1,1)
        _TexGrass ("Albedo Grass", 2D) = "white" {}
        _TexIron ("Albedo Iron", 2D) = "white" {}
        _TexSand ("Albedo Sand", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200


        // ---- forward rendering base pass:
        Pass {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma multi_compile_fwdbasealpha noshadowmask nodynlightmap nolightmap noshadow

            #define UNITY_BRDF_PBS BRDF3_Unity_PBS   /* cheapest one, no visible difference here */
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"

            
            fixed4 _Color;
            sampler2D _TexGrass, _TexIron, _TexSand;
            float4 _TexGrass_ST, _TexIron_ST, _TexSand_ST;

            Texture2D<float> MTerrainKind;


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 view_dir : TEXCOORD0;
                float3 world_normal : TEXCOORD1;
                float2 uv : TEXCOOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.view_dir = UnityWorldSpaceViewDir(worldPos);
                o.world_normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float kind = MTerrainKind.Load(uint3(IN.uv, 0)).r;

                float2 uv;
                float3 color;

                [forcecase]
                switch (kind)
                {
                    case 1:
                        uv = TRANSFORM_TEX(IN.uv, _TexGrass);
                        color = tex2D (_TexGrass, uv).rgb;
                        break;

                    case 2:
                        uv = TRANSFORM_TEX(IN.uv, _TexIron);
                        color = tex2D (_TexIron, uv).rgb;
                        break;
                    
                    case 3:
                        uv = TRANSFORM_TEX(IN.uv, _TexSand);
                        color = tex2D (_TexSand, uv).rgb;
                        break;

                    default:
                        color = _Color.rgb;
                        break;
                }
                
                
                /* the rest is extracted from the standard shader.  This was originally extracted
                   into SimplifiedTransparentStandardShader.shader in VRactory */
                float3 worldViewDir = normalize(IN.view_dir);
                fixed3 lightDir = _WorldSpaceLightPos0.xyz;

                SurfaceOutputStandard o;
                UNITY_INITIALIZE_OUTPUT(SurfaceOutputStandard, o);
                o.Albedo = color;
                o.Alpha = 1.0;
                o.Occlusion = 1.0;
                o.Normal = IN.world_normal;
                o.Smoothness = 0.0;
                o.Metallic = 0.0;

                float3 worldPos = float3(0, 0, 0);
                float atten = 1;

                // Setup lighting environment
                UnityGI gi;
                UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
                gi.indirect.diffuse = 0;
                gi.indirect.specular = 0;
                gi.light.color = _LightColor0.rgb;
                gi.light.dir = lightDir;
                // Call GI (lightmaps/SH/reflections) lighting function
                UnityGIInput giInput;
                UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
                giInput.light = gi.light;
                giInput.worldPos = worldPos;
                giInput.worldViewDir = worldViewDir;
                giInput.atten = atten;
                giInput.lightmapUV = 0.0;
                giInput.ambient.rgb = 0.0;
                giInput.probeHDR[0] = unity_SpecCube0_HDR;
                giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
                giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
                giInput.boxMax[0] = unity_SpecCube0_BoxMax;
                giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
                giInput.boxMax[1] = unity_SpecCube1_BoxMax;
                giInput.boxMin[1] = unity_SpecCube1_BoxMin;
                giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
                LightingStandard_GI(o, giInput, gi);
                return LightingStandard(o, worldViewDir, gi);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
