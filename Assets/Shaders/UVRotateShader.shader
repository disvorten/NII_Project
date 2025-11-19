// chatGPT
Shader "Custom/UVRotateShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RotationSpeed ("Rotation Angle", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
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
            float _RotationSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Rotate UVs
                float2 centeredUV = v.uv - float2(0.5, 0.5);
                float angle = _RotationSpeed * float(0.0174);
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2x2 rotationMatrix = float2x2(cosA, -sinA, sinA, cosA);
                o.uv = mul(centeredUV, rotationMatrix) + float2(0.5, 0.5);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture with rotated UVs
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
