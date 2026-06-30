using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using DG.Tweening;

public class MobSquadGameManager : NetworkBehaviour
{
    public static MobSquadGameManager Instance { get; private set; }

    [Header("Match Settings")]
    public int totalPlayersGoal = 8;
    public float connectionTimeout = 5f;
    public int maxRounds = 3;

    [Header("Prefabs & Spawning")]
    public GameObject playerPrefab; 
    public GameObject npcPrefab;    
    public Transform spawnLineParent;     
    public Transform chestBoxTransform;   
    public ChestOpeningSequence chestSeq; 

    [Header("UI Panels (Assign Only Here)")]
    public GameObject tapToPlayPanel; 
    public GameObject winPanel;
    public GameObject lossPanel;
    public Button winNextRoundBtn;
    public Button lossNextRoundBtn;

    // Synced Network Properties
    [Networked] public int currentRound { get; set; } = 1;
    [Networked] public NetworkBool isGameActive { get; set; } = false;
    [Networked] public PlayerRef roundWinner { get; set; } = PlayerRef.None;
    [Networked] public NetworkBool isMatchOver { get; set; } = false;
    [Networked] public int nextRoundConfirmations { get; set; } = 0;

    private bool isOfflineGameActive = false;

    public bool IsGameActiveSafe
    {
        get
        {
            try
            {
                if (Object == null || !Object.IsValid) return isOfflineGameActive;
                return isGameActive;
            }
            catch (System.InvalidOperationException)
            {
                return isOfflineGameActive;
            }
        }
    }

    private List<Transform> spawnPoints = new List<Transform>();
    private List<GameObject> spawnedCharacters = new List<GameObject>();
    private bool hasConfirmedNextRound = false;
    private bool matchmakingActive = false;
    private NetworkRunner localRunner;
    
    private SquidGameManager squidManager;
    private GameObject loadingScreen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Spawned()
    {
        base.Spawned();
        if (tapToPlayPanel != null) tapToPlayPanel.SetActive(false);
        if (loadingScreen != null) loadingScreen.SetActive(true);

        if (Object.HasStateAuthority)
        {
            localRunner = Runner; 
            StartCoroutine(MatchmakingTimeoutRoutine());
        }
    }

 private void Start()
{
    connectionTimeout = 3f; 
    squidManager = FindObjectOfType<SquidGameManager>();
    
    if (spawnLineParent == null)
    {
        GameObject spawnLineObj = GameObject.Find("Spawn-Green-Line");
        spawnLineParent = spawnLineObj?.transform;
    }
    
    spawnPoints.Clear(); 
    if (spawnLineParent != null)
    {
        foreach (Transform child in spawnLineParent) 
        {
            spawnPoints.Add(child);
        }
        Debug.Log($"[MobSquad] সফলভাবে {spawnPoints.Count}টি স্পন পয়েন্ট পাওয়া গেছে।");
    }
    else
    {
        Debug.LogError("[MobSquad] Spawn-Green-Line খুঁজে পাওয়া যায়নি! হায়ারার্কিতে এই নামের অবজেক্ট আছে কি?");
    }
    
    if (chestBoxTransform == null)
    {
        GameObject boxObj = GameObject.Find("Box");
        if (boxObj != null)
        {
            chestBoxTransform = boxObj.transform;
            chestSeq = boxObj.GetComponent<ChestOpeningSequence>();
            Debug.Log("[MobSquad] Box এবং Chest sequence পাওয়া গেছে।");
        }
    }
    
    if (tapToPlayPanel == null) 
        tapToPlayPanel = GameObject.Find("Tap-loads mob-squead3d world panel");

    if (tapToPlayPanel != null)
    {
        var tapToLoad = tapToPlayPanel.GetComponent<TapToLoadScene>();
        if (tapToLoad != null) Destroy(tapToLoad);

        Button panelBtn = tapToPlayPanel.GetComponent<Button>();
        if (panelBtn == null) panelBtn = tapToPlayPanel.AddComponent<Button>();
        
        panelBtn.onClick.RemoveAllListeners();
        panelBtn.onClick.AddListener(OnTapPanelClicked);
        Debug.Log("[MobSquad] TapToPlay বাটনে লিসেনার সেট করা হয়েছে।");
    }
    
    if (winNextRoundBtn != null) winNextRoundBtn.onClick.AddListener(OnNextRoundClicked);
    if (lossNextRoundBtn != null) lossNextRoundBtn.onClick.AddListener(OnNextRoundClicked);
    
    if (loadingScreen == null)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform loading = canvas.transform.Find("LoadinScreen-mob squead scene");
            if (loading != null) loadingScreen = loading.gameObject;
        }
    }
}

    private void OnTapPanelClicked()
    {
        if (matchmakingActive) return;
        matchmakingActive = true;
        if (tapToPlayPanel != null) tapToPlayPanel.SetActive(false);
        if (loadingScreen != null) loadingScreen.SetActive(true);
        StartMatchmakingAndPlay();
    }

    private async void StartMatchmakingAndPlay()
    {
        localRunner = FindObjectOfType<NetworkRunner>();
        if (localRunner == null)
        {
            GameObject runnerGo = new GameObject("FusionRunner");
            localRunner = runnerGo.AddComponent<NetworkRunner>();
        }

        try
        {
            var result = await localRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = "MobSquadSession_" + Random.Range(1000, 9999),
                PlayerCount = 8,
                Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
                SceneManager = localRunner.gameObject.GetComponent<NetworkSceneManagerDefault>() ?? 
                               localRunner.gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                if (localRunner.IsServer || localRunner.IsSharedModeMasterClient)
                {
                    StartCoroutine(MatchmakingTimeoutRoutine());
                }
                return;
            }
            else
            {
                Debug.LogWarning($"[MobSquad] Fusion StartGame failed: {result.ShutdownReason}. Falling back to offline local mode.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogWarning("[MobSquad] Exception during Fusion StartGame. Falling back to offline local mode.");
        }

        // Fallback to offline/single player mode
        StartCoroutine(OfflineStartRoutine());
    }

    private IEnumerator OfflineStartRoutine()
    {
        yield return new WaitForSeconds(1.0f); // Small delay to feel like matchmaking
        InitializePlayersAndNPCsOffline();
    }

  private void InitializePlayersAndNPCsOffline()
{
    // ১. বিদ্যমান ক্যারেক্টার ডিজেবল করা
    GameObject localPlayerObj = GameObject.Find("Pangopal_01");
    if (localPlayerObj != null) localPlayerObj.SetActive(false);

    // ২. স্পন পয়েন্ট সাফল করা
    List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
    System.Random rng = new System.Random();
    shuffledSpawns = shuffledSpawns.OrderBy(a => rng.Next()).ToList();

    int spawnIndex = 0;

    // ৩. প্লেয়ার স্পন করা এবং পজিশন ঠিক করা
    if (spawnIndex < shuffledSpawns.Count)
    {
        Transform localSpawnPoint = shuffledSpawns[spawnIndex++];
        // Y-axis এ সামান্য উপরে স্পন করছি যেন মাটির সাথে ক্লিপ না করে
        Vector3 spawnPos = localSpawnPoint.position + Vector3.up * 0.1f; 
        
        GameObject spawnedLocalPlayer = Instantiate(playerPrefab, spawnPos, localSpawnPoint.rotation);
        spawnedLocalPlayer.name = "Pangopal_01_Spawned";
        spawnedLocalPlayer.SetActive(true); 
        spawnedCharacters.Add(spawnedLocalPlayer);

        // ৪. ক্যামেরা সেটআপ (Main Camera ফোকাস)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // ক্যামেরাকে প্লেয়ারের পেছনে সেট করছি
            mainCam.transform.position = spawnPos + new Vector3(0, 2, -5);
            mainCam.transform.LookAt(spawnPos + Vector3.up * 1f);
            
            // যদি আপনার কোনো ক্যামেরা ফলো স্ক্রিপ্ট থাকে, সেটি এখানে এনাবল করতে পারেন
            var camScript = mainCam.GetComponent<CameraFollow>(); // আপনার ক্যামেরা স্ক্রিপ্টের নাম দিন
            if (camScript != null) 
            {
                camScript.target = spawnedLocalPlayer.transform;
                camScript.enabled = true;
            }
        }

        var controllerScript = spawnedLocalPlayer.GetComponent<ThirdPersonCharacterController>();
        if (controllerScript != null) controllerScript.enabled = true;
    }

    // ৫. NPC বট স্পন করা
    while (spawnIndex < totalPlayersGoal && spawnIndex < shuffledSpawns.Count)
    {
        Transform spawnPoint = shuffledSpawns[spawnIndex++];
        Vector3 npcSpawnPos = spawnPoint.position + Vector3.up * 0.1f;

        GameObject npcObj = Instantiate(npcPrefab != null ? npcPrefab : playerPrefab, npcSpawnPos, spawnPoint.rotation);
        npcObj.name = "NPC_Bot_" + spawnIndex;
        npcObj.SetActive(true);
        spawnedCharacters.Add(npcObj);
        
        var ai = npcObj.GetComponent<NPCSquadAI>() ?? npcObj.AddComponent<NPCSquadAI>();
        ai.target = chestBoxTransform;
        ai.moveSpeed = Random.Range(2.5f, 3.5f);
        ai.EnableAI(false);
    }

    if (loadingScreen != null) loadingScreen.SetActive(false);
    
    // ৬. গেম স্টার্ট
    StartCoroutine(CountdownRoutine());
}

    private IEnumerator MatchmakingTimeoutRoutine()
    {
        float timer = 0f;
        while (timer < connectionTimeout)
        {
            if (localRunner.ActivePlayers.Count() >= totalPlayersGoal) break;
            timer += Time.deltaTime;
            yield return null;
        }
        InitializePlayersAndNPCs();
    }

    private void InitializePlayersAndNPCs()
    {
        Debug.Log($"[DEBUG] Starting Spawning. Available Spawn Points: {spawnPoints.Count}");

        GameObject localPlayer = GameObject.Find("Pangopal_01");
        if (localPlayer != null) localPlayer.SetActive(false);

        List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
        // Shuffle logic... (আগের মতোই)

        int spawnIndex = 0;

        // ১. অনলাইন প্লেয়ারদের স্পন করা
        foreach (var playerRef in localRunner.ActivePlayers)
        {
            if (spawnIndex >= shuffledSpawns.Count) break;
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
        
            // এখানে পজিশন এবং প্রিফ্যাব চেক করি
            if (playerPrefab == null) {
                Debug.LogError("[ERROR] playerPrefab is NULL in Inspector!");
                continue;
            }

            NetworkObject playerObj = localRunner.Spawn(playerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);
        
            if (playerObj != null) {
                spawnedCharacters.Add(playerObj.gameObject);
                Debug.Log($"[DEBUG] Successfully spawned network player: {playerObj.name} at {spawnPoint.position}");
            } else {
                Debug.LogError("[ERROR] Fusion failed to spawn playerPrefab!");
            }
        }

        // ২. এনপিসি স্পন করা
        while (spawnIndex < totalPlayersGoal && spawnIndex < shuffledSpawns.Count)
        {
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
            GameObject npcObj = Instantiate(npcPrefab != null ? npcPrefab : playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
            if (npcObj != null) {
                spawnedCharacters.Add(npcObj);
                npcObj.SetActive(true); // নিশ্চিত করছি অবজেক্ট একটিভ
                Debug.Log($"[DEBUG] Successfully spawned NPC: {npcObj.name} at {spawnPoint.position}");
            }
        }

        RPC_StartCountdown();
    }

// ক্যামেরা স্বয়ংক্রিয়ভাবে প্লেয়ারের পেছনে সেট করার জন্য হেল্পার মেথড
private void SetupCameraFollow(Transform targetCharacter)
{
    Transform cam = Camera.main != null ? Camera.main.transform : GameObject.FindWithTag("MainCamera")?.transform;
    if (cam == null) cam = GameObject.Find("Main Camera")?.transform;

    if (cam != null && targetCharacter != null)
    {
        // প্লেয়ারের পেছনে ২ ইউনিট উপরে এবং ৫ ইউনিট পেছনে ক্যামেরা সেট করবে
        cam.position = targetCharacter.position + new Vector3(0f, 2f, -5f);
        cam.LookAt(targetCharacter.position + Vector3.up * 1f);
        
        // যদি আপনার প্রজেক্টে কোনো ক্যামেরা ফলো স্ক্রিপ্ট (যেমন CM FreeLook বা Cinemachine) থাকে, 
        // তবে তার Target এখানে রানটাইমে ডাইনামিকালি সেট করে দিতে পারেন।
        Debug.Log($"[MobSquad Visuals] Camera successfully attached and focused on local player: {targetCharacter.name}");
    }
}

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartCountdown()
    {
        if (loadingScreen != null) loadingScreen.SetActive(false);
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 3; i > 0; i--)
        {
            if (squidManager != null) squidManager.AnimateStatusText(i.ToString(), Color.yellow, 1.5f);
            yield return new WaitForSeconds(1f);
        }

        if (squidManager != null) squidManager.AnimateStatusText("GO!", Color.green, 2.0f);
        yield return new WaitForSeconds(0.8f);
        if (squidManager != null) squidManager.HideStatusText();

        if (squidManager != null) squidManager.StartMiniGame();

        if (Object != null && Object.IsValid && Object.HasStateAuthority)
        {
            isGameActive = true;
        }
        else
        {
            isOfflineGameActive = true;
        }

        foreach (var charGo in spawnedCharacters)
        {
            if (charGo != null)
            {
                var ai = charGo.GetComponent<NPCSquadAI>();
                if (ai != null) ai.EnableAI(true);
            }
        }
    }

    public void OnLocalPlayerEliminated()
    {
        if (Object != null && Object.IsValid)
        {
            isGameActive = false;
        }
        else
        {
            isOfflineGameActive = false;
        }
        if (lossPanel != null) lossPanel.SetActive(true);
    }

    public void OnPlayerReachedBox(GameObject characterGo)
    {
        if (Object != null && Object.IsValid && !Object.HasStateAuthority) return;
        
        bool active = IsGameActiveSafe;
        if (!active) return;

        if (Object != null && Object.IsValid)
        {
            isGameActive = false;
            RPC_StopLocalMiniGame();
        }
        else
        {
            isOfflineGameActive = false;
            if (squidManager != null) squidManager.StopMiniGame();
        }
        
        var netObj = characterGo.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsValid)
        {
            roundWinner = netObj.InputAuthority;
            RPC_TriggerSelection(netObj);
        }
        else
        {
            if (Object != null && Object.IsValid)
            {
                RPC_TriggerNPCSelection(characterGo.name);
            }
            else
            {
                if (chestSeq != null) chestSeq.PlayOpeningSequence();
                
                // Show offline result panels after a short delay
                bool isLocalPlayer = (characterGo.name == "Pangopal_01_Spawned");
                StartCoroutine(ShowOfflineResultPanels(isLocalPlayer));
            }
        }
    }

    private IEnumerator ShowOfflineResultPanels(bool isWinnerLocal)
    {
        yield return new WaitForSeconds(2.0f);
        if (isWinnerLocal)
        {
            if (winPanel != null) winPanel.SetActive(true);
        }
        else
        {
            if (lossPanel != null) lossPanel.SetActive(true);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopLocalMiniGame()
    {
        if (squidManager != null) squidManager.StopMiniGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerSelection(NetworkObject winnerObj)
    {
        bool isWinnerLocal = (winnerObj.InputAuthority == localRunner.LocalPlayer);
        if (isWinnerLocal && chestSeq != null) chestSeq.PlayOpeningSequence();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerNPCSelection(string npcName)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            StartCoroutine(NPCSelectionRoutine(npcName));
        }
    }

    private IEnumerator NPCSelectionRoutine(string npcName)
    {
        yield return new WaitForSeconds(1.5f);
        int randomWeapon = Random.Range(0, 5);
        RPC_ExecuteNPCAttack(npcName, randomWeapon);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ExecuteNPCAttack(string npcName, int weaponIndex)
    {
        GameObject attacker = spawnedCharacters.FirstOrDefault(c => c != null && c.name == npcName);
        if (attacker != null) 
        {
            var ai = attacker.GetComponent<NPCSquadAI>();
            if (ai != null) ai.EnableAI(false);
            ExecuteAttackSequence(attacker, weaponIndex);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ExecuteAttack(NetworkObject attacker, int weaponIndex)
    {
        if (attacker != null)
        {
            ExecuteAttackSequence(attacker.gameObject, weaponIndex);
        }
    }

    private void ExecuteAttackSequence(GameObject attacker, int weaponIndex)
    {
        GameObject target = FindNearestOpponent(attacker);

        if (target != null)
        {
            Transform attackerTrans = attacker.transform;
            Transform targetTrans = target.transform;

            attackerTrans.DOLookAt(targetTrans.position, 0.2f);

            attackerTrans.DOMove(attackerTrans.position + attackerTrans.forward * 1.2f, 0.3f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    targetTrans.DOPunchPosition(attackerTrans.forward * 0.8f, 0.5f, 10, 1f);
                    targetTrans.DORotate(new Vector3(0, 720, 0), 0.8f, RotateMode.FastBeyond360);
                    targetTrans.DOScale(Vector3.zero, 0.8f).OnComplete(() =>
                    {
                        var netObj = attacker.GetComponent<NetworkObject>();
                        ShowEndRoundPanels(netObj != null ? netObj.InputAuthority : PlayerRef.None);
                    });
                });
        }
        else
        {
            var netObj = attacker.GetComponent<NetworkObject>();
            ShowEndRoundPanels(netObj != null ? netObj.InputAuthority : PlayerRef.None);
        }
    }

    private GameObject FindNearestOpponent(GameObject self)
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var charGo in spawnedCharacters)
        {
            if (charGo == null || charGo == self) continue;

            float dist = Vector3.Distance(self.transform.position, charGo.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = charGo;
            }
        }
        return nearest;
    }

    private void ShowEndRoundPanels(PlayerRef winnerRef)
    {
        bool isLocalWinner = (winnerRef == localRunner.LocalPlayer);

        if (isLocalWinner)
        {
            if (winPanel != null) winPanel.SetActive(true);
        }
        else
        {
            if (lossPanel != null) lossPanel.SetActive(true);
        }
    }

    private void OnNextRoundClicked()
    {
        if (hasConfirmedNextRound) return;
        hasConfirmedNextRound = true;

        if (winPanel != null) winPanel.SetActive(false);
        if (lossPanel != null) lossPanel.SetActive(false);

        RPC_ConfirmNextRound();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ConfirmNextRound()
    {
        nextRoundConfirmations++;
        if (nextRoundConfirmations >= localRunner.ActivePlayers.Count())
        {
            nextRoundConfirmations = 0;
            if (currentRound < maxRounds)
            {
                currentRound++;
                RPC_LoadNextRoundScene();
            }
            else
            {
                isMatchOver = true;
                RPC_ShowFinalGameResult();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LoadNextRoundScene()
    {
        hasConfirmedNextRound = false;
        if (Object.HasStateAuthority) ResetRoundCharacters();
    }

    private void ResetRoundCharacters()
    {
        List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
        for (int i = 0; i < shuffledSpawns.Count; i++)
        {
            Transform temp = shuffledSpawns[i];
            int randomIndex = Random.Range(i, shuffledSpawns.Count);
            shuffledSpawns[i] = shuffledSpawns[randomIndex];
            shuffledSpawns[randomIndex] = temp;
        }

        int spawnIndex = 0;
        foreach (var charGo in spawnedCharacters)
        {
            if (charGo == null) continue;
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
            charGo.transform.position = spawnPoint.position;
            charGo.transform.rotation = spawnPoint.rotation;
            charGo.transform.localScale = Vector3.one;

            var ai = charGo.GetComponent<NPCSquadAI>();
            if (ai != null) ai.EnableAI(false);
        }
        RPC_StartCountdown();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFinalGameResult()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
    }
}

public class NPCSquadAI : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3f;
    private bool aiActive = false;

    public void EnableAI(bool active) { aiActive = active; }

    private void Update()
    {
        if (!aiActive || target == null) return;
        Vector3 targetPos = target.position;
        targetPos.y = transform.position.y; 
        transform.LookAt(targetPos);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetFloat("Speed", moveSpeed);
    }
}