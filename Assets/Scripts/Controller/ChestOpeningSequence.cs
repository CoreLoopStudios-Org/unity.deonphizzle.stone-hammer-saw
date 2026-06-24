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
    [Tooltip("The WeoponSelect-Panel-Moiib Squad New panel UI GameObject.")]
    public GameObject weaponSelectPanel;
    [Tooltip("Prefab for the Hammer weapon (SledgeHammer2.fbx).")]
    public GameObject hammerPrefab;
    [Tooltip("Cached or explicitly assigned right hand transform of the player.")]
    public Transform playerHandTransform;

    private bool isOpened = false;
    private GameObject spawnedTool;
    private Sequence idleSequence;

    private bool selectionMade = false;
    private Coroutine countdownCoroutine;
    private GameObject scrollerManagerGo;

    private void Start()
    {
        // Fallbacks if not assigned in Inspector
        if (chestBox == null) chestBox = transform;
        if (spawnPoint == null) spawnPoint = transform;

        // Try to find SledgeHammer2 prefab in Resources if not assigned
        if (hammerPrefab == null)
        {
            hammerPrefab = Resources.Load<GameObject>("SledgeHammer2");
        }

        // Find the WeaponSelect manaeger at start (while it is active) and deactivate it so it doesn't auto-spin/show
        scrollerManagerGo = GameObject.Find("WeaponSelect manaeger");
        if (scrollerManagerGo != null)
        {
            scrollerManagerGo.SetActive(false);
            Debug.Log("[ChestOpeningSequence] Deactivated 'WeaponSelect manaeger' at start to prevent auto-spin.");
        }

        // Ensure the BoxCollider is marked as a trigger and has a generous size for reliable detection
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            // Expand the size if it's too small (e.g. less than 1.8 in horizontal/vertical dimensions)
            Vector3 size = boxCollider.size;
            if (size.x < 1.8f) size.x = 1.8f;
            if (size.y < 1.5f) size.y = 1.5f;
            if (size.z < 1.8f) size.z = 1.8f;
            boxCollider.size = size;
            
            // Adjust center slightly upwards so it triggers well at character body height
            Vector3 center = boxCollider.center;
            if (center.y < 0.2f) center.y = 0.4f;
            boxCollider.center = center;
            
            Debug.Log($"[ChestOpeningSequence] BoxCollider trigger optimized. New size: {boxCollider.size}, center: {boxCollider.center}");
        }
    }

    private bool IsSceneObject(GameObject go)
    {
        return go != null && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ChestOpeningSequence] OnTriggerEnter triggered by: {other.gameObject.name} (Root: {other.transform.root.name})");
        
        // Check root and parents as well in case collider is on a nested child bone/component
        bool nameMatches = other.gameObject.name.Contains(targetCharacterName) || 
                           (other.transform.root != null && other.transform.root.name.Contains(targetCharacterName));

        if (!isOpened && nameMatches)
        {
            Debug.Log("[ChestOpeningSequence] Target character detected! Play opening sequence...");
            PlayOpeningSequence();
        }
    }

    [ContextMenu("Test Opening Sequence")]
    public void PlayOpeningSequence()
    {
        if (isOpened) return;
        isOpened = true;

        Debug.Log("[ChestOpeningSequence] PlayOpeningSequence started.");
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

            // Wait exactly 1.0 second before showing the selection panel using a coroutine for complete reliability
            StartCoroutine(DelayedShowPanel(1.0f));
        });

        // Open chest lid
        if (chestLid != null)
        {
            openSeq.Join(chestLid.DOLocalRotate(lidOpenRotation, lidOpenDuration).SetEase(lidOpenEase));
        }

        // Return box position to normal on sequence finish/kill
        openSeq.OnKill(() => chestBox.position = originalBoxPos);
    }

    private System.Collections.IEnumerator DelayedShowPanel(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowWeaponSelectPanel();
    }

    private void ShowWeaponSelectPanel()
    {
        Debug.Log("[ChestOpeningSequence] ShowWeaponSelectPanel called.");

        // If assigned, ensure it is a scene object and not a prefab asset
        if (weaponSelectPanel != null && !IsSceneObject(weaponSelectPanel))
        {
            Debug.LogWarning("[ChestOpeningSequence] Assigned weaponSelectPanel was a prefab asset! Resetting to find the scene instance.");
            weaponSelectPanel = null;
        }

        // Find WeoponSelect-Panel-Moiib Squad New dynamically in the scene if not explicitly assigned
        if (weaponSelectPanel == null)
        {
            // 1. Scan memory for scene GameObjects matching the name
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (IsSceneObject(go))
                {
                    if (go.name == "WeoponSelect-Panel-Moiib Squad New")
                    {
                        weaponSelectPanel = go;
                        Debug.Log($"[ChestOpeningSequence] Found panel object '{go.name}' in scene memory.");
                        break;
                    }
                }
            }

            // 2. Fallback to Canvas transform lookup if not found
            if (weaponSelectPanel == null)
            {
                GameObject canvasGo = GameObject.Find("Canvas");
                if (canvasGo != null)
                {
                    Transform panelTrans = canvasGo.transform.Find("WeoponSelect-Panel-Moiib Squad New");
                    if (panelTrans != null)
                    {
                        weaponSelectPanel = panelTrans.gameObject;
                        Debug.Log($"[ChestOpeningSequence] Found panel object '{weaponSelectPanel.name}' under Canvas.");
                    }
                }
            }
        }

        if (weaponSelectPanel != null)
        {
            Debug.Log($"[ChestOpeningSequence] Activating weaponSelectPanel '{weaponSelectPanel.name}' in scene.");
            weaponSelectPanel.SetActive(true);
            selectionMade = false;

            // Make the panel background clickable to select the Hammer
            UnityEngine.UI.Button panelBtn = weaponSelectPanel.GetComponent<UnityEngine.UI.Button>();
            if (panelBtn == null)
            {
                panelBtn = weaponSelectPanel.AddComponent<UnityEngine.UI.Button>();
            }
            panelBtn.onClick.RemoveAllListeners();
            panelBtn.onClick.AddListener(() => SelectWeapon(2)); // Default to Hammer on panel background click

            // Dynamically assign listeners to any UI Buttons on this panel
            UnityEngine.UI.Button[] buttons = weaponSelectPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            
            // If the panel has no buttons, procedurally create them!
            if (buttons.Length == 0 || (buttons.Length == 1 && buttons[0] == panelBtn))
            {
                CreateProceduralButtons(weaponSelectPanel);
            }
            else
            {
                Debug.Log($"[ChestOpeningSequence] Panel has {buttons.Length} existing buttons. Hooking up select events...");
                for (int i = 0; i < buttons.Length; i++)
                {
                    int index = i; // local copy for closure
                    if (buttons[i] == panelBtn) continue; // Skip panel background button
                    
                    buttons[i].onClick.RemoveAllListeners();
                    buttons[i].onClick.AddListener(() => SelectWeapon(index));
                }
            }

            // Start the slot machine spinning now that the panel is visible
            SlotMachineManager slotMachine = FindObjectOfType<SlotMachineManager>();
            if (slotMachine != null)
            {
                slotMachine.ResetAndStartSpin();
                // Subscribe to slot machine selection event
                slotMachine.OnWeaponSelected -= OnSlotMachineWeaponSelected;
                slotMachine.OnWeaponSelected += OnSlotMachineWeaponSelected;
            }

            // Also support WeaponSelect manaeger / SlotMachineScroller
            if (scrollerManagerGo == null)
            {
                Transform scrollerTrans = weaponSelectPanel.transform.parent.Find("WeaponSelect manaeger");
                if (scrollerTrans != null)
                {
                    scrollerManagerGo = scrollerTrans.gameObject;
                }
            }

            if (scrollerManagerGo != null)
            {
                scrollerManagerGo.SetActive(true);
                SlotMachineScroller scroller = scrollerManagerGo.GetComponent<SlotMachineScroller>();
                if (scroller != null)
                {
                    // Subscribe to slot machine selection event
                    scroller.OnWeaponSelected -= OnScrollerWeaponSelected;
                    scroller.OnWeaponSelected += OnScrollerWeaponSelected;
                }
            }

            // Start the 5-second countdown timer
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(StartCountdownTimer());
        }
        else
        {
            Debug.LogError("[ChestOpeningSequence] WeoponSelect-Panel-Moiib Squad New NOT found in scene! Auto-selecting Hammer as fallback.");
            SelectWeapon(2); // Fallback to Hammer immediately
        }
    }

    private void CreateProceduralButtons(GameObject parentPanel)
    {
        Debug.Log("[ChestOpeningSequence] No buttons found on WeoponSelect-Panel-Moiib Squad New. Creating procedural weapon buttons...");

        // Add a VerticalLayoutGroup to position buttons automatically
        UnityEngine.UI.VerticalLayoutGroup layout = parentPanel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = parentPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 15f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        string[] weaponNames = { "Mini Saw", "Big Saw", "Hammer", "Mini Stone", "Big Stone" };

        for (int i = 0; i < weaponNames.Length; i++)
        {
            int index = i; // local copy for closure
            
            // Create Button GameObject
            GameObject btnObj = new GameObject($"Button_{weaponNames[i]}");
            btnObj.transform.SetParent(parentPanel.transform, false);
            
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240f, 55f);

            btnObj.AddComponent<CanvasRenderer>();
            
            // Background Image
            UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.92f); // Sleek dark gray
            
            // Button component
            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            
            // Add click listener
            btn.onClick.AddListener(() => SelectWeapon(index));

            // Create Text label
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            textObj.AddComponent<CanvasRenderer>();
            
            // Use TMPro for high-quality text layout
            TMPro.TextMeshProUGUI txt = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = weaponNames[i];
            txt.fontSize = 20;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color = Color.white;
            
            // Smoothly pop-in the button
            btnObj.transform.localScale = Vector3.zero;
            btnObj.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(i * 0.08f);
        }
    }

    private System.Collections.IEnumerator StartCountdownTimer()
    {
        float timeLeft = 5f;

        // Try to locate a text component inside the panel to show the timer
        TMPro.TextMeshProUGUI countdownText = weaponSelectPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        UnityEngine.UI.Text legacyText = null;
        
        // If we don't have a label for the countdown, create one!
        if (countdownText == null)
        {
            GameObject timerTextObj = new GameObject("CountdownText");
            timerTextObj.transform.SetParent(weaponSelectPanel.transform, false);
            timerTextObj.transform.SetAsFirstSibling(); // Put it at the top
            
            RectTransform textRt = timerTextObj.AddComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(300f, 40f);
            
            timerTextObj.AddComponent<CanvasRenderer>();
            countdownText = timerTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            countdownText.fontSize = 24;
            countdownText.alignment = TMPro.TextAlignmentOptions.Center;
            countdownText.color = new Color(1f, 0.8f, 0.2f); // Warm gold timer text
        }

        while (timeLeft > 0f)
        {
            string timerString = $"Time Remaining: {Mathf.CeilToInt(timeLeft)}s";
            if (countdownText != null) countdownText.text = timerString;
            else if (legacyText != null) legacyText.text = timerString;

            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        // Timer expired: auto-select Hammer (index 2)
        if (!selectionMade)
        {
            Debug.Log("[ChestOpeningSequence] Time expired! Auto-selecting Hammer.");
            SelectWeapon(2);
        }
    }

    // Public method that buttons or local scripts can invoke to select the weapon
    public void SelectWeapon(int weaponIndex)
    {
        if (selectionMade) return;
        selectionMade = true;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        // Unsubscribe from slot machine selection event
        SlotMachineManager slotMachine = FindObjectOfType<SlotMachineManager>();
        if (slotMachine != null)
        {
            slotMachine.OnWeaponSelected -= OnSlotMachineWeaponSelected;
            slotMachine.StopSpinning(weaponIndex);
        }

        if (scrollerManagerGo != null)
        {
            SlotMachineScroller scroller = scrollerManagerGo.GetComponent<SlotMachineScroller>();
            if (scroller != null)
            {
                scroller.OnWeaponSelected -= OnScrollerWeaponSelected;
            }
        }

        if (GameplayController.Instance != null)
        {
            GameplayController.Instance.SelectWeapon(weaponIndex);
        }

        SpawnAndEquipWeapon(weaponIndex);
    }

    private void OnSlotMachineWeaponSelected(int index)
    {
        SelectWeapon(index);
    }

    private void OnScrollerWeaponSelected(int weaponIndex)
    {
        StartCoroutine(DelayedEquipScrollerWeapon(weaponIndex));
    }

    private System.Collections.IEnumerator DelayedEquipScrollerWeapon(int weaponIndex)
    {
        // Wait 1 second so the player can see the selected weapon highlight in the slot machine
        yield return new WaitForSeconds(1.0f);

        // Deactivate the WeaponSelect manaeger GameObject
        if (scrollerManagerGo != null)
        {
            scrollerManagerGo.SetActive(false);
        }

        // Proceed to select and equip the weapon
        SelectWeapon(weaponIndex);
    }

    private void SpawnAndEquipWeapon(int weaponIndex)
    {
        // 1. Hide the selection panel
        if (weaponSelectPanel != null)
        {
            // Destroy procedural elements so they can be clean next time
            foreach (Transform child in weaponSelectPanel.transform)
            {
                if (child.name.StartsWith("Button_") || child.name == "CountdownText")
                {
                    Destroy(child.gameObject);
                }
            }
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
                localOffsetPos = new Vector3(0.03641049f, 0.08302949f, -0.0680940f);
                localOffsetRot = Quaternion.Euler(-12.213f, -14.958f, -83.297f);
                targetEquipScale = new Vector3(0.00286978f, 0.00286977f, 0.00286977f);
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
        equipSeq.Join(weaponObj.transform.DOScale(targetEquipScale * 1.3f, 0.7f).SetEase(Ease.OutBack));
        equipSeq.Join(weaponObj.transform.DORotate(new Vector3(0f, 360f, 45f), 0.7f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

        equipSeq.AppendInterval(0.2f);

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
            mat.color = new Color(0.35f, 0.2f, 0.1f);
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
        GameObject lightObj = new GameObject("VFX_ChestGlowLight");
        lightObj.transform.position = spawnPoint.position;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.2f);
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
                    mat.color = new Color(0.2f, 0.7f, 1f);
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

        idleSequence.Append(spawnedTool.transform.DOMoveY(peakPosition.y + floatRange, floatCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));

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
        
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        SlotMachineManager slotMachine = FindObjectOfType<SlotMachineManager>();
        if (slotMachine != null)
        {
            slotMachine.OnWeaponSelected -= OnSlotMachineWeaponSelected;
        }

        if (scrollerManagerGo != null)
        {
            SlotMachineScroller scroller = scrollerManagerGo.GetComponent<SlotMachineScroller>();
            if (scroller != null)
            {
                scroller.OnWeaponSelected -= OnScrollerWeaponSelected;
            }
        }
    }
}
