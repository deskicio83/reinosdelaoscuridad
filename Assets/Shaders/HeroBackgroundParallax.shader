Shader "Custom/HeroBackgroundParallax"
{
    Properties
    {
        _MainTex ("Fondo Principal", 2D) = "white" {}
        _SmokeTex ("Textura Humo", 2D) = "black" {}
        _ParallaxStrength ("Intensidad Parallax", Range(0,0.2)) = 0.05
        _SmokeSpeedX ("Velocidad Humo X", Range(-1,1)) = 0.05
        _SmokeSpeedY ("Velocidad Humo Y", Range(-1,1)) = 0.02
        _SmokeIntensity ("Intensidad Humo", Range(0,1)) = 0.3
        _OffsetX ("Offset X", Float) = 0
        _OffsetY ("Offset Y", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _SmokeTex;
            float4 _SmokeTex_ST;

            float _ParallaxStrength;
            float _SmokeSpeedX;
            float _SmokeSpeedY;
            float _SmokeIntensity;
            float _OffsetX;
            float _OffsetY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // === Fondo con scroll lineal continuo ===
                float2 uv = i.uv;
                float2 finalUV = uv + float2(_OffsetX, _OffsetY) * _ParallaxStrength;

                // Fondo principal
                fixed4 col = tex2D(_MainTex, finalUV);

                // === Humo animado (scroll continuo) ===
                float2 smokeUV = i.uv + float2(_Time.y * _SmokeSpeedX, _Time.y * _SmokeSpeedY);
                fixed4 smoke = tex2D(_SmokeTex, smokeUV);

                // Mezcla humo con el fondo
                col.rgb = lerp(col.rgb, col.rgb * smoke.rgb, _SmokeIntensity);

                return col;
            }
            ENDCG
        }
    }
}
