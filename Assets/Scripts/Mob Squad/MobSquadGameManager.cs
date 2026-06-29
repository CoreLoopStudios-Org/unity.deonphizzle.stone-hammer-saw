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
    public float connectionTimeout = 10f;
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

    private List<Transform> spawnPoints = new List<Transform>();
    private List<GameObject> spawnedCharacters = new List<GameObject>();
    private bool hasConfirmedNextRound = false;
    private bool matchmakingActive = false;
    private NetworkRunner localRunner;
    
    private SquidGameManager squidManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        squidManager = FindObjectOfType<SquidGameManager>();

        // Auto-assign references if they are empty
        if (spawnLineParent == null)
            spawnLineParent = GameObject.Find("Spawn-Green-Line")?.transform;

        if (spawnLineParent != null)
        {
            foreach (Transform child in spawnLineParent) spawnPoints.Add(child);
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
            Button panelBtn = tapToPlayPanel.GetComponent<Button>();
            if (panelBtn == null) panelBtn = tapToPlayPanel.AddComponent<Button>();
            panelBtn.onClick.RemoveAllListeners();
            panelBtn.onClick.AddListener(OnTapPanelClicked);
        }

        if (winNextRoundBtn != null) winNextRoundBtn.onClick.AddListener(OnNextRoundClicked);
        if (lossNextRoundBtn != null) lossNextRoundBtn.onClick.AddListener(OnNextRoundClicked);
    }

    private void OnTapPanelClicked()
    {
        if (matchmakingActive) return;
        matchmakingActive = true;
        if (tapToPlayPanel != null) tapToPlayPanel.SetActive(false);
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
        }
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
        GameObject localPlayer = GameObject.Find("Pangopal_01");
        if (localPlayer != null) localPlayer.SetActive(false);

        List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
        for (int i = 0; i < shuffledSpawns.Count; i++)
        {
            Transform temp = shuffledSpawns[i];
            int randomIndex = Random.Range(i, shuffledSpawns.Count);
            shuffledSpawns[i] = shuffledSpawns[randomIndex];
            shuffledSpawns[randomIndex] = temp;
        }

        int spawnIndex = 0;

        foreach (var playerRef in localRunner.ActivePlayers)
        {
            if (spawnIndex >= shuffledSpawns.Count) break;
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
            NetworkObject playerObj = localRunner.Spawn(playerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);
            spawnedCharacters.Add(playerObj.gameObject);
        }

        while (spawnIndex < totalPlayersGoal && spawnIndex < shuffledSpawns.Count)
        {
            Transform spawnPoint = shuffledSpawns[spawnIndex++];
            GameObject npcObj = Instantiate(npcPrefab != null ? npcPrefab : playerPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedCharacters.Add(npcObj);
            
            var ai = npcObj.GetComponent<NPCSquadAI>() ?? npcObj.AddComponent<NPCSquadAI>();
            ai.target = chestBoxTransform;
            ai.moveSpeed = Random.Range(2.5f, 3.5f);
            ai.EnableAI(false);
        }

        RPC_StartCountdown();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartCountdown()
    {
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

        if (Object != null && Object.HasStateAuthority)
        {
            isGameActive = true;
            foreach (var charGo in spawnedCharacters)
            {
                if (charGo != null)
                {
                    var ai = charGo.GetComponent<NPCSquadAI>();
                    if (ai != null) ai.EnableAI(true);
                }
            }
        }
    }

    public void OnLocalPlayerEliminated()
    {
        isGameActive = false;
        if (lossPanel != null) lossPanel.SetActive(true);
    }

    public void OnPlayerReachedBox(GameObject characterGo)
    {
        if (Object != null && !Object.HasStateAuthority) return;
        if (!isGameActive) return;

        isGameActive = false;
        RPC_StopLocalMiniGame();
        
        var netObj = characterGo.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            roundWinner = netObj.InputAuthority;
            RPC_TriggerSelection(netObj);
        }
        else
        {
            RPC_TriggerNPCSelection(characterGo.name);
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

    public void OnLocalPlayerEliminated()
    {
        isGameActive = false;
        if (lossPanel != null) lossPanel.SetActive(true);
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