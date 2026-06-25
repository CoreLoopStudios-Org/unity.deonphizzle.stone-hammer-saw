using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class SquidGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float timeLimit = 60f; // Changed to 1 minute
    public AudioSource dollMusic;  
    public float reactionTime = 0.5f; 
    
    [Header("UI Panel References")]
    public GameObject tapToPlayPanel; // Assign Tap-loads mob-squead3d world panel
    public GameObject gameOverPanel;  // Assign Loss (1) panel

    [Header("Status (Don't Touch)")]
    public bool isGreenLight = false;
    public bool isGameOver = false;
    public bool isGameStarted = false;
    public bool isCountdownActive = false;

    private float lightTimer = 0f;
    private float currentReactionTime = 0f;
    private int lastSec = -1;
    private bool tapPanelHasBeenActive = false;

    [Header("UI Text References")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Transform uiParent; // Assign your "Controller Background" panel or canvas here

    private void Start()
    {
        // Setup text components dynamically inside the canvas (if not assigned)
        SetupDynamicUI();

        // Initialize display values at startup
        if (statusText != null) statusText.text = "";
        if (timerText != null) timerText.text = "Time: " + Mathf.CeilToInt(timeLimit) + "s";

        // Music should start paused
        if (dollMusic != null) dollMusic.Stop();
    }

    private void SetupDynamicUI()
    {
        // If already assigned via inspector, don't recreate them dynamically
        if (statusText != null && timerText != null)
        {
            return;
        }

        Transform parentTransform = uiParent;
        if (parentTransform == null)
        {
            GameObject parentObj = GameObject.Find("Controller Background");
            if (parentObj == null) parentObj = GameObject.Find("Pungupops bg-panel");
            if (parentObj == null) parentObj = GameObject.Find("Canvas");
            if (parentObj != null) parentTransform = parentObj.transform;
        }

        if (parentTransform != null)
        {
            // Status Text (Match countdown and Light updates)
            if (statusText == null)
            {
                Transform existingStatus = parentTransform.Find("SquidStatusText");
                if (existingStatus != null)
                {
                    statusText = existingStatus.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    GameObject statusObj = new GameObject("SquidStatusText");
                    statusObj.transform.SetParent(parentTransform, false);
                    statusText = statusObj.AddComponent<TextMeshProUGUI>();
                    statusText.alignment = TextAlignmentOptions.Center;
                    statusText.fontSize = 70f;
                    statusText.fontStyle = FontStyles.Bold;
                    statusText.color = Color.yellow;
                    
                    RectTransform statusRect = statusText.rectTransform;
                    statusRect.anchorMin = new Vector2(0.5f, 0.5f);
                    statusRect.anchorMax = new Vector2(0.5f, 0.5f);
                    statusRect.anchoredPosition = new Vector2(0f, 150f);
                    statusRect.sizeDelta = new Vector2(800f, 150f);
                    statusText.text = "";
                }
            }

            // Timer Text (60s countdown)
            if (timerText == null)
            {
                Transform existingTimer = parentTransform.Find("SquidTimerText");
                if (existingTimer != null)
                {
                    timerText = existingTimer.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    GameObject timerObj = new GameObject("SquidTimerText");
                    timerObj.transform.SetParent(parentTransform, false);
                    timerText = timerObj.AddComponent<TextMeshProUGUI>();
                    timerText.alignment = TextAlignmentOptions.Center;
                    timerText.fontSize = 45f;
                    timerText.fontStyle = FontStyles.Bold;
                    timerText.color = Color.white;

                    RectTransform timerRect = timerText.rectTransform;
                    timerRect.anchorMin = new Vector2(0.5f, 1f);
                    timerRect.anchorMax = new Vector2(0.5f, 1f);
                    timerRect.anchoredPosition = new Vector2(0f, -60f);
                    timerRect.sizeDelta = new Vector2(400f, 80f);
                    timerText.text = "";
                }
            }
        }
    }

    private void AnimateStatusText(string text, Color color, float targetScale = 1.0f)
    {
        if (statusText == null) return;
        
        statusText.transform.DOKill();
        statusText.text = text;
        statusText.color = color;
        
        // Pop-in scale animation using DOTween
        statusText.transform.localScale = Vector3.zero;
        statusText.transform.DOScale(Vector3.one * targetScale, 0.35f).SetEase(Ease.OutBack);
    }

    private void Update()
    {
        // 1. Wait until playerthe  taps the load/tap screen
        if (!isGameStarted)
        {
            if (tapToPlayPanel != null)
            {
                if (tapToPlayPanel.activeSelf)
                {
                    tapPanelHasBeenActive = true;
                    return; // Wait until player taps and closes it
                }

                // If the panel is now closed (or started inactive), start the game
                isGameStarted = true;
                StartCoroutine(StartMatchCountdown());
            }
            else
            {
                // Fallback: start immediately if no panel reference is assigned
                isGameStarted = true;
                StartCoroutine(StartMatchCountdown());
            }
            return;
        }

        if (isGameOver || isCountdownActive) return;

        // 2. Decrement the 1-minute timer
        timeLimit -= Time.deltaTime;
        
        int currentSec = Mathf.Max(0, Mathf.CeilToInt(timeLimit));
        if (currentSec != lastSec)
        {
            lastSec = currentSec;
            if (timerText != null)
            {
                timerText.text = "Time: " + currentSec + "s";
                
                // DOTween tick pulse animation on each second change
                timerText.transform.DOKill();
                timerText.transform.localScale = Vector3.one;
                if (timeLimit <= 10f)
                {
                    timerText.color = Color.red;
                    timerText.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f, 5, 1f);
                }
                else
                {
                    timerText.color = Color.white;
                    timerText.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 2, 1f);
                }
            }
        }

        if (timeLimit <= 0)
        {
            EliminatePlayer("Time is up! You failed to reach the chest.");
            return;
        }

        // 3. Random light timer transition
        lightTimer -= Time.deltaTime;
        if (lightTimer <= 0)
        {
            if (isGreenLight) SwitchToRedLight();
            else SwitchToGreenLight();
        }

        // 4. Movement detection during Red Light
        if (!isGreenLight)
        {
            if (currentReactionTime > 0)
            {
                currentReactionTime -= Time.deltaTime;
            }
            else
            {
                float moveInput = SimpleMobileJoystick.InputDirection.magnitude;
                bool pcMovement = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;

                if (moveInput > 0.1f || pcMovement)
                {
                    EliminatePlayer("You moved during RED LIGHT! Eliminated.");
                }
            }
        }
    }

    private IEnumerator StartMatchCountdown()
    {
        isCountdownActive = true;
        if (statusText != null) statusText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            AnimateStatusText(i.ToString(), Color.yellow, 1.2f);
            yield return new WaitForSeconds(1f);
        }

        AnimateStatusText("GO!", Color.green, 1.5f);
        yield return new WaitForSeconds(1f);

        if (statusText != null) statusText.text = "";
        isCountdownActive = false;

        // Start gameplay timer and music
        timeLimit = 60f;
        SwitchToGreenLight();
    }

    private void SwitchToGreenLight()
    {
        isGreenLight = true;
        if (dollMusic != null) dollMusic.Play();
        lightTimer = Random.Range(3.0f, 6.0f);
        
        AnimateStatusText("GREEN LIGHT - RUN!", Color.green, 1.0f);
    }

    private void SwitchToRedLight()
    {
        isGreenLight = false;
        if (dollMusic != null) dollMusic.Pause();
        currentReactionTime = reactionTime;
        lightTimer = Random.Range(2.0f, 3.5f);

        AnimateStatusText("RED LIGHT - STOP!", Color.red, 1.0f);
    }

    public void EliminatePlayer(string reason)
    {
        isGameOver = true;
        if (dollMusic != null) dollMusic.Pause();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        AnimateStatusText("ELIMINATED!", Color.red, 1.3f);
        
        Debug.Log("<color=red>GAME OVER: " + reason + "</color>");
    }

    public void PlayerWon()
    {
        isGameOver = true;
        if (dollMusic != null) dollMusic.Pause();
        
        AnimateStatusText("YOU WON!", Color.green, 1.3f);
        
        Debug.Log("<color=green>YOU WON! You reached the chest in time.</color>");
    }
}