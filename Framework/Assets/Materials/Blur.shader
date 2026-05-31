Shader "Lightweave/Blur"
{
    Properties
    {
        _BlurSize ("Blur Size (pixels)", Range(0, 64)) = 12
        _Color ("Tint", Color) = (1,1,1,1)
        _CornerRadius ("Corner Radius (px)", Float) = 0
        _RectSize ("Rect Size (px)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Cull Off
        Lighting Off

        GrabPass
        {
            "_LW_GrabH"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _LW_GrabH;
            float4 _LW_GrabH_TexelSize;
            float _BlurSize;
            float _CornerRadius;
            float4 _RectSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 grabPos : TEXCOORD0; float2 uv : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Uniform-radius rounded-rect coverage in [0,1]. Symmetric in both axes,
            // so it is invariant to the texcoord y-flip that differs across graphics
            // APIs. Returns 1 everywhere when no radius is set (square callers).
            float lwRoundedMask(float2 uv)
            {
                if (_CornerRadius <= 0.5) return 1.0;
                float2 p = (uv - 0.5) * _RectSize.xy;
                float2 b = _RectSize.xy * 0.5 - _CornerRadius;
                float2 q = abs(p) - b;
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _CornerRadius;
                float aa = fwidth(d) + 1e-4;
                return saturate(0.5 - d / aa);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grabPos.xy / i.grabPos.w;
                float jitter = 0.9 + 0.2 * hash21(i.vertex.xy);
                float2 step = float2(_LW_GrabH_TexelSize.x * (_BlurSize / 3.0) * jitter, 0);

                fixed4 sum  = tex2D(_LW_GrabH, uv) * 0.1597;
                sum += (tex2D(_LW_GrabH, uv + step) + tex2D(_LW_GrabH, uv - step)) * 0.1474;
                sum += (tex2D(_LW_GrabH, uv + step * 2.0) + tex2D(_LW_GrabH, uv - step * 2.0)) * 0.1159;
                sum += (tex2D(_LW_GrabH, uv + step * 3.0) + tex2D(_LW_GrabH, uv - step * 3.0)) * 0.0777;
                sum += (tex2D(_LW_GrabH, uv + step * 4.0) + tex2D(_LW_GrabH, uv - step * 4.0)) * 0.0444;
                sum += (tex2D(_LW_GrabH, uv + step * 5.0) + tex2D(_LW_GrabH, uv - step * 5.0)) * 0.0216;
                sum += (tex2D(_LW_GrabH, uv + step * 6.0) + tex2D(_LW_GrabH, uv - step * 6.0)) * 0.0090;
                sum += (tex2D(_LW_GrabH, uv + step * 7.0) + tex2D(_LW_GrabH, uv - step * 7.0)) * 0.0032;
                sum += (tex2D(_LW_GrabH, uv + step * 8.0) + tex2D(_LW_GrabH, uv - step * 8.0)) * 0.0010;
                sum.a = lwRoundedMask(i.uv);
                return sum;
            }
            ENDCG
        }

        GrabPass
        {
            "_LW_GrabV"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _LW_GrabV;
            float4 _LW_GrabV_TexelSize;
            float _BlurSize;
            float4 _Color;
            float _CornerRadius;
            float4 _RectSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 grabPos : TEXCOORD0; float2 uv : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float lwRoundedMask(float2 uv)
            {
                if (_CornerRadius <= 0.5) return 1.0;
                float2 p = (uv - 0.5) * _RectSize.xy;
                float2 b = _RectSize.xy * 0.5 - _CornerRadius;
                float2 q = abs(p) - b;
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _CornerRadius;
                float aa = fwidth(d) + 1e-4;
                return saturate(0.5 - d / aa);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grabPos.xy / i.grabPos.w;
                float jitter = 0.9 + 0.2 * hash21(i.vertex.yx);
                float2 step = float2(0, _LW_GrabV_TexelSize.y * (_BlurSize / 3.0) * jitter);

                fixed4 sum  = tex2D(_LW_GrabV, uv) * 0.1597;
                sum += (tex2D(_LW_GrabV, uv + step) + tex2D(_LW_GrabV, uv - step)) * 0.1474;
                sum += (tex2D(_LW_GrabV, uv + step * 2.0) + tex2D(_LW_GrabV, uv - step * 2.0)) * 0.1159;
                sum += (tex2D(_LW_GrabV, uv + step * 3.0) + tex2D(_LW_GrabV, uv - step * 3.0)) * 0.0777;
                sum += (tex2D(_LW_GrabV, uv + step * 4.0) + tex2D(_LW_GrabV, uv - step * 4.0)) * 0.0444;
                sum += (tex2D(_LW_GrabV, uv + step * 5.0) + tex2D(_LW_GrabV, uv - step * 5.0)) * 0.0216;
                sum += (tex2D(_LW_GrabV, uv + step * 6.0) + tex2D(_LW_GrabV, uv - step * 6.0)) * 0.0090;
                sum += (tex2D(_LW_GrabV, uv + step * 7.0) + tex2D(_LW_GrabV, uv - step * 7.0)) * 0.0032;
                sum += (tex2D(_LW_GrabV, uv + step * 8.0) + tex2D(_LW_GrabV, uv - step * 8.0)) * 0.0010;

                fixed4 col = sum * _Color;
                col.a *= lwRoundedMask(i.uv);
                return col;
            }
            ENDCG
        }
    }
}
