// 黒いにじみ（下から立ち上がる） Blender の BlackSmudge_03_Rise を移植したもの。
// 円筒メッシュ用 / URP / テクスチャ不要。_Intensity を 0〜1 で動かすと出し入れできる。
Shader "DebuffEffect/BlackSmudge_Rise"
{
    Properties
    {
        _Intensity("Intensity (0-1)", Range(0, 1)) = 1
        _Color("Mist Color", Color) = (0.312, 0.105, 0.528, 1)
        _FlowSpeed("Rise Speed", Float) = 1.6
        _Evolve("Pattern Change Speed", Float) = 0.16
        _NoiseScale("Noise Scale XYZ", Vector) = (2.2, 1.5, 2.2, 0)
        _TimeOffset("Time Offset", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "BlackSmudge"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity, _FlowSpeed, _Evolve, _TimeOffset;
                float4 _Color, _NoiseScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float Noise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                return lerp(
                    lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), u.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), u.x), u.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), u.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), u.x), u.y), u.z);
            }

            float Fbm(float3 p, int octaves, float rough)
            {
                float v = 0.0, a = 1.0, sum = 0.0;
                for (int i = 0; i < octaves; i++)
                {
                    v += a * Noise(p);
                    sum += a;
                    a *= rough;
                    p = p * 2.03 + float3(17.1, 3.7, 9.2);
                }
                return v / sum;
            }

            float Ease(float a, float b, float x) { return smoothstep(a, b, x); }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float t = _Time.y + _TimeOffset;
                float3 q = i.positionOS;
                q.y -= _FlowSpeed * t;
                q *= _NoiseScale.xyz;

                // ③ 靄の模様（細かいノイズ × 大きいノイズ）
                float3 drift = float3(0.73, -0.31, 0.52);
                float fineN = Fbm(q + drift * t * _Evolve, 3, 0.4);
                float bigN = Fbm(q * 0.5 + drift * (t * _Evolve * 0.6 + 37.0), 2, 0.35);
                float fine = lerp(0.15, 1.0, Ease(0.32, 0.58, fineN));
                float big = lerp(0.55, 1.0, Ease(0.30, 0.62, bigN));

                // ④ 高さフェード（下が濃く、上で消える）
                float rawH = (i.positionOS.y + 1.0) * 0.5;
                float h = rawH + (bigN - 0.5) * 0.5;
                float height = Ease(0.0, 0.12, h) * lerp(1.0, 0.65, Ease(0.12, 0.42, h)) * (1.0 - Ease(0.42, 0.92, h));
                height *= Ease(0.0, 0.10, rawH) * (1.0 - Ease(0.90, 1.0, rawH)); // 円筒の開口部で必ず0にする

                // ⑤ 輪郭フェード（円筒の硬い縁を消す）
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float facing = 1.0 - abs(dot(normalize(i.normalWS), V));
                float fn = facing * (0.6 + bigN * 0.9);
                float edge = lerp(1.0, 0.5, Ease(0.05, 0.30, fn)) * (1.0 - Ease(0.30, 0.62, fn));

                return float4(_Color.rgb, saturate(fine * big * height * edge * _Intensity));
            }
            ENDHLSL
        }
    }
}
