
Shader "Custom/PlayerColorTest"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "black" {}
        _PlayerColor ("Player Color", Color) = (1, 0, 0, 1)
    }
    SubShader
    {
        //Tags { 
        //    "RenderType"="Transparent" 
        //    "Queue"="Transparent" 
        //    "RenderPipeline"="UniversalRenderPipeline" 
        //}
        Tags {
            "Queue"= "Transparent"//"AlphaTest" // or Transparent+10
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline" 
        }
        //ZWrite On
        //ZTest LEqual

        Pass
        {
            Name "PlayerColorPass"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite Off
            Cull Off
            
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            float4 _PlayerColor;
            //TEXTURE2D(_CameraDepthTexture);
            //SAMPLER(sampler_CameraDepthTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 RGBtoHSL(float3 color)
            {
                float maxC = max(color.r, max(color.g, color.b));
                float minC = min(color.r, min(color.g, color.b));
                float delta = maxC - minC;

                float h = 0.0;
                float s = 0.0;
                float l = (maxC + minC) * 0.5;

                if (delta > 0.00001)
                {
                    s = delta / (1.0 - abs(2.0 * l - 1.0) + 1e-5);
                    if (maxC == color.r)
                        h = (color.g - color.b) / delta + (color.g < color.b ? 6.0 : 0.0);
                    else if (maxC == color.g)
                        h = (color.b - color.r) / delta + 2.0;
                    else
                        h = (color.r - color.g) / delta + 4.0;

                    h /= 6.0;
                }

                return float3(h, s, l);
            }

            float hue2rgb(float p, float q, float t)
            {
                if (t < 0.0) t += 1.0;
                if (t > 1.0) t -= 1.0;
                if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
                return p;
            }

            float3 HSLtoRGB(float3 hsl)
            {
                float r, g, b;

                if (hsl.y == 0.0)
                {
                    r = g = b = hsl.z;
                }
                else
                {
                    float q = hsl.z < 0.5 ? hsl.z * (1.0 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
                    float p = 2.0 * hsl.z - q;
                    r = hue2rgb(p, q, hsl.x + 1.0 / 3.0);
                    g = hue2rgb(p, q, hsl.x);
                    b = hue2rgb(p, q, hsl.x - 1.0 / 3.0);
                }

                return saturate(float3(r, g, b));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).a;
            
                // Player mask coloring (your original logic)
                if (mask >= 0.99)
                {
                    float3 baseHSL = RGBtoHSL(baseColor.rgb);
                    float3 playerHSL = RGBtoHSL(_PlayerColor.rgb);
                    float3 resultHSL = float3(playerHSL.r, playerHSL.g, baseHSL.b);
                    float3 resultRGB = HSLtoRGB(resultHSL);
                    return float4(resultRGB, baseColor.a);
                }
            
                return baseColor;
            }
            ENDHLSL
        }
    }
}