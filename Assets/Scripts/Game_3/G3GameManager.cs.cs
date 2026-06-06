using UnityEngine;
using TMPro;

public class G3GameManager : MonoBehaviour
{
    public static G3GameManager Instance { get; private set; }

    [Header("Managers")]
    public GyroDetectionManager gyroManager;
    public WeaponSelectManager weaponManager;

    [Header("Game Settings")]
    public int maxRounds = 3;
    public int roundsToWin = 2;

    [Header("UI")]
    public GameObject roundStartPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI enemyScoreText;

    // Game state
    private int currentRound = 1;
    private int playerWins = 0;
    private int enemyWins = 0;
    private bool isPlayerHost = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        currentRound = 1;
        playerWins = 0;
        enemyWins = 0;

        UpdateScoreUI();
        StartRound();
    }

    public void StartRound()
    {
        Debug.Log($"Starting Round {currentRound}");

        if (roundText != null)
        {
            roundText.text = $"Round {currentRound}/{maxRounds}";
        }

        if (gyroManager != null)
        {
            gyroManager.ResetRound();
        }
    }

    public void OnPlayerSelectedWeapon(int weaponId)
    {
        Debug.Log($"Player selected weapon: {weaponId}");

        // In multiplayer, send this to the other player
        // For now, simulate the result
        SimulateRoundResult();
    }

    private void SimulateRoundResult()
    {
        // Temporary: 50/50 chance to win
        bool playerWon = UnityEngine.Random.value > 0.5f;

        if (playerWon)
        {
            playerWins++;
            Debug.Log("Player wins this round!");
        }
        else
        {
            enemyWins++;
            Debug.Log("Enemy wins this round!");
        }

        UpdateScoreUI();
        CheckMatchEnd();
    }

    private void UpdateScoreUI()
    {
        if (playerScoreText != null)
            playerScoreText.text = $"You: {playerWins}";
        if (enemyScoreText != null)
            enemyScoreText.text = $"Enemy: {enemyWins}";
    }

    private void CheckMatchEnd()
    {
        if (playerWins >= roundsToWin)
        {
            EndMatch(true);
        }
        else if (enemyWins >= roundsToWin)
        {
            EndMatch(false);
        }
        else
        {
            currentRound++;
            Invoke(nameof(StartRound), 2f);
        }
    }

    private void EndMatch(bool playerVictory)
    {
        Debug.Log(playerVictory ? "You won the match!" : "You lost the match!");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void OnPlayerEliminated()
    {
        Debug.Log("Player eliminated by early pickup");
        enemyWins++;
        UpdateScoreUI();
        CheckMatchEnd();
    }
}
