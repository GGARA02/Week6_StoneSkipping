Shader "ScreenSuction/Cloth"
{
    Properties
    {
        _MainTex ("Captured Frame", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _SuctionCenter;
            float _Progress, _Aspect, _SuctionStrength, _SuctionRadius, _Falloff;
            float _StretchStrength, _WrinkleStrength, _TwistStrength, _FinalCollapseSpeed;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float shade : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 delta = (input.uv - _SuctionCenter) * float2(_Aspect, 1.0);
                float radius = length(delta);
                float angle = atan2(delta.y, delta.x);
                float p = saturate(_Progress);
                // Near points yield first; distant corners hold, then accelerate independently.
                float distancePhase = pow(saturate(radius / max(_SuctionRadius, 0.01)), 1.0 / _Falloff);
                float delay = 0.36 * distancePhase + 0.045 * distancePhase * sin(angle * 3.0 + radius * 5.0);
                float local = saturate((p - delay) / (1.0 - delay));
                float pull = 1.0 - pow(1.0 - local * local, _SuctionStrength);
                float envelope = sin(local * UNITY_PI);
                float fold = sin(angle * 11.0 + radius * 8.0 - local * 4.0);
                float ribs = sin(angle * 19.0 - radius * 5.0 + local * 3.0);
                float wrinkle = _WrinkleStrength * envelope * (fold + 0.35 * ribs);
                float deformedRadius = radius * (1.0 - pull) * (1.0 + wrinkle);
                float twist = _TwistStrength * envelope * (1.0 - distancePhase) * sin(radius * 6.0);
                float2 direction = float2(cos(angle + twist), sin(angle + twist));
                float2 displaced = direction * deformedRadius;

                // Gather the sheet across its width, leaving a long creased funnel along its length.
                float gather = smoothstep(0.18, 0.86, p);
                float2 axis = float2((0.5 - _SuctionCenter.x) * _Aspect + 0.16, 0.5 - _SuctionCenter.y + 0.28);
                float axisLength = length(axis);
                axis = axisLength > 0.001 ? axis / axisLength : float2(0.0, 1.0);
                float2 across = float2(-axis.y, axis.x);
                float along = dot(displaced, axis);
                float width = dot(displaced, across);
                float taper = lerp(0.1, 1.0, saturate(deformedRadius / max(_SuctionRadius, 0.01)));
                width *= lerp(1.0, taper / (1.0 + _StretchStrength * 5.0), gather);
                along += _StretchStrength * 0.13 * gather * envelope * deformedRadius;
                displaced = axis * along + across * width;

                // The last remnant snaps into the exact contact-frame viewport position.
                float collapse = pow(1.0 - smoothstep(0.82, 1.0, p), _FinalCollapseSpeed);
                float2 destination = _SuctionCenter + displaced * collapse / float2(_Aspect, 1.0);
                output.position = UnityObjectToClipPos(input.vertex);
                output.position.xy += (destination - input.uv) * 2.0 * output.position.w;
                output.uv = input.uv;
                output.shade = 1.0 - 0.32 * envelope * gather * (0.5 + 0.5 * fold);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0) uv.y = 1.0 - uv.y;
                #endif
                half4 color = tex2D(_MainTex, saturate(uv));
                color.rgb *= input.shade;
                color.a = 1.0 - smoothstep(0.975, 1.0, _Progress);
                return color;
            }
            ENDHLSL
        }
    }
}
