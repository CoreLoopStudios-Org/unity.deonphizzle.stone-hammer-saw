using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;
using System.Threading.Tasks;

public class TapToStartMatchmaking : MonoBehaviour, IPointerClickHandler
{
    [Header("Matchmaking Settings")]
    [Tooltip("If checked, matchmaking will launch automatically on start (optional).")]
    public bool autoStart = false;

    private bool isStarting = false;

    private void Start()
    {
        if (autoStart)
        {
            StartMatchmakingProcess();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isStarting) return;
        isStarting = true;

        Debug.Log("[TapToStartMatchmaking] Panel tapped. Beginning Fusion Matchmaking...");
        StartMatchmakingProcess();
    }

    private async void StartMatchmakingProcess()
    {
        // 1. Find or create NetworkRunner
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            GameObject runnerGo = new GameObject("FusionRunner");
            runner = runnerGo.AddComponent<NetworkRunner>();
        }

        // 2. Add callbacks if needed
        var matchmakingManager = FindObjectOfType<MatchmakingManager>();
        if (matchmakingManager != null)
        {
            runner.AddCallbacks(matchmakingManager);
        }

        // 3. Close the Tap Panel
        gameObject.SetActive(false);

        // 4. Start the Fusion Game session in Shared mode
        Debug.Log("[TapToStartMatchmaking] Connecting to Fusion in Shared Mode...");
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "MobSquadSession", // Shared room name
            PlayerCount = 8,                 // Max 8 players
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = runner.gameObject.GetComponent<NetworkSceneManagerDefault>() ?? 
                           runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("[TapToStartMatchmaking] Joined session successfully!");
        }
        else
        {
            Debug.LogError($"[TapToStartMatchmaking] Failed to join session: {result.ShutdownReason}");
        }
    }
}
