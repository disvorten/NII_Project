Shader "Custom/RotateNew"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        
        // Угол вращения в градусах
        _RotationAngle("Rotation Angle", Range(0, 360)) = 0.0
        [Toggle] _UseRotation("Use Rotation", Float) = 0.0
        
        // Оригинальные тогглы для обратной совместимости (можно удалить если не нужны)
        [Toggle] _RotateCW90("Rotate Clockwise 90 (Deprecated)", Float) = 0.0
        [Toggle] _Rotate180("Rotate 180 (Deprecated)", Float) = 0.0
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

            // Флаги для компиляции
            #pragma multi_compile __ _USEROTATION_ON
            #pragma multi_compile __ _ROTATECW90_ON
            #pragma multi_compile __ _ROTATE180_ON
            
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
            float _RotationAngle;
            
            // Функция вращения UV координат
            float2 rotateUV(float2 uv, float angle)
            {
                // Переводим угол в радианы
                float rad = radians(angle);
                
                // Центр текстуры
                float2 center = float2(0.5, 0.5);
                
                // Смещаем UV к центру, вращаем, возвращаем на место
                uv -= center;
                
                float s = sin(rad);
                float c = cos(rad);
                
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                uv = mul(rotationMatrix, uv);
                
                uv += center;
                
                return uv;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Новое вращение по заданному углу
                #if _USEROTATION_ON
                    uv = rotateUV(uv, _RotationAngle);
                #endif
                
                // Старое вращение на 90 градусов (для обратной совместимости)
                #if _ROTATECW90_ON
                    uv = float2(1 - uv.y, uv.x);
                #endif
                
                // Старое вращение на 180 градусов (для обратной совместимости)
                #if _ROTATE180_ON
                    uv = float2(1 - uv.x, 1 - uv.y);
                #endif

                fixed4 color = tex2D(_MainTex, uv);
                return color;
            }
            ENDCG
        }
    }
    
    CustomEditor "RotateShaderGUI"
}