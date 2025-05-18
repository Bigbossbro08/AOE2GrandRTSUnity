Shader "Custom/TeamColorShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TargetAlpha ("Target Alpha", Range(0,1)) = 0.996 // 254/255
        _HighlightColor ("Highlight Color", Color) = (1,0,0,1) // Color replacement
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Sprite Unlit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha // Enable transparency
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _TargetAlpha;
            float4 _HighlightColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // If the alpha is exactly 254/255, replace only the color (keep original alpha)
                if (abs(col.a - _TargetAlpha) < 0.001) 
                {
                    //uint offset = col.g + (col.r << 8);
                    col.rgb = _HighlightColor.rgb; // Change color but keep alpha
                }

                return col;
            }
            ENDHLSL
        }
    }
}