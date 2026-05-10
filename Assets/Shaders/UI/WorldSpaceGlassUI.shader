Shader "UI/World Space Glass"
{
    Properties
    {
        [Header(Base)]
        _Color ("Tint", Color) = (1,1,1,0.65)
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Light)]
        _LightAngle ("Light Angle (deg)", Float) = -51
        _LightIntensity ("Light Intensity", Range(0, 1)) = 0.35

        [Header(Refraction)]
        _Refraction ("Refraction", Range(0, 1)) = 0.12

        [Header(Depth)]
        _DepthScale ("Depth", Range(0, 2)) = 0.35

        [Header(Dispersion)]
        _Dispersion ("Dispersion", Range(0, 10)) = 2.5

        [Header(Frost)]
        _Frost ("Frost", Range(0, 1)) = 0.45

        [Header(Splay)]
        _Splay ("Splay", Range(0, 3)) = 1

        _WorldSpaceGlass_BG ("", 2D) = "black" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float2 canvasLocal : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;

            sampler2D _WorldSpaceGlass_BG;
            float4 _WorldSpaceGlass_BG_TexelSize;

            float _LightAngle;
            float _LightIntensity;
            float _Refraction;
            float _DepthScale;
            float _Dispersion;
            float _Frost;
            float _Splay;

            float4 _ClipRect;
            fixed4 _TextureSampleAdd;

            float2 GlassScreenUV(float4 worldPos)
            {
                float4 clip = mul(UNITY_MATRIX_VP, worldPos);
                float2 uv = clip.xy / max(clip.w, 1e-5);
                uv = uv * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif
                return uv;
            }

            float2 RefractScreenOffset(float3 worldNormal, float3 viewDir, float strength)
            {
                float3 n = normalize(worldNormal);
                float3 v = normalize(viewDir);
                float3 refr = refract(-v, n, 0.9);
                return refr.xy * strength;
            }

            void BuildLightBasis(float3 worldN, out float3 t, out float3 b)
            {
                float3 up = abs(worldN.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                t = normalize(cross(up, worldN));
                b = normalize(cross(worldN, t));
            }

            float2 DispersionDir(float3 worldN)
            {
                float rad = _LightAngle * UNITY_PI / 180.0;
                float3 t, b;
                BuildLightBasis(worldN, t, b);
                float2 d = cos(rad) * t.xz + sin(rad) * b.xz;
                return normalize(d + 1e-5);
            }

            fixed4 FrostSample(float2 uv)
            {
                float2 px = _WorldSpaceGlass_BG_TexelSize.xy;
                float r = _Frost * _Splay * 5.0;
                fixed4 c = tex2D(_WorldSpaceGlass_BG, uv);
                c += tex2D(_WorldSpaceGlass_BG, uv + float2(r, 0) * px);
                c += tex2D(_WorldSpaceGlass_BG, uv - float2(r, 0) * px);
                c += tex2D(_WorldSpaceGlass_BG, uv + float2(0, r) * px);
                c += tex2D(_WorldSpaceGlass_BG, uv - float2(0, r) * px);
                c += tex2D(_WorldSpaceGlass_BG, uv + float2(r, r) * px * 0.65);
                c += tex2D(_WorldSpaceGlass_BG, uv - float2(r, r) * px * 0.65);
                return c / 7.0;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 wPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wPos;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.canvasLocal = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 worldN = normalize(i.worldNormal);
                float3 worldPos = i.worldPos.xyz;
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                float depthMul = 1.0 + _DepthScale;
                float2 baseUV = GlassScreenUV(i.worldPos);
                float2 refr = RefractScreenOffset(worldN, viewDir, _Refraction * depthMul);
                float2 uv = baseUV + refr;

                float2 dispDir = DispersionDir(worldN);
                float dispPx = (_Dispersion / 10.0) * 0.014 * _Splay * depthMul;
                float2 uvR = uv + dispDir * dispPx * 1.2;
                float2 uvG = uv;
                float2 uvB = uv - dispDir * dispPx * 1.2;

                fixed4 bg;
                bg.r = FrostSample(uvR).r;
                bg.g = FrostSample(uvG).g;
                bg.b = FrostSample(uvB).b;
                bg.a = 1.0;

                float rad = _LightAngle * UNITY_PI / 180.0;
                float3 t, b;
                BuildLightBasis(worldN, t, b);
                float3 L = normalize(cos(rad) * t + sin(rad) * b);
                float3 V = viewDir;
                float3 R = reflect(-V, worldN);
                float spec = pow(saturate(dot(R, L)), 40.0) * _LightIntensity * 2.5;
                float3 lit = bg.rgb + spec;

                fixed4 col = fixed4(lit, i.color.a);
                col *= tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.canvasLocal, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
