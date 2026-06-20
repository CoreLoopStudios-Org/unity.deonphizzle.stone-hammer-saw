# Weapon Selection VFX Implementation Plan

This document provides a comprehensive implementation plan and code for a premium visual upgrade to the `WeoponSelect-Panel-Moiib Squad New` UI panel in Unity. It outlines a custom HDR UI Glow Shader, breathing/pulsing C# controllers, Particle VFX setup, and URP Post-Processing Bloom volume integration.

---

## 1. Custom UI Glow Shader (HLSL)

This shader is a standard Unity UI Template-compliant shader that incorporates:
*   **Scrolling Glowing Border:** Detects the boundaries of the image UV and animates an emissive glow along the edges.
*   **Subtle Energy-Pulse Background:** Animates a soft breathing wave across the center area of the UI panel.
*   **HDR Support:** Annotates properties with `[HDR]` to allow URP Bloom post-processing to generate neon halos.
*   **Stencil Support:** Full compatibility with standard `UnityEngine.UI.Mask` components.

Create a shader file at `Assets/Shaders/UIGlowShader.shader` and paste the following code:

```hlsl
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
```

---

## 2. Dynamic HDR Glow Controller (C#)

This controller instantiates a unique copy of the material at runtime to prevent overriding asset values on disk. It uses **DOTween** to smoothly animate the emissive HDR color and intensity, creating a pulsing/breathing light effect.

Save the script as `Assets/Scripts/Animaion/UIGlowEffectController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class UIGlowEffectController : MonoBehaviour
{
    private Image uiImage;
    private Material instantiatedMaterial;

    [Header("Glow Intensity Animation")]
    [SerializeField] private float minGlowIntensity = 1.0f;
    [SerializeField] private float maxGlowIntensity = 3.5f;
    [SerializeField] private float pulseDuration = 2.0f;
    [SerializeField] private Ease pulseEase = Ease.InOutSine;

    [Header("Energy Pulse Animation")]
    [SerializeField] private Color pulseStartColor = new Color(0f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color pulseEndColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private float colorCycleDuration = 4.0f;

    private void Awake()
    {
        uiImage = GetComponent<Image>();
        if (uiImage != null && uiImage.material != null)
        {
            // Instantiate material to prevent changes to project asset
            instantiatedMaterial = new Material(uiImage.material);
            uiImage.material = instantiatedMaterial;
        }
    }

    private void Start()
    {
        if (instantiatedMaterial != null)
        {
            StartBreathingAnimations();
        }
    }

    private void StartBreathingAnimations()
    {
        // 1. Pulse Intensity using DOTween
        DOTween.To(
            () => instantiatedMaterial.GetFloat("_GlowIntensity"),
            x => instantiatedMaterial.SetFloat("_GlowIntensity", x),
            maxGlowIntensity,
            pulseDuration
        )
        .From(minGlowIntensity)
        .SetEase(pulseEase)
        .SetLoops(-1, LoopType.Yoyo)
        .SetLink(gameObject);

        // 2. Cycle Glow color using DOTween
        DOTween.To(
            () => instantiatedMaterial.GetColor("_GlowColor"),
            c => instantiatedMaterial.SetColor("_GlowColor", c),
            pulseEndColor,
            colorCycleDuration
        )
        .From(pulseStartColor)
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo)
        .SetLink(gameObject);
    }

    private void OnDestroy()
    {
        if (instantiatedMaterial != null)
        {
            Destroy(instantiatedMaterial);
        }
    }
}
```

---

## 3. Particle VFX Integration

To integrate lightweight glowing sparks or dust behind the slot machine container while it spins, use a dedicated controller class.

Save this script as `Assets/Scripts/Animaion/UISpinParticleController.cs`:

```csharp
using UnityEngine;

public class UISpinParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem uiParticleSystem;

    public void PlayParticles()
    {
        if (uiParticleSystem != null)
        {
            var main = uiParticleSystem.main;
            main.loop = true;
            if (!uiParticleSystem.isPlaying)
            {
                uiParticleSystem.Play();
            }
        }
    }

    public void StopParticles()
    {
        if (uiParticleSystem != null)
        {
            var main = uiParticleSystem.main;
            main.loop = false;
            // Stop emitting new particles, allow remaining to fade out naturally
            uiParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
```

### Editor Particle System Configuration Steps

To render particles behind UI elements inside a Canvas, follow these setup rules:

1. **GameObject Hierarchy:**
    *   Create a new **Particle System** under `WeoponSelect-Panel-Moiib Squad New`.
    *   Re-order its position in the hierarchy to be placed **behind** the slot machine container (`HeadlineFContainer` siblings) but in front of the panel's background image.
2. **Particle System Module Settings:**
    *   **Duration / Start Lifetime:** Set to `1.5 - 2.0s`.
    *   **Start Speed:** Set to a low value (`10 - 50` units depending on canvas scale) or use gravity.
    *   **Emission:** Set rate over time to `15 - 25` (keeps it lightweight).
    *   **Shape:** Set to **Box** or **Rectangle**, and scale the width/height to match the borders of the slot machine panel.
    *   **Size over Lifetime:** Add a curve scaling down to $0.0$ at the end of the particle's life.
    *   **Color over Lifetime:** Apply a gradient fading to $0$ alpha at the end of its lifetime. Set the start color to matching neon cyan or cyan-blue.
3. **Renderer Module Settings (Critical for UI):**
    *   **Render Mode:** Sprite.
    *   **Material:** Create a new material using `Universal Render Pipeline/2D/Sprite-Unlit-Default` or a custom Mobile Additive shader. Apply a soft glowing circle sprite.
    *   **Sorting Layer / Order in Layer:** Set to match the UI Canvas sorting properties.

---

## 4. Post-Processing (Bloom) Setup for UI in URP

By default, Unity UI elements in an Overlay Canvas bypass URP Post-Processing and will not bloom. To allow Canvas elements to glow, the Canvas must render in Screen Space or World Space using a camera that supports HDR.

### Step 1: Canvas Render Mode Adjustments
1.  Select your root Canvas (or the Canvas component hosting `WeoponSelect-Panel-Moiib Squad New`).
2.  Change **Render Mode** to **Screen Space - Camera**.
3.  Assign the scene's **Main Camera** (which renders character & background meshes) to the **Render Camera** field.
4.  Set the Z-position of the Panel's RectTransform to a small positive distance (e.g. `10.0` or `100.0`) in front of the camera, ensuring it fits within the camera's near and far clipping planes.

### Step 2: Camera Inspector Configurations
1.  Select the **Main Camera** GameObject in the hierarchy.
2.  Locate the URP **Universal Additional Camera Data** or camera settings.
3.  Ensure **Post Processing** is checked/enabled.
4.  Ensure **HDR** is checked/enabled (this is critical: without HDR, values above `1.0` are clamped, preventing any glow bloom).

### Step 3: Volume Setup
1.  In the scene hierarchy, click `+` $\rightarrow$ `Volume` $\rightarrow$ `Global Volume`.
2.  In the **Volume** component, click **New** next to the Profile field to generate a new Volume Profile.
3.  Click **Add Override** $\rightarrow$ **Post-processing** $\rightarrow$ **Bloom**.
4.  Activate the following parameters and configure their values:
    *   **Intensity:** `1.5 - 2.5` (controls the size of the glow halo).
    *   **Threshold:** `0.9 - 1.0` (prevents standard UI elements from blooming; only HDR properties exceeding intensity 1.0 will glow).
    *   **Scatter:** `0.5 - 0.7` (controls diffusion smoothness).

---

## 5. Integration Checklist

1.  **Create Assets:**
    *   Create `UIGlowShader.shader` in `Assets/Shaders/`.
    *   Create `UIGlowEffectController.cs` and `UISpinParticleController.cs` in `Assets/Scripts/Animaion/`.
2.  **Setup Material:**
    *   Right-click `UIGlowShader.shader` $\rightarrow$ `Create` $\rightarrow$ `Material`. Name it `M_WeaponSelect_Glow`.
    *   Configure `_GlowColor` to neon cyan and set intensity/background colors in the Inspector.
3.  **Apply to UI:**
    *   Select the background image of `WeoponSelect-Panel-Moiib Squad New`.
    *   Assign `M_WeaponSelect_Glow` to its **Material** slot.
    *   Attach `UIGlowEffectController` to the same GameObject to animate it.
4.  **Wire Spinning VFX:**
    *   Attach `UISpinParticleController` to the `WeaponSelect manager`.
    *   Set up a Particle System behind the slot columns and bind it to `UISpinParticleController`.
    *   In [SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs), call `PlayParticles()` on spin start and `StopParticles()` on deceleration.
