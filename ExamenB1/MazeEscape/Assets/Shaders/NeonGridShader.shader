Shader "Custom/NeonGridShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0, 1, 1, 1)
        _GridSize ("Grid Size", Float) = 10
        _Speed ("Scroll Speed", Float) = 1.0
    }
    SubShader
    {
        Tags {"Queue"="Background"}
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float _GridSize;
            float _Speed;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                float2 grid = frac(i.uv * _GridSize - _Time.y * _Speed);
                float gridPattern = step(0.05, grid.x) + step(0.05, grid.y);
                return lerp(_MainColor, float4(0, 0, 0, 1), gridPattern);
            }
            ENDCG
        }
    }
}
