// A simple, bright, cartoon sea. Transparent blue that gets deeper at glancing angles, with soft white ripple
// lines that drift across the surface (the pattern is drawn in world space, so every water box lines up).
// No depth texture or special URP settings needed. Used for the sea, the pool and the secret room's flood.
Shader "Badeland/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.75, 1.0, 0.65)
        _DeepColor ("Deep Color", Color) = (0.03, 0.3, 0.85, 0.85)
        _RippleColor ("Ripple Color", Color) = (1, 1, 1, 1)
        _RippleScale ("Ripple Scale", Float) = 0.3
        _RippleSpeed ("Ripple Speed", Float) = 0.25
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _RippleColor;
                float _RippleScale;
                float _RippleSpeed;
                float _RippleStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                return output;
            }

            // Three crossing waves. Where their sum is near zero the surface "catches the light": thin wobbly lines.
            float RippleField(float2 p, float t)
            {
                float a = sin(p.x * 6.2831 + t);
                float b = sin(p.y * 6.2831 - t * 0.8);
                float c = sin((p.x + p.y) * 4.4 + t * 1.3);
                float d = sin((p.x - p.y) * 3.1 - t * 0.6);
                return a + b + c + d;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz * _RippleScale;
                float t = _Time.y * _RippleSpeed * 6.2831;

                float field = RippleField(p, t);
                float lines = 1.0 - smoothstep(0.0, 0.35, abs(field));

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float glancing = pow(1.0 - saturate(viewDir.y), 2.5);

                half4 color = lerp(_ShallowColor, _DeepColor, glancing);
                color.rgb += lines * _RippleStrength * _RippleColor.rgb;
                color.a = saturate(color.a + lines * 0.12);
                return color;
            }
            ENDHLSL
        }
    }
}
