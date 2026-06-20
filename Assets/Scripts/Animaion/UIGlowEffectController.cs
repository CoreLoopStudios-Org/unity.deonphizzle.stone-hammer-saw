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
