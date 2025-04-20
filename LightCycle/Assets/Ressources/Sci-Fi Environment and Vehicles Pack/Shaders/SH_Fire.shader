Shader "Hayq Art/Fire"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FireColor ("Fire Color", Color) = (1, 1, 1, 1)
        _FireDetail ("Fire Detail", Range(1, 20)) = 10
        _FirePower ("Fire Power", Range(0, 1)) = 0.5
        _FireThreshold ("Fire Threshold", Range(-1, 0)) = -0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _FireColor;
            float _FireDetail;
            float _FirePower;
            float _FireThreshold;

            inline float unity_noise_randomValue (float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
            }

            inline float unity_noise_interpolate (float a, float b, float t)
            {
                return (1.0-t)*a + (t*b);
            }

            inline float unity_valueNoise (float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                uv = abs(frac(uv) - 0.5);
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0 = unity_noise_randomValue(c0);
                float r1 = unity_noise_randomValue(c1);
                float r2 = unity_noise_randomValue(c2);
                float r3 = unity_noise_randomValue(c3);

                float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
                float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
                float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
                return t;
            }

            void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
            {
                float t = 0.0;

                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3-0));
                t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3-1));
                t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3-2));
                t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                Out = t;
            }

            inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
            {
                float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
                UV = frac(sin(mul(UV, m)) * 46839.32);
                return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
            }

            void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
            {
                float2 g = floor(UV * CellDensity);
                float2 f = frac(UV * CellDensity);
                float t = 8.0;
                float3 res = float3(8.0, 0.0, 0.0);

                for(int y=-1; y<=1; y++)
                {
                    for(int x=-1; x<=1; x++)
                    {
                        float2 lattice = float2(x,y);
                        float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
                        float d = distance(lattice + offset, f);
                        if(d < res.x)
                        {
                            res = float3(d, offset.x, offset.y);
                            Out = res.x;
                            Cells = res.y;
                        }
                    }
                }
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {    
                float2 c_nTime = float2(i.uv.x, i.uv.y + (-0.3 * _Time.y));
                float2 c_vTime = float2(i.uv.x, i.uv.y + (-0.5 * _Time.y));

                float c_vNoise = 0;
                Unity_SimpleNoise_float(c_vTime, _FireDetail, c_vNoise);
                float2 c_vLerp = lerp(c_vTime, c_vNoise, 0.5);

                float c_noise = 0;
                Unity_SimpleNoise_float(c_nTime, _FireDetail, c_noise);

                float c_voronoi = 0;
                float c_cells = 0;
                Unity_Voronoi_float(c_vLerp, 2, _FireDetail, c_voronoi, c_cells);

                float c_multiply = c_noise * c_voronoi;
                float2 c_lerp = lerp(i.uv, c_multiply, float2(0, _FirePower));

                float c_threshold = saturate(((1 - sqrt(i.uv.y)) + _FireThreshold) * 2);
                // sample the texture
                fixed4 col = tex2D(_MainTex, c_lerp);
                col.rgb -= c_threshold;
                float c_alpha = saturate(col.r - c_threshold);
                col.rgb *= _FireColor;

                float3 c_gradient = ((col.rgb * _FireColor) * 20) * 0.5;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, c_gradient);
                return float4(c_gradient, c_alpha);
            }
            ENDCG
        }
    }
}
