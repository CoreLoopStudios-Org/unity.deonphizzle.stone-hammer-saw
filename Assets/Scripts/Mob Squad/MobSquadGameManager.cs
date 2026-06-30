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
    private bool isSpawningStarted = false; // এটি ডাবল স্পন ঠেকাবে
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
        // কোড থেকে জোড় করে ৮ সেট করে দেওয়া হলো যেন ১৫ জন স্পন না হয়
        totalPlayersGoal = 8; 
        
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
        }
        
        if (chestBoxTransform == null)
        {
            GameObject boxObj = GameObject.Find("Box");
            if (boxObj != null)
            {
                chestBoxTransform = boxObj.transform;
                chestSeq = boxObj.GetComponent<ChestOpeningSequence>();
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
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        StartCoroutine(OfflineStartRoutine());
    }

    private IEnumerator OfflineStartRoutine()
    {
        // অফলাইনেও যেন ডাবল স্পন না হয়
        if (isSpawningStarted) yield break; 
        isSpawningStarted = true;

        yield return new WaitForSeconds(1.0f);
        InitializePlayersAndNPCsOffline();
    }

    private void InitializePlayersAndNPCsOffline()
    {
        GameObject localPlayerObj = GameObject.Find("Pangopal_01");
        if (localPlayerObj != null) localPlayerObj.SetActive(false);

        spawnedCharacters.Clear(); // লিস্ট ক্লিয়ার করা হলো যেন ডাবল স্পন না হয়

        List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
        System.Random rng = new System.Random();
        shuffledSpawns = shuffledSpawns.OrderBy(a => rng.Next()).ToList();

        int spawnIndex = 0;
        int maxSpawns = Mathf.Min(totalPlayersGoal, shuffledSpawns.Count);

        // লোকাল প্লেয়ার স্পন
        if (spawnIndex < maxSpawns)
        {
            Transform localSpawnPoint = shuffledSpawns[spawnIndex++];
            Vector3 spawnPos = localSpawnPoint.position + Vector3.up * 0.1f; 
            
            GameObject spawnedLocalPlayer = Instantiate(playerPrefab, spawnPos, localSpawnPoint.rotation);
            spawnedLocalPlayer.name = "Pangopal_01_Spawned";
            spawnedLocalPlayer.SetActive(true); 
            spawnedCharacters.Add(spawnedLocalPlayer);

            // বক্সের দিকে ঘুরিয়ে দেওয়া
            if (chestBoxTransform != null)
            {
                Vector3 lookPos = chestBoxTransform.position;
                lookPos.y = spawnedLocalPlayer.transform.position.y;
                spawnedLocalPlayer.transform.LookAt(lookPos);
            }

            SetupCameraFollow(spawnedLocalPlayer.transform);

            var controllerScript = spawnedLocalPlayer.GetComponent<ThirdPersonCharacterController>();
            if (controllerScript != null) controllerScript.enabled = false; 
        }

        // NPC স্পন
        while (spawnIndex < maxSpawns)
        {
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
            Vector3 npcSpawnPos = spawnPoint.position + Vector3.up * 0.1f;

            GameObject npcObj = Instantiate(npcPrefab != null ? npcPrefab : playerPrefab, npcSpawnPos, spawnPoint.rotation);
            npcObj.name = "NPC_Bot_" + spawnIndex;
            npcObj.SetActive(true);
            spawnedCharacters.Add(npcObj);
            
            var ai = npcObj.GetComponent<NPCSquadAI>() ?? npcObj.AddComponent<NPCSquadAI>();
            ai.target = chestBoxTransform;
            ai.baseMoveSpeed = Random.Range(2.5f, 3.5f); // ভেরিয়েবল আপডেট করা হলো
            ai.EnableAI(false);
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator MatchmakingTimeoutRoutine()
    {
        // যদি স্পনিং আগে থেকেই শুরু হয়ে থাকে, তবে এই কোড আর রান করবে না
        if (isSpawningStarted) yield break; 
        isSpawningStarted = true;

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
        GameObject localPlayer = GameObject.Find("Pangopal_01");
        if (localPlayer != null) localPlayer.SetActive(false);

        spawnedCharacters.Clear();

        List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
        System.Random rng = new System.Random();
        shuffledSpawns = shuffledSpawns.OrderBy(a => rng.Next()).ToList();

        int spawnIndex = 0;
        int maxSpawns = Mathf.Min(totalPlayersGoal, shuffledSpawns.Count);

        foreach (var playerRef in localRunner.ActivePlayers)
        {
            if (spawnIndex >= maxSpawns) break;
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
        
            if (playerPrefab == null) continue;

            NetworkObject playerObj = localRunner.Spawn(playerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);
        
            if (playerObj != null) 
            {
                spawnedCharacters.Add(playerObj.gameObject);

                if (chestBoxTransform != null)
                {
                    Vector3 lookPos = chestBoxTransform.position;
                    lookPos.y = playerObj.transform.position.y;
                    playerObj.transform.LookAt(lookPos);
                }

                if (playerRef == localRunner.LocalPlayer)
                {
                    SetupCameraFollow(playerObj.transform);
                }

                var controllerScript = playerObj.GetComponent<ThirdPersonCharacterController>();
                if (controllerScript != null) controllerScript.enabled = false;
            }
        }

        while (spawnIndex < maxSpawns)
        {
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
            GameObject npcObj = Instantiate(npcPrefab != null ? npcPrefab : playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
            if (npcObj != null) 
            {
                spawnedCharacters.Add(npcObj);
                npcObj.SetActive(true); 
                
                var ai = npcObj.GetComponent<NPCSquadAI>() ?? npcObj.AddComponent<NPCSquadAI>();
                ai.target = chestBoxTransform;
                ai.baseMoveSpeed = Random.Range(2.5f, 3.5f); // ভেরিয়েবল আপডেট করা হলো
                ai.EnableAI(false); 
            }
        }

        RPC_StartCountdown();
    }

    private void SetupCameraFollow(Transform targetCharacter)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : GameObject.FindWithTag("MainCamera")?.transform;
        if (cam == null) cam = GameObject.Find("Main Camera")?.transform;

        if (cam != null && targetCharacter != null)
        {
            cam.position = targetCharacter.position + new Vector3(0f, 2f, -5f);
            cam.LookAt(targetCharacter.position + Vector3.up * 1f);
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

        if (squidManager != null) 
        {
            squidManager.AnimateStatusText("GO!", Color.green, 2.0f);
            squidManager.StartMiniGame(); 
        }
        
        yield return new WaitForSeconds(0.8f);
        if (squidManager != null) squidManager.HideStatusText();

        if (Object != null && Object.IsValid && Object.HasStateAuthority) isGameActive = true;
        else isOfflineGameActive = true;

        foreach (var charGo in spawnedCharacters)
        {
            if (charGo == null) continue;

            var ai = charGo.GetComponent<NPCSquadAI>();
            if (ai != null) ai.EnableAI(true);

            var controller = charGo.GetComponent<ThirdPersonCharacterController>();
            if (controller != null)
            {
                var netObj = charGo.GetComponent<NetworkObject>();
                if (netObj == null || netObj.HasInputAuthority || netObj.HasStateAuthority)
                {
                    controller.enabled = true;
                }
            }
        }
    }

    public void OnLocalPlayerEliminated()
    {
        if (Object != null && Object.IsValid) isGameActive = false;
        else isOfflineGameActive = false;
        
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

            Transform spawnPoint = shuffledSpawns[spawnIndex % shuffledSpawns.Count];
            spawnIndex++;

            var cc = charGo.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            charGo.transform.position = spawnPoint.position;
            
            // নতুন রাউন্ডেও যেন বক্সের দিকে তাকিয়ে থাকে
            if (chestBoxTransform != null)
            {
                Vector3 lookPos = chestBoxTransform.position;
                lookPos.y = charGo.transform.position.y;
                charGo.transform.LookAt(lookPos);
            }
            else
            {
                charGo.transform.rotation = spawnPoint.rotation;
            }

            if (cc != null) cc.enabled = true; 

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

// র‍্যান্ডম মুভমেন্ট এবং রোটেশনের জন্য আপডেটেড AI ক্লাস
public class NPCSquadAI : MonoBehaviour
{
    public Transform target;
    public float baseMoveSpeed = 3f;
    
    private float currentSpeed;
    private bool aiActive = false;
    private CharacterController cc;
    private float randomStartDelay;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        randomStartDelay = Random.Range(0.1f, 1.2f); 
        currentSpeed = baseMoveSpeed;
    }

    private void Start()
    {
        if (target != null)
        {
            Vector3 lookPos = target.position;
            lookPos.y = transform.position.y; 
            transform.LookAt(lookPos);
        }
    }

    public void EnableAI(bool active) 
    { 
        if (active) 
        {
            StartCoroutine(StartMovingWithDelay());
        }
        else 
        {
            aiActive = false;
            StopAllCoroutines();
        }
    }

    private IEnumerator StartMovingWithDelay()
    {
        yield return new WaitForSeconds(randomStartDelay);
        aiActive = true;
        StartCoroutine(RandomizeSpeedRoutine());
    }

    private IEnumerator RandomizeSpeedRoutine()
    {
        while (aiActive)
        {
            currentSpeed = baseMoveSpeed + Random.Range(-0.8f, 1.5f);
            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }

    private void Update()
    {
        if (!aiActive || target == null) return;
        
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; 
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
        }

        if (cc != null)
        {
            cc.SimpleMove(direction * currentSpeed);
        }
        else
        {
            transform.position += direction * currentSpeed * Time.deltaTime;
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetFloat("Speed", currentSpeed);
    }
}