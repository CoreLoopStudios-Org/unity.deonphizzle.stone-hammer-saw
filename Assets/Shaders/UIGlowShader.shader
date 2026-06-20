Shader "Custom/UIGlowShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [HDR] _GlowColor ("Glow Color", Color) = (0,1,1,1)
        _GlowIntensity ("Glow Intensity", Float) = 2.0
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.05
        _BorderSpeed ("Border Speed", Float) = 2.0
        
        [HDR] _BgColor ("Background Energy Color", Color) = (0.05, 0.2, 0.2, 1)
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _PulseFrequency ("Pulse Frequency", Float) = 3.0
        
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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            #pragma target 2.0

            #include "UnityCG.cginc"
            #pragma shader_feature_local_fragment UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            // Custom Glow parameters
            float4 _GlowColor;
            float _GlowIntensity;
            float _BorderWidth;
            float _BorderSpeed;
            
            float4 _BgColor;
            float _PulseSpeed;
            float _PulseFrequency;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                float2 uv = IN.texcoord;
                
                // --- Scrolling Glow Border ---
                float leftDist = uv.x;
                float rightDist = 1.0 - uv.x;
                float bottomDist = uv.y;
                float topDist = 1.0 - uv.y;
                
                float minDist = min(min(leftDist, rightDist), min(bottomDist, topDist));
                
                // Border mask fades smoothly inwards
                float borderMask = smoothstep(_BorderWidth, 0.0, minDist);
                
                // Angle to scroll wave along the border edges
                float2 toCenter = uv - float2(0.5, 0.5);
                float angle = atan2(toCenter.y, toCenter.x);
                float wave = sin(angle * _PulseFrequency + _Time.y * _BorderSpeed) * 0.5 + 0.5;
                
                float4 finalGlow = _GlowColor * _GlowIntensity * borderMask * wave;
                
                // --- Background Energy Pulse ---
                float bgPulse = sin(_Time.y * _PulseSpeed) * 0.15 + 0.85;
                float4 finalBg = _BgColor * bgPulse;
                
                float4 combined = lerp(color + finalBg, finalGlow, borderMask);
                combined.a = max(color.a, borderMask * _GlowColor.a);
                
                return combined;
            }
            ENDCG
        }
    }
}
