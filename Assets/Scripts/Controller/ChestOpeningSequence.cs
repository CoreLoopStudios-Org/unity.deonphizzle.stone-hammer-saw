using UnityEngine;
using DG.Tweening;

public class ChestOpeningSequence : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("The root transform of the chest box. Usually named 'Chest-Box'.")]
    public Transform chestBox;
    [Tooltip("The lid transform that will rotate open.")]
    public Transform chestLid;
    [Tooltip("The character name that triggers the chest. Usually 'Pangopal_01'.")]
    public string targetCharacterName = "Pangopal_01";

    [Header("Chest Animation Settings")]
    public Vector3 shakeStrength = new Vector3(0.08f, 0.04f, 0.08f);
    public float shakeDuration = 0.5f;
    public int shakeVibrato = 15;
    
    public Vector3 lidOpenRotation = new Vector3(-110f, 0f, 0f);
    public float lidOpenDuration = 0.8f;
    public Ease lidOpenEase = Ease.OutBack;

    [Header("Tool / Item Settings (Fallback)")]
    public GameObject toolPrefab;
    public Transform spawnPoint;
    public Vector3 toolTargetScale = Vector3.one;
    public float floatHeight = 1.3f;
    public float popDuration = 0.9f;
    public Ease popScaleEase = Ease.OutBack;
    public Ease popMoveEase = Ease.OutQuad;

    [Header("Tool Idle Floating Settings")]
    public float floatRange = 0.12f;
    public float floatCycleDuration = 1.8f;
    public float spinSpeed = 40f; // Degrees per second

    [Header("Visual Effects Hook")]
    public ParticleSystem openParticleSystem;

    [Header("Weapon Selection Integration")]
    [Tooltip("The Weapon-Select-Panel UI GameObject.")]
    public GameObject weaponSelectPanel;
    [Tooltip("The SlotMachineManager attached to the panel.")]
    public SlotMachineManager slotMachineManager;
    [Tooltip("Prefab for the Hammer weapon (SledgeHammer2.fbx).")]
    public GameObject hammerPrefab;
    [Tooltip("Cached or explicitly assigned right hand transform of the player.")]
    public Transform playerHandTransform;

    private bool isOpened = false;
    private GameObject spawnedTool;
    private Sequence idleSequence;

    private void Start()
    {
        // Fallbacks if not assigned in Inspector
        if (chestBox == null) chestBox = transform;
        if (spawnPoint == null) spawnPoint = transform;

        // Hook up to the slot machine manager's weapon selection event
        if (slotMachineManager != null)
        {
            slotMachineManager.OnWeaponSelected += OnWeaponSelected;
        }
        else if (weaponSelectPanel != null)
        {
            slotMachineManager = weaponSelectPanel.GetComponent<SlotMachineManager>();
            if (slotMachineManager != null)
            {
                slotMachineManager.OnWeaponSelected += OnWeaponSelected;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trigger only when Pangopal_01 arrives and chest is closed
        if (!isOpened && other.gameObject.name.Contains(targetCharacterName))
        {
            PlayOpeningSequence();
        }
    }

    [ContextMenu("Test Opening Sequence")]
    public void PlayOpeningSequence()
    {
        if (isOpened) return;
        isOpened = true;

        Vector3 originalBoxPos = chestBox.position;

        // Build DOTween Sequence
        Sequence openSeq = DOTween.Sequence();

        // 1. Shake the Box (Anticipation)
        openSeq.Append(chestBox.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true));

        // 2. Open Lid, Play Particles, and Show UI or Spawn default tool
        openSeq.AppendCallback(() =>
        {
            if (openParticleSystem != null)
            {
                openParticleSystem.Play();
            }

            // Play custom DOTween VFX (glow and burst spheres)
            PlayChestVFXEffects();

            if (weaponSelectPanel != null)
            {
                // Wait for the lid opening animation to finish before showing UI
                DOVirtual.DelayedCall(lidOpenDuration - 0.2f, () =>
                {
                    weaponSelectPanel.SetActive(true);
                    
                    // Restart slot machine spin
                    if (slotMachineManager != null)
                    {
                        slotMachineManager.ResetAndStartSpin();
                    }
                });
            }
            else if (toolPrefab != null)
            {
                // Fallback behavior: instantiate the default tool prefab immediately
                spawnedTool = Instantiate(toolPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedTool.transform.localScale = Vector3.zero;

                Sequence toolPopSeq = DOTween.Sequence();
                toolPopSeq.Append(spawnedTool.transform.DOScale(toolTargetScale, popDuration).SetEase(popScaleEase));
                toolPopSeq.Join(spawnedTool.transform.DOMoveY(spawnPoint.position.y + floatHeight, popDuration).SetEase(popMoveEase));
                toolPopSeq.Join(spawnedTool.transform.DORotate(new Vector3(0f, 360f, 0f), popDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
                
                toolPopSeq.OnComplete(StartToolIdleAnimation);
            }
        });

        // Open chest lid
        if (chestLid != null)
        {
            openSeq.Join(chestLid.DOLocalRotate(lidOpenRotation, lidOpenDuration).SetEase(lidOpenEase));
        }

        // Return box position to normal on sequence finish/kill
        openSeq.OnKill(() => chestBox.position = originalBoxPos);
    }

    private void OnWeaponSelected(int weaponIndex)
    {
        SpawnAndEquipWeapon(weaponIndex);
    }

    private void SpawnAndEquipWeapon(int weaponIndex)
    {
        // 1. Hide the selection panel
        if (weaponSelectPanel != null)
        {
            weaponSelectPanel.SetActive(false);
        }

        // 2. Find player's right hand transform if not assigned
        if (playerHandTransform == null)
        {
            GameObject playerObj = GameObject.Find(targetCharacterName);
            if (playerObj != null)
            {
                playerHandTransform = FindChildRecursive(playerObj.transform, "CC_Base_R_Hand");
            }
        }

        if (playerHandTransform == null)
        {
            Debug.LogError("[ChestOpeningSequence] Player right hand bone 'CC_Base_R_Hand' not found!");
            return;
        }

        // 3. Destroy any previously equipped weapon on the hand
        foreach (Transform child in playerHandTransform)
        {
            if (child.name.StartsWith("Equipped_"))
            {
                Destroy(child.gameObject);
            }
        }

        // 4. Create/Instantiate the weapon
        GameObject weaponObj = null;
        Vector3 localOffsetPos = Vector3.zero;
        Quaternion localOffsetRot = Quaternion.identity;
        Vector3 targetEquipScale = Vector3.one;

        switch (weaponIndex)
        {
            case 0: // Mini Saw
                weaponObj = CreateProceduralSaw(0.18f, Color.gray);
                weaponObj.name = "Equipped_MiniSaw";
                localOffsetPos = new Vector3(0.04f, 0.06f, 0.02f);
                localOffsetRot = Quaternion.Euler(0f, 90f, 90f);
                targetEquipScale = new Vector3(0.8f, 0.8f, 0.8f);
                break;
            case 1: // Big Saw
                weaponObj = CreateProceduralSaw(0.3f, new Color(0.25f, 0.25f, 0.25f));
                weaponObj.name = "Equipped_BigSaw";
                localOffsetPos = new Vector3(0.06f, 0.08f, 0.02f);
                localOffsetRot = Quaternion.Euler(0f, 90f, 90f);
                targetEquipScale = new Vector3(1.1f, 1.1f, 1.1f);
                break;
            case 2: // Hammer
                if (hammerPrefab != null)
                {
                    weaponObj = Instantiate(hammerPrefab);
                }
                else
                {
                    weaponObj = CreateProceduralHammer();
                }
                weaponObj.name = "Equipped_Hammer";
                localOffsetPos = new Vector3(0.02f, 0.12f, -0.04f);
                localOffsetRot = Quaternion.Euler(-90f, 0f, 0f);
                targetEquipScale = new Vector3(0.15f, 0.15f, 0.15f); // Scale properly for sledgehammer mesh
                break;
            case 3: // Mini Stone
                weaponObj = CreateProceduralStone(0.12f, new Color(0.45f, 0.45f, 0.45f));
                weaponObj.name = "Equipped_MiniStone";
                localOffsetPos = new Vector3(0.02f, 0.05f, 0f);
                localOffsetRot = Quaternion.identity;
                targetEquipScale = Vector3.one;
                break;
            case 4: // Big Stone
                weaponObj = CreateProceduralStone(0.24f, new Color(0.3f, 0.3f, 0.3f));
                weaponObj.name = "Equipped_BigStone";
                localOffsetPos = new Vector3(0.03f, 0.07f, 0f);
                localOffsetRot = Quaternion.identity;
                targetEquipScale = Vector3.one;
                break;
            default:
                Debug.LogError("[ChestOpeningSequence] Invalid weapon index: " + weaponIndex);
                return;
        }

        if (weaponObj == null) return;

        // Position weapon at chest spawn point
        weaponObj.transform.position = spawnPoint.position;
        weaponObj.transform.rotation = spawnPoint.rotation;
        weaponObj.transform.localScale = Vector3.zero;

        // 5. DOTween Equip Animation Sequence
        Sequence equipSeq = DOTween.Sequence();

        // Step A: Rise from chest and scale up
        Vector3 midAirPos = spawnPoint.position + Vector3.up * floatHeight;
        equipSeq.Append(weaponObj.transform.DOMove(midAirPos, 0.7f).SetEase(Ease.OutQuad));
        equipSeq.Join(weaponObj.transform.DOScale(targetEquipScale * 1.3f, 0.7f).SetEase(Ease.OutBack)); // pop up and scale slightly bigger
        equipSeq.Join(weaponObj.transform.DORotate(new Vector3(0f, 360f, 45f), 0.7f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

        // Let it float for a brief split second
        equipSeq.AppendInterval(0.2f);

        // Flight sparks
        equipSeq.AppendCallback(() => PlayFlightTrailVFX(midAirPos, playerHandTransform));

        // Step B: Fly smoothly and rotate into character's hand
        equipSeq.Append(weaponObj.transform.DOMove(playerHandTransform.position, 0.6f).SetEase(Ease.InOutSine));
        equipSeq.Join(weaponObj.transform.DOScale(targetEquipScale, 0.6f).SetEase(Ease.InOutSine));
        equipSeq.Join(weaponObj.transform.DORotate(playerHandTransform.rotation.eulerAngles + localOffsetRot.eulerAngles, 0.6f).SetEase(Ease.InOutSine));

        // Step C: Equip onto hand and play impact VFX
        equipSeq.OnComplete(() =>
        {
            weaponObj.transform.SetParent(playerHandTransform);
            weaponObj.transform.localPosition = localOffsetPos;
            weaponObj.transform.localRotation = localOffsetRot;
            weaponObj.transform.localScale = targetEquipScale;

            // Bounce impact scale
            weaponObj.transform.DOPunchScale(targetEquipScale * 0.3f, 0.4f, 10, 1f);

            // Flash effect at the hand
            PlayEquipFlashVFX(playerHandTransform.position);

            // Trigger player animator Attack
            Animator playerAnim = playerHandTransform.root.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("Attack");
            }
        });
    }

    private GameObject CreateProceduralSaw(float radius, Color bladeColor)
    {
        GameObject saw = new GameObject("ProceduralSaw");
        
        // Blade Disc
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        blade.name = "Blade";
        blade.transform.SetParent(saw.transform);
        blade.transform.localPosition = Vector3.zero;
        blade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        blade.transform.localScale = new Vector3(radius * 2f, 0.015f, radius * 2f);
        Destroy(blade.GetComponent<Collider>());
        
        Renderer renderer = blade.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = bladeColor;
            mat.SetFloat("_Metallic", 0.95f);
            mat.SetFloat("_Smoothness", 0.8f);
            renderer.material = mat;
        }

        // Shaft/Handle
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        handle.name = "Handle";
        handle.transform.SetParent(saw.transform);
        handle.transform.localPosition = new Vector3(0f, -radius * 0.7f, 0f);
        handle.transform.localScale = new Vector3(0.03f, radius * 0.7f, 0.03f);
        Destroy(handle.GetComponent<Collider>());
        
        Renderer handleRenderer = handle.GetComponent<Renderer>();
        if (handleRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.35f, 0.2f, 0.1f); // Wood/Brown
            handleRenderer.material = mat;
        }

        return saw;
    }

    private GameObject CreateProceduralStone(float size, Color stoneColor)
    {
        GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(stone.GetComponent<Collider>());
        stone.transform.localScale = new Vector3(size, size, size);
        
        Renderer renderer = stone.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = stoneColor;
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Smoothness", 0.05f);
            renderer.material = mat;
        }
        return stone;
    }

    private GameObject CreateProceduralHammer()
    {
        GameObject hammer = new GameObject("ProceduralHammer");
        
        // Handle
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(hammer.transform);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.04f, 0.5f, 0.04f);
        Destroy(handle.GetComponent<Collider>());
        
        Renderer handleRenderer = handle.GetComponent<Renderer>();
        if (handleRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.35f, 0.2f, 0.1f);
            handleRenderer.material = mat;
        }

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(hammer.transform);
        head.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        head.transform.localScale = new Vector3(0.15f, 0.15f, 0.3f);
        Destroy(head.GetComponent<Collider>());
        
        Renderer headRenderer = head.GetComponent<Renderer>();
        if (headRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.gray;
            mat.SetFloat("_Metallic", 0.9f);
            mat.SetFloat("_Smoothness", 0.6f);
            headRenderer.material = mat;
        }

        return hammer;
    }

    private void PlayChestVFXEffects()
    {
        // 1. Dynamic Point Light Glow inside the box
        GameObject lightObj = new GameObject("VFX_ChestGlowLight");
        lightObj.transform.position = spawnPoint.position;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.2f); // Warm magical glow
        light.range = 0f;
        light.intensity = 0f;

        DOTween.To(() => light.range, x => light.range = x, 5f, 0.3f).SetEase(Ease.OutQuad);
        DOTween.To(() => light.intensity, x => light.intensity = x, 15f, 0.3f).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                DOTween.To(() => light.range, x => light.range = x, 0f, 1f).SetEase(Ease.InQuad);
                DOTween.To(() => light.intensity, x => light.intensity = x, 0f, 1f).SetEase(Ease.InQuad)
                    .OnComplete(() => Destroy(lightObj));
            });

        // 2. Spawn burst of 12 glowing particles (small spheres)
        for (int i = 0; i < 12; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(particle.GetComponent<Collider>());
            particle.transform.position = spawnPoint.position + Random.insideUnitSphere * 0.15f;
            particle.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);

            Renderer renderer = particle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.Lerp(new Color(1f, 0.4f, 0f), new Color(1f, 0.9f, 0.2f), Random.value);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", mat.color * 1.5f);
                renderer.material = mat;
            }

            Vector3 direction = (Vector3.up * 1.4f + Random.onUnitSphere * 0.7f).normalized;
            float force = Random.Range(1.5f, 3.2f);
            float lifeTime = Random.Range(0.6f, 1.2f);

            particle.transform.DOMove(particle.transform.position + direction * (force * lifeTime), lifeTime)
                .SetEase(Ease.OutQuad);
            
            particle.transform.DOScale(Vector3.zero, lifeTime)
                .SetEase(Ease.InQuad)
                .OnComplete(() => Destroy(particle));
        }
    }

    private void PlayFlightTrailVFX(Vector3 start, Transform target)
    {
        for (int i = 0; i < 6; i++)
        {
            float delay = i * 0.08f;
            DOVirtual.DelayedCall(delay, () =>
            {
                if (target == null) return;
                
                GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(trail.GetComponent<Collider>());
                trail.transform.position = start + Random.insideUnitSphere * 0.05f;
                trail.transform.localScale = Vector3.one * 0.06f;

                Renderer renderer = trail.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.2f, 0.7f, 1f); // Blue trail spark
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", mat.color * 1.5f);
                    renderer.material = mat;
                }

                trail.transform.DOMove(target.position, 0.4f).SetEase(Ease.OutQuad);
                trail.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InQuad)
                    .OnComplete(() => Destroy(trail));
            });
        }
    }

    private void PlayEquipFlashVFX(Vector3 position)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(flash.GetComponent<Collider>());
        flash.transform.position = position;
        flash.transform.localScale = Vector3.zero;

        Renderer renderer = flash.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.8f, 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", mat.color * 2.5f);
            renderer.material = mat;
        }

        flash.transform.DOScale(Vector3.one * 0.5f, 0.15f).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                flash.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad)
                    .OnComplete(() => Destroy(flash));
            });
    }

    private void StartToolIdleAnimation()
    {
        if (spawnedTool == null) return;

        Vector3 peakPosition = spawnedTool.transform.position;
        idleSequence = DOTween.Sequence();

        // Loop A: Floating Yoyo
        idleSequence.Append(spawnedTool.transform.DOMoveY(peakPosition.y + floatRange, floatCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));

        // Loop B: Slow Spinning
        spawnedTool.transform.DORotate(new Vector3(0f, 360f, 0f), 360f / spinSpeed, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (idleSequence != null) idleSequence.Kill();
        
        if (slotMachineManager != null)
        {
            slotMachineManager.OnWeaponSelected -= OnWeaponSelected;
        }
    }
}
