Shader "Custom/InvisibleShadowReceiver"
{
    Properties
    {
        _Alpha ("Alpha", Range(0,1)) = 0.0 // Transparency control
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        Pass
        {
            Name "ShadowReceiver"
            Tags { "LightMode"="UniversalForward" }

            Cull Off          // Render both sides (if needed)
            ZWrite On         // Writes to depth buffer so shadows are computed
            ZTest LEqual      // Ensures depth accuracy
            Blend SrcAlpha OneMinusSrcAlpha // Keeps transparency

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.worldPos); // Convert to shadow coordinates
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half shadow = MainLightRealtimeShadow(IN.shadowCoord); // Compute shadow factor
                return half4(shadow, shadow, shadow, 0); // Invisible but darkens with shadows
            }
            ENDHLSL
        }
    }
}

//Shader "Unlit/InvisibleShadowReceiver"
//{
//    Properties
//    {
//        _MainTex ("Texture", 2D) = "white" {}
//    }
//    SubShader
//    {
//        Tags { "RenderType"="Opaque" }
//        LOD 100
//
//        Pass
//        {
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            // make fog work
//            #pragma multi_compile_fog
//
//            #include "UnityCG.cginc"
//
//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2f
//            {
//                float2 uv : TEXCOORD0;
//                UNITY_FOG_COORDS(1)
//                float4 vertex : SV_POSITION;
//            };
//
//            sampler2D _MainTex;
//            float4 _MainTex_ST;
//
//            v2f vert (appdata v)
//            {
//                v2f o;
//                o.vertex = UnityObjectToClipPos(v.vertex);
//                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                UNITY_TRANSFER_FOG(o,o.vertex);
//                return o;
//            }
//
//            fixed4 frag (v2f i) : SV_Target
//            {
//                // sample the texture
//                fixed4 col = tex2D(_MainTex, i.uv);
//                // apply fog
//                UNITY_APPLY_FOG(i.fogCoord, col);
//                return col;
//            }
//            ENDCG
//        }
//    }
//}
