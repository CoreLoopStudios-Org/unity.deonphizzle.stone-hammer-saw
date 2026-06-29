using UnityEngine;
using TMPro;
using DG.Tweening;

public class SquidGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float timeLimit = 60f; 
    public AudioSource dollMusic;  
    public float reactionTime = 0.5f; 

    [Header("Status (Don't Touch)")]
    public bool isGreenLight = false;
    private float lightTimer = 0f;
    private float currentReactionTime = 0f;
    private int lastSec = -1;

    private TextMeshProUGUI statusText;
    private TextMeshProUGUI timerText;

    private void Start()
    {
        SetupDynamicUI();
        if (statusText != null) statusText.text = "";
        if (timerText != null) timerText.text = "Time: 60s";
        if (dollMusic != null) dollMusic.Stop();
    }

    private void SetupDynamicUI()
    {
        Transform parentTransform = GameObject.Find("Controller Background")?.transform 
            ?? GameObject.Find("Pungupops bg-panel")?.transform 
            ?? GameObject.Find("Canvas")?.transform;

        if (parentTransform != null)
        {
            Transform existingStatus = parentTransform.Find("SquidStatusText");
            if (existingStatus != null) statusText = existingStatus.GetComponent<TextMeshProUGUI>();
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

            Transform existingTimer = parentTransform.Find("SquidTimerText");
            if (existingTimer != null) timerText = existingTimer.GetComponent<TextMeshProUGUI>();
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

    public void AnimateStatusText(string text, Color color, float targetScale = 1.0f)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(true);
        statusText.transform.DOKill();
        statusText.text = text;
        statusText.color = color;
        statusText.transform.localScale = Vector3.zero;
        statusText.transform.DOScale(Vector3.one * targetScale, 0.35f).SetEase(Ease.OutBack);
    }
    
    public void HideStatusText()
    {
        if (statusText != null) statusText.gameObject.SetActive(false);
    }

    public void StartMiniGame()
    {
        timeLimit = 60f;
        SwitchToGreenLight();
    }

    public void StopMiniGame()
    {
        if (dollMusic != null) dollMusic.Pause();
    }

    private void Update()
    {
        if (MobSquadGameManager.Instance == null || !MobSquadGameManager.Instance.IsGameActiveSafe) return;

        timeLimit -= Time.deltaTime;
        int currentSec = Mathf.Max(0, Mathf.CeilToInt(timeLimit));
        if (currentSec != lastSec)
        {
            lastSec = currentSec;
            if (timerText != null)
            {
                timerText.text = "Time: " + currentSec + "s";
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
            EliminatePlayer("Time is up!");
            return;
        }

        lightTimer -= Time.deltaTime;
        if (lightTimer <= 0)
        {
            if (isGreenLight) SwitchToRedLight();
            else SwitchToGreenLight();
        }

        if (!isGreenLight)
        {
            if (currentReactionTime > 0)
            {
                currentReactionTime -= Time.deltaTime;
            }
            else
            {
                // Joystick movement check
                float moveInput = SimpleMobileJoystick.InputDirection.magnitude; 
                bool pcMovement = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;

                if (moveInput > 0.1f || pcMovement)
                {
                    EliminatePlayer("Moved during RED LIGHT!");
                }
            }
        }
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

    private void EliminatePlayer(string reason)
    {
        StopMiniGame();
        AnimateStatusText("ELIMINATED!", Color.red, 1.3f);
        MobSquadGameManager.Instance.OnLocalPlayerEliminated();
    }
}