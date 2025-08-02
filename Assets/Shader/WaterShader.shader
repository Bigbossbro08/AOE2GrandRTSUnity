Shader "Custom/WaterShader"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
        _Color ("Tint Color", Color) = (0.2, 0.5, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ScrollSpeed;
            fixed4 _Color;

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

                // Scroll UVs with time
                float2 offset = _ScrollSpeed.xy * _Time.y;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + offset;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv * 20.0);
                return tex * _Color;
            }
            ENDCG
        }
    }
}