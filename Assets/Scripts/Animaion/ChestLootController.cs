using UnityEngine;
using DG.Tweening;

public class ChestLootController : MonoBehaviour
{
    [Header("Chest Components")]
    [SerializeField] private Transform chestLid; // Chest's upper lid
    [SerializeField] private Transform chestBody; // Chest's main body (for Shaking)

    [Header("Loot Settings")]
    [SerializeField] private GameObject toolPrefab; // The hammer or tool that will spawn
    [SerializeField] private Transform spawnPoint; // Where the tool will spawn from inside the chest

    [Header("Effects")]
    [SerializeField] private ParticleSystem glowParticles; // Magical light effect

    private bool _isOpened = false;

    // This function is called when the character approaches the chest
    public void OpenChestSequence()
    {
        if (_isOpened) return;
        _isOpened = true;

        // 1. Anticipation effect: The chest will shake a little before the lid opens (Shake)
        chestBody.DOShakeScale(0.3f, 0.15f, 10, 90, true)
            .OnComplete(() =>
            {
                // 2. Lid opening (Rotate Open)
                // Axis (like X Axis) and Angle (-110f) may need to be adjusted based on your model
                chestLid.DOLocalRotate(new Vector3(-110f, 0f, 0f), 0.6f)
                    .SetEase(Ease.OutBack); // Using OutBack will make the lid bounce slightly at the end of opening

                // 3. Play particle effect
                if (glowParticles != null)
                {
                    glowParticles.Play();
                }

                // 4. Spawn tool and start animation
                AnimateLootDrop();
            });
    }

    private void AnimateLootDrop()
    {
        // Spawn the tool at zero scale inside the chest
        GameObject spawnedTool = Instantiate(toolPrefab, spawnPoint.position, Quaternion.identity);
        spawnedTool.transform.localScale = Vector3.zero;

        // Create DOTween Sequence
        Sequence lootSequence = DOTween.Sequence();

        // Float position in the air (2 units above the chest)
        Vector3 targetPosition = spawnPoint.position + Vector3.up * 2f;

        // 5. Rise, scale up, and rotate simultaneously
        lootSequence.Append(spawnedTool.transform.DOMove(targetPosition, 0.8f).SetEase(Ease.OutQuad));
        lootSequence.Join(spawnedTool.transform.DOScale(Vector3.one, 0.8f).SetEase(Ease.OutBack));
        lootSequence.Join(spawnedTool.transform.DORotate(new Vector3(0f, 360f, 45f), 0.8f, RotateMode.FastBeyond360));

        // 6. Start loop animation (floating and spinning) when the main animation ends
        lootSequence.OnComplete(() =>
        {
            // Gently float up and down indefinitely (Yoyo Floating)
            spawnedTool.transform.DOMoveY(targetPosition.y + 0.2f, 1.2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // Spin slowly around its own axis indefinitely (Slow Spin)
            spawnedTool.transform.DORotate(new Vector3(0f, 360f, 0f), 4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        });
    }
}