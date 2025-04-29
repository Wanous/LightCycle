Shader "Hayq Art / SH_Skybox"
{
	Properties
	{
		[HideInInspector] _texcoord("", 2D) = "white" {}
		[HideInInspector] __dirty("", Int) = 1
	}

		SubShader
		{
			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
			Cull Back
			CGPROGRAM
			#pragma target 3.0
			#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
			struct Input
			{
				float2 uv_texcoord;
			};


			struct Gradient
			{
				int type;
				int colorsLength;
				int alphasLength;
				float4 colors[8];
				float2 alphas[8];
			};


			Gradient NewGradient(int type, int colorsLength, int alphasLength,
			float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
			float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
			{
				Gradient g;
				g.type = type;
				g.colorsLength = colorsLength;
				g.alphasLength = alphasLength;
				g.colors[0] = colors0;
				g.colors[1] = colors1;
				g.colors[2] = colors2;
				g.colors[3] = colors3;
				g.colors[4] = colors4;
				g.colors[5] = colors5;
				g.colors[6] = colors6;
				g.colors[7] = colors7;
				g.alphas[0] = alphas0;
				g.alphas[1] = alphas1;
				g.alphas[2] = alphas2;
				g.alphas[3] = alphas3;
				g.alphas[4] = alphas4;
				g.alphas[5] = alphas5;
				g.alphas[6] = alphas6;
				g.alphas[7] = alphas7;
				return g;
			}


			float4 SampleGradient(Gradient gradient, float time)
			{
				float3 color = gradient.colors[0].rgb;
				UNITY_UNROLL
				for (int c = 1; c < 8; c++)
				{
				float colorPos = saturate((time - gradient.colors[c - 1].w) / (0.00001 + (gradient.colors[c].w - gradient.colors[c - 1].w)) * step(c, (float)gradient.colorsLength - 1));
				color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
				}
				#ifndef UNITY_COLORSPACE_GAMMA
				color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
				#endif
				float alpha = gradient.alphas[0].x;
				UNITY_UNROLL
				for (int a = 1; a < 8; a++)
				{
				float alphaPos = saturate((time - gradient.alphas[a - 1].y) / (0.00001 + (gradient.alphas[a].y - gradient.alphas[a - 1].y)) * step(a, (float)gradient.alphasLength - 1));
				alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
				}
				return float4(color, alpha);
			}


			void surf(Input i , inout SurfaceOutputStandard o)
			{
				Gradient gradient1 = NewGradient(0, 2, 2, float4(0, 0.1570692, 0.1698113, 0.06350805), float4(0, 0, 0, 0.4907607), 0, 0, 0, 0, 0, 0, float2(1, 0), float2(1, 1), 0, 0, 0, 0, 0, 0);
				float2 temp_cast_0 = (0.4).xx;
				float2 uv_TexCoord3 = i.uv_texcoord + temp_cast_0;
				o.Emission = (SampleGradient(gradient1, uv_TexCoord3.y) * 0.8).rgb;
				o.Alpha = 1;
			}

			ENDCG
		}
			Fallback "Diffuse"
}
