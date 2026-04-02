Shader "Skybox/Panoramic_Upgraded"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Panoramic Texture", 2D) = "grey" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        [Toggle] _ImageType180 ("Image Type 180", Float) = 1
        _HorizontalOffset ("Horizontal Offset", Range(-1, 1)) = 0
        _VerticalOffset ("Vertical Offset", Range(-1, 1)) = 0
        _TilingX ("Tiling X", Range(0.25, 4)) = 1
        _TilingY ("Tiling Y", Range(0.25, 4)) = 1
        [KeywordEnum(Equirectangular, FisheyeEquidistant)] _Projection ("Projection", Float) = 0
        _FisheyeFov ("Fisheye FOV (deg)", Range(90, 220)) = 180
        _FisheyeCenter ("Fisheye Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _FisheyeRadius ("Fisheye Radius (UV)", Range(0.1, 1.0)) = 0.5
        _FisheyeWarp ("Fisheye Warp", Range(0.25, 4)) = 1
        [Toggle] _ShowBackFor180 ("Show Backside For 180", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Tint;
            half _Exposure;
            float _Rotation;
            float _ImageType180;
            float _HorizontalOffset;
            float _VerticalOffset;
            float _TilingX;
            float _TilingY;
            float _Projection;
            float _FisheyeFov;
            float4 _FisheyeCenter;
            float _FisheyeRadius;
            float _FisheyeWarp;
            float _ShowBackFor180;

            static const float INV_PI = 0.31830988618;
            static const float INV_TWO_PI = 0.15915494309;
            static const float EPS = 1e-6;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float3 RotateY(float3 dir, float degrees)
            {
                float angle = radians(degrees);
                float s = sin(angle);
                float c = cos(angle);
                return float3(
                    c * dir.x + s * dir.z,
                    dir.y,
                    -s * dir.x + c * dir.z
                );
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Use world space direction from camera to the surface point.
                float3 dir = RotateY(normalize(i.worldPos - _WorldSpaceCameraPos), _Rotation);

                float2 uv;
                if (_Projection < 0.5)
                {
                    // Equirectangular mapping.
                    float u = atan2(dir.x, dir.z) * INV_TWO_PI + 0.5;
                    float v = asin(clamp(dir.y, -1.0, 1.0)) * INV_PI + 0.5;
                    uv = float2(u, v);

                    // ImageType 180: use only front hemisphere, then remap to full texture width.
                    if (_ImageType180 > 0.5)
                    {
                        if (dir.z < 0.0 && _ShowBackFor180 < 0.5)
                        {
                            return fixed4(0, 0, 0, 1);
                        }
                        uv.x = frac((uv.x - 0.25) * 2.0);
                    }
                }
                else
                {
                    // Fisheye (equidistant) mapping for front hemisphere.
                    if (dir.z < 0.0 && _ShowBackFor180 < 0.5)
                    {
                        return fixed4(0, 0, 0, 1);
                    }

                    float fovRad = radians(_FisheyeFov);
                    float theta = acos(clamp(dir.z, -1.0, 1.0));          // 0..pi
                    float r = theta / max(fovRad * 0.5, EPS);             // 0..~1 for 180
                    r = pow(saturate(r), _FisheyeWarp);

                    float2 xy = dir.xy;
                    float lenXY = max(length(xy), EPS);
                    float2 n = xy / lenXY;

                    uv = _FisheyeCenter.xy + n * (r * _FisheyeRadius);
                }

                uv = uv + float2(_HorizontalOffset, _VerticalOffset);

                float2 tiling = max(float2(_TilingX, _TilingY), 0.0001);
                uv = (uv - 0.5) / tiling + 0.5;

                uv = frac(uv);

                fixed4 tex = tex2D(_MainTex, uv);
                fixed3 rgb = tex.rgb * _Tint.rgb * _Exposure;
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
