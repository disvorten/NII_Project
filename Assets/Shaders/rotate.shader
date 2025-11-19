Shader "Custom/rotate"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        // Волшебные свойства для инспектора
        [Toggle] _RotateCW90("Rotate Clockwise 90", Float) = 0.0
        [Toggle] _Rotate180("Rotate 180", Float) = 0.0
        ////////////////////////////////////
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType" = "Plane" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Флаги для компиляции нескольких вариантов шейдера
            #pragma multi_compile __ _ROTATECW90_ON
            #pragma multi_compile __ _ROTATE180_ON
            ////////////////////////////////////////////////////
            
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {

                // Собственно код вращения uv
#if _ROTATECW90_ON
                i.uv = float2(1 - i.uv.y, i.uv.x);
#endif
#if _ROTATE180_ON
                i.uv = float2(1 - i.uv.x, 1 - i.uv.y);
#endif
                /////////////////////////////

                fixed4 color = tex2D(_MainTex, i.uv);
                return color;
            }
            ENDCG
        }
    }
}
