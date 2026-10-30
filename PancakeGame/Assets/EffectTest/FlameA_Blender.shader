// Blender の材質「炎A_青」の透明感を Unity で再現する URP 用シェーダー
// Blender 側の式:  fac = 高さ(Generated座標Z) - ノイズ(4D FBM, Scale4, Detail3, Roughness0.5, W=frame/6) * 0.5
//   色   = カラーランプ(fac)
//   透明 = pow(clamp((0.98 - fac) / (0.98 - 0.05), 0, 1), 1.2)   ← 範囲マッピング(0.98→0, 0.05→1) と 累乗1.2
//   最終 = 透過BSDF と 放射 を上の値でミックス（通常のアルファブレンドと同じ）
// ノイズは Blender の 4D ノイズと同一ではなく、3D バリューノイズ FBM を時間でスクロールして近似している。
Shader "FlameA/BlenderMatch"
{
    Properties
    {
        _Intensity ("発光の強さ (Blenderは3.0。Unityは0〜1の表示のため1.0から調整)", Float) = 1.0
        _NoiseScale ("ノイズのスケール (Blender: 4)", Float) = 4.0
        _NoiseSpeed ("ノイズの速さ (Blender: frame/6 = 24fpsで毎秒4)", Float) = 4.0
        _NoiseStrength ("ノイズの強さ (Blender: 0.5)", Float) = 0.5
        _FadeStart ("フェード開始 (Blender: 0.05)", Float) = 0.05
        _FadeEnd ("フェード終了 (Blender: 0.98)", Float) = 0.98
        _FadePower ("フェードの累乗 (Blender: 1.2)", Float) = 1.2
        _BBoxMin ("ジェットの外形 最小 xy (Blender実測)", Vector) = (-0.028, -0.027, 0, 0)
        _BBoxSize ("ジェットの外形 大きさ xy (Blender実測)", Vector) = (0.062, 0.054, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity, _NoiseScale, _NoiseSpeed, _NoiseStrength, _FadeStart, _FadeEnd, _FadePower;
                float4 _BBoxMin, _BBoxSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 gen : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                // Blender の「生成(Generated)」座標: 外形を 0〜1 に正規化。Z は UV の V（高さ）を使う
                float2 xy = (v.positionOS.xy - _BBoxMin.xy) / _BBoxSize.xy;
                o.gen = float3(xy, v.uv.y);
                return o;
            }

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float vnoise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = lerp(lerp(hash31(i),                  hash31(i + float3(1,0,0)), f.x),
                               lerp(hash31(i + float3(0,1,0)),  hash31(i + float3(1,1,0)), f.x), f.y);
                float b = lerp(lerp(hash31(i + float3(0,0,1)),  hash31(i + float3(1,0,1)), f.x),
                               lerp(hash31(i + float3(0,1,1)),  hash31(i + float3(1,1,1)), f.x), f.y);
                return lerp(a, b, f.z);
            }
            // Detail 3 (=4オクターブ相当は多いので3層)、Roughness 0.5、Lacunarity 2。正規化して 0.5 中心
            float fbm(float3 p)
            {
                float sum = 0, amp = 1, norm = 0;
                for (int k = 0; k < 3; k++) { sum += vnoise(p) * amp; norm += amp; p *= 2.0; amp *= 0.5; }
                return sum / norm;
            }
            // Blender のカラーランプ（線形補間、scene-linear の値そのまま）
            float3 ramp(float f)
            {
                const float3 c0 = float3(0.10, 0.30, 0.95);
                const float3 c1 = float3(0.20, 0.42, 1.00);
                const float3 c2 = float3(0.35, 0.55, 1.00);
                const float3 c3 = float3(1.00, 0.08, 0.02);
                if (f <= 0.16) return lerp(c0, c1, saturate(f / 0.16));
                if (f <= 0.30) return lerp(c1, c2, saturate((f - 0.16) / 0.14));
                if (f <= 0.42) return lerp(c2, c3, saturate((f - 0.30) / 0.12));
                return c3;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _NoiseSpeed;                       // W = frame/6
                float n = fbm(i.gen * _NoiseScale + float3(0.0, 0.0, t * 0.5) + t * float3(0.37, 0.21, 0.0));
                float fac = i.gen.z - n * _NoiseStrength;               // 数式(SUBTRACT)
                float a = saturate((_FadeEnd - fac) / (_FadeEnd - _FadeStart));   // 範囲マッピング(クランプ)
                a = pow(a, _FadePower);                                  // 数式(POWER 1.2)
                float3 col = ramp(saturate(fac)) * _Intensity;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
