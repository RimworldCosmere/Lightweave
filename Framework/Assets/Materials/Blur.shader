Shader "Lightweave/Blur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BlurStep ("Blur Step (texels)", Float) = 1.2
        _Color ("Tint", Color) = (1,1,1,1)
        _CornerRadius ("Corner Radius (px)", Float) = 0
        _RectSize ("Rect Size (px)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _BlurStep;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

        v2f vertFull(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        // Separable sigma=2.5 truncated Gaussian (9 taps, weights sum ~1.0).
        fixed4 lwGaussian(float2 uv, float2 dir)
        {
            fixed4 sum  = tex2D(_MainTex, uv) * 0.1597;
            sum += (tex2D(_MainTex, uv + dir) + tex2D(_MainTex, uv - dir)) * 0.1474;
            sum += (tex2D(_MainTex, uv + dir * 2.0) + tex2D(_MainTex, uv - dir * 2.0)) * 0.1159;
            sum += (tex2D(_MainTex, uv + dir * 3.0) + tex2D(_MainTex, uv - dir * 3.0)) * 0.0777;
            sum += (tex2D(_MainTex, uv + dir * 4.0) + tex2D(_MainTex, uv - dir * 4.0)) * 0.0444;
            sum += (tex2D(_MainTex, uv + dir * 5.0) + tex2D(_MainTex, uv - dir * 5.0)) * 0.0216;
            sum += (tex2D(_MainTex, uv + dir * 6.0) + tex2D(_MainTex, uv - dir * 6.0)) * 0.0090;
            sum += (tex2D(_MainTex, uv + dir * 7.0) + tex2D(_MainTex, uv - dir * 7.0)) * 0.0032;
            sum += (tex2D(_MainTex, uv + dir * 8.0) + tex2D(_MainTex, uv - dir * 8.0)) * 0.0010;
            return sum;
        }
        ENDCG

        // Pass 0: horizontal
        Pass
        {
            CGPROGRAM
            #pragma vertex vertFull
            #pragma fragment frag
            #pragma target 3.0
            fixed4 frag(v2f i) : SV_Target
            {
                float2 dir = float2(_MainTex_TexelSize.x * _BlurStep, 0);
                return lwGaussian(i.uv, dir);
            }
            ENDCG
        }

        // Pass 1: vertical
        Pass
        {
            CGPROGRAM
            #pragma vertex vertFull
            #pragma fragment frag
            #pragma target 3.0
            fixed4 frag(v2f i) : SV_Target
            {
                float2 dir = float2(0, _MainTex_TexelSize.y * _BlurStep);
                return lwGaussian(i.uv, dir);
            }
            ENDCG
        }

        // Pass 2: composite (tint + rounded mask), drawn into the surface rect.
        // Samples the full-screen blurred RT by SCREEN position (ComputeGrabScreenPos),
        // which auto-handles the platform Y-flip, so the glass shows exactly the blurred
        // backdrop behind it. i.uv (0..1 across the quad) drives only the rounded mask.
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vertC
            #pragma fragment frag
            #pragma target 3.0

            float4 _Color;
            float _CornerRadius;
            float4 _RectSize;
            float4 _SubRectUV;

            struct appdataC { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2fC { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2fC vertC(appdataC v)
            {
                v2fC o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float lwRoundedMask(float2 localUv)
            {
                if (_CornerRadius <= 0.5) return 1.0;
                float2 p = (localUv - 0.5) * _RectSize.xy;
                float2 b = _RectSize.xy * 0.5 - _CornerRadius;
                float2 q = abs(p) - b;
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _CornerRadius;
                float aa = fwidth(d) + 1e-4;
                return saturate(0.5 - d / aa);
            }

            fixed4 frag(v2fC i) : SV_Target
            {
                float2 suv = _SubRectUV.xy + i.uv * _SubRectUV.zw;
                fixed3 rgb = tex2D(_MainTex, suv).rgb * _Color.rgb;
                float a = _Color.a * lwRoundedMask(i.uv);
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
