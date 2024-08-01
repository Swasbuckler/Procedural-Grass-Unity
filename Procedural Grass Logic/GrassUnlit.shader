Shader "Unlit/GrassUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off

        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define PI 3.14159265358979323846

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct GrassData
            {
                float4 position;
                float2 uv;
            };

            sampler2D _MainTex;
            sampler2D _WindTex;
            float4 _MainTex_ST;
            StructuredBuffer<GrassData> grassBuffer;

            float _Speed;
 
            float4 RotateAroundYInDegrees (float4 vertex, float degrees)
            {
                float alpha = degrees * PI / 180.0;
                float sina;
                float cosa;

                sincos(alpha, sina, cosa);

                float2x2 m = float2x2(cosa, -sina, sina, cosa);

                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }

            float hash2 (float2 uv)
            {
                return frac(sin(7.289 * uv.x + 11.23 * uv.y) * 43758.5453123);
            }

            float hash (float x)
            {
                return frac(sin(8.134 * x) * 34578.2347875);
            }

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                float4 grassPos = grassBuffer[instanceID].position;

                float perGrassHash = hash2(grassPos.xz);
                float randomAngle = perGrassHash * 360;

                float3 pos = RotateAroundYInDegrees(v.vertex, randomAngle).xyz;

                float swayVariance = lerp(0.8, 1.0, perGrassHash);
                
                float4 worldUV = float4(grassBuffer[instanceID].uv, 0, 0);
                worldUV = worldUV / (50.0f * 2.0f);
                worldUV += 0.5f * (cos(_Time.y * 0.05f * _Speed) + 1.1f);

                float grassSway = tex2Dlod(_WindTex, worldUV) * grassPos.w;
                float maxSway = worldUV * grassPos.w;
                float sway = ((1 - v.uv.x) * (grassSway - (maxSway * 0.5f)) * 1.5f);

                sway *= swayVariance;

                pos.xz += sway;

                float3 worldPos = grassPos.xyz + pos;

                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = lerp(float4(0.5, 0.5, 0.1, 1.0), float4(0.05, 0.2, 0.01, 1.0), i.uv.x);

                return col;
            }

            ENDCG
        }
    }
}
