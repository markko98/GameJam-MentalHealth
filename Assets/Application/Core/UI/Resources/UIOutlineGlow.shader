// Silhouette outline + glow taken from a sprite's alpha, for UIOutlineGlow.
// Lower DIRECTIONS / BAND_RINGS / GLOW_RINGS if the effect ever costs too much.
Shader "UI/Outline Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SpriteUV ("Sprite UV (min xy, size zw)", Vector) = (0, 0, 1, 1)
        _Shape ("Outline, Softness, Glow, Glow Strength", Vector) = (0.02, 0.005, 0.08, 0.65)
        _Extra ("Aspect, Inner Fill", Vector) = (1, 0, 0, 0)
        _AlphaThreshold ("Alpha Threshold", Range(0.01, 0.99)) = 0.35
        _GlowPower ("Glow Falloff", Range(0.5, 4)) = 1.6

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define DIRECTIONS 12
            #define BAND_RINGS 3
            #define GLOW_RINGS 3
            #define TAU 6.28318530718

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _SpriteUV;
            float4 _Shape;
            float4 _Extra;
            float _AlphaThreshold;
            float _GlowPower;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            // Alpha of the icon at a point in sprite space; there is nothing outside the sprite.
            float SilhouetteAlpha(float2 sp)
            {
                float inside = step(0.0, sp.x) * step(0.0, sp.y) * step(sp.x, 1.0) * step(sp.y, 1.0);
                float2 uv = _SpriteUV.xy + clamp(sp, 0.0, 1.0) * _SpriteUV.zw;
                return (tex2Dlod(_MainTex, float4(uv, 0, 0)) + _TextureSampleAdd).a * inside;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                float2 uvSize = max(_SpriteUV.zw, float2(0.000001, 0.000001));
                float2 sp = (IN.texcoord - _SpriteUV.xy) / uvSize;

                // Widths are in sprite-space x units; y is corrected by the sprite's aspect so a
                // circle in pixels stays a circle.
                float2 axis = float2(1.0, max(_Extra.x, 0.0001));

                float outlineSp = _Shape.x;
                float softSp = max(_Shape.y, 0.00001);
                float glowSp = _Shape.z;
                float glowStrength = _Shape.w;

                float bandMax = outlineSp + softSp;
                float reach = bandMax + glowSp;

                float centerAlpha = SilhouetteAlpha(sp);
                float isInside = step(_AlphaThreshold, centerAlpha);

                // Crisp band: distance to the silhouette, searched outward in fine rings.
                float distanceSp = bandMax;
                if (isInside > 0.0)
                {
                    distanceSp = 0.0;
                }
                else
                {
                    for (int band = 1; band <= BAND_RINGS; band++)
                    {
                        float t = (float)band / BAND_RINGS;
                        float hit = 0.0;

                        for (int d = 0; d < DIRECTIONS; d++)
                        {
                            float angle = (TAU * d) / DIRECTIONS;
                            float2 offset = float2(cos(angle), sin(angle)) * (t * bandMax) * axis;
                            hit = max(hit, step(_AlphaThreshold, SilhouetteAlpha(sp + offset)));
                        }

                        if (hit > 0.0)
                        {
                            distanceSp = t * bandMax;
                            break;
                        }
                    }
                }

                float outline = 1.0 - smoothstep(outlineSp - softSp, outlineSp + softSp, distanceSp);

                // Soft halo: a weighted average of the silhouette around this pixel, so no ring banding.
                float glow = 0.0;
                if (glowSp > 0.0 && glowStrength > 0.0 && isInside <= 0.0)
                {
                    float sum = 0.0;
                    float weights = 0.0;

                    for (int ring = 1; ring <= GLOW_RINGS; ring++)
                    {
                        float t = (float)ring / GLOW_RINGS;
                        float weight = 1.0 - t * 0.75;

                        for (int g = 0; g < DIRECTIONS; g++)
                        {
                            float angle = (TAU * g) / DIRECTIONS;
                            float2 offset = float2(cos(angle), sin(angle)) * (t * reach) * axis;
                            sum += SilhouetteAlpha(sp + offset) * weight;
                            weights += weight;
                        }
                    }

                    glow = weights > 0.0 ? sum / weights : 0.0;
                    glow = pow(saturate(glow), _GlowPower);
                }

                // Inside the silhouette only Inner Fill shows, so the outline can sit over the icon.
                float insideSoft = smoothstep(0.0, _AlphaThreshold, centerAlpha);
                float ring2 = max(outline, glow * glowStrength);

                half4 color = IN.color;
                color.a *= saturate(ring2 * (1.0 - insideSoft) + insideSoft * _Extra.y);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Premultiplied, so the material can switch to additive with Dst Blend = One.
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
