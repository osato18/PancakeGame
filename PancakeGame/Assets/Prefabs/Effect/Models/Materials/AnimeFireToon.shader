// Blender の AnimeFire_Toon マテリアル（レイヤーウェイト Facing → カラーランプ「一定」→ 放射）を URP で再現した Unlit シェーダー
Shader "DesktopPC/AnimeFireToon"
{
    Properties
    {
        _C0 ("Core (白)", Color) = (1, 0.973, 0.863, 1)
        _C1 ("Yellow", Color) = (1, 0.882, 0.29, 1)
        _C2 ("Orange", Color) = (1, 0.635, 0.122, 1)
        _C3 ("Red Orange", Color) = (1, 0.353, 0.118, 1)
        _C4 ("Edge (濃い赤)", Color) = (0.788, 0.137, 0.102, 1)
        _T1 ("Threshold Yellow", Range(0, 1)) = 0.14
        _T2 ("Threshold Orange", Range(0, 1)) = 0.34
        _T3 ("Threshold Red Orange", Range(0, 1)) = 0.56
        _T4 ("Threshold Edge", Range(0, 1)) = 0.76
        _Blend ("Layer Weight Blend", Range(0, 0.99)) = 0.7
        _NoiseScale ("Noise Scale", Float) = 4
        _NoiseAmount ("Noise Amount", Float) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _C0, _C1, _C2, _C3, _C4;
                float _T1, _T2, _T3, _T4, _Blend, _NoiseScale, _NoiseAmount;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionOS = i.positionOS.xyz;
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // 境目を不規則にするための値ノイズ（Blender のノイズテクスチャの代わり）
            float ValueNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x),
                         lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                         lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                // Blender のレイヤーウェイト「前方向」：正面 0 → 輪郭 1。ブレンドは指数に変換
                float fac = 1.0 - abs(dot(n, v));
                float b = clamp(_Blend, 0.0, 0.99999);
                b = (b < 0.5) ? 2.0 * b : 0.5 / (1.0 - b);
                fac = pow(max(fac, 1e-5), b);
                fac = saturate(fac + (ValueNoise(i.positionOS * _NoiseScale) - 0.5) * _NoiseAmount);
                // カラーランプ「一定」
                half3 c = _C0.rgb;
                c = fac >= _T1 ? _C1.rgb : c;
                c = fac >= _T2 ? _C2.rgb : c;
                c = fac >= _T3 ? _C3.rgb : c;
                c = fac >= _T4 ? _C4.rgb : c;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
