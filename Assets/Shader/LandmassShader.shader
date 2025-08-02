Shader "Custom/Landmass_Unlit"
{
    Properties
    {
        //_MainTex ("Base UV", 2D) = "white" {}
        _TextureA ("Grass", 2D) = "white" {}
        _TextureB ("Beach", 2D) = "white" {}
        _Mask ("LandMask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            //sampler2D _MainTex;
            sampler2D _TextureA;
            sampler2D _TextureB;
            sampler2D _Mask;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 a = tex2D(_TextureA, i.uv * 30.0);
                fixed4 b = tex2D(_TextureB, i.uv * 40.0);
                float blend = tex2D(_Mask, i.uv).r;
                return lerp(a, b, blend);
            }
            ENDCG
        }
    }
}