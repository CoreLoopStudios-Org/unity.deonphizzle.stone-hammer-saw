using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("App Flow Panels")]
    public GameObject appLoadingPanel;         // 1. Loading-Panel
    public GameObject characterSelectionPanel; // 2. CharacterSelectionPanel
    public GameObject gameSelectionPanel;      // 3. Game-Selection-Panel
    public GameObject gameLoadingPanel;        // 4. Stone-saw-hammer-Panel

    [Header("Game Panels")]
    public GameObject weaponSelectPanel;
    public GameObject characterPanel;
    public GameObject winPanel;
    public GameObject lossPanel;

    [Header("UI Elements")]
    public Image appLoadingBar;       // Loading-Panel এর Filled-progress
    public TextMeshProUGUI appLoadingText; 
    public TMP_InputField playerNameInput; 
    public Image gameLoadingBar;      // Stone-saw-hammer-Panel এর Filled-progress

    [Header("Gameplay UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public Image progressBar;
    public Image[] roundCheckmarks;

    private int selectedCharacterIndex = -1;

    private void Start()
    {
        Debug.Log("UIManager: Starting App Loading...");
        ShowPanel(appLoadingPanel);
        StartAppLoading();
    }

    private void StartAppLoading()
    {
        if (appLoadingBar == null)
        {
            Debug.LogError("UIManager: App Loading Bar is NULL! Please assign it in the Inspector.");
            ShowPanel(characterSelectionPanel);
            return;
        }

        appLoadingBar.fillAmount = 0f;
        
        if(appLoadingText != null)
            appLoadingText.DOFade(0f, 0.5f).SetLoops(-1, LoopType.Yoyo);

        appLoadingBar.DOFillAmount(1f, 3f).OnComplete(() =>
        {
            if(appLoadingText != null) appLoadingText.DOKill();
            Debug.Log("UIManager: App Loading Complete. Going to Character Selection.");
            ShowPanel(characterSelectionPanel); 
        });
    }

    public void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        Debug.Log("UIManager: Character Selected -> " + index);
    }

    public void GoToGameSelection()
    {
        if (playerNameInput == null || string.IsNullOrEmpty(playerNameInput.text))
        {
            Debug.LogWarning("UIManager: Please enter your name!");
            return;
        }

        if (selectedCharacterIndex == -1)
        {
            Debug.LogWarning("UIManager: Please select a character!");
            return;
        }

        Debug.Log("UIManager: Character and Name accepted. Moving to Game Selection.");
        ShowPanel(gameSelectionPanel);
    }

    // ম্যাচমেকিং হওয়ার পর গেমের স্পেসিফিক লোডিং
    public void StartGameSpecificLoading(System.Action onComplete)
    {
        Debug.Log("UIManager: StartGameSpecificLoading Triggered!");
        ShowPanel(gameLoadingPanel);

        if (gameLoadingBar != null)
        {
            gameLoadingBar.fillAmount = 0f;
            gameLoadingBar.DOFillAmount(1f, 3f).OnComplete(() =>
            {
                Debug.Log("UIManager: Game Specific Loading Complete! Calling onComplete Action.");
                onComplete?.Invoke(); 
            });
        }
        else
        {
            Debug.LogError("UIManager: Game Loading Bar is NULL! Skipping animation.");
            onComplete?.Invoke(); // এরর থাকলেও যেন গেম আটকে না যায়
        }
    }

    public void ShowWeaponSelect()
    {
        Debug.Log("UIManager: Showing Weapon Select Panel");
        ShowPanel(weaponSelectPanel);
        StartTimer();
    }

    private void StartTimer()
    {
        if (timerText != null)
        {
            float currentTime = 3f;
            DOTween.To(() => currentTime, x => currentTime = x, 0f, 3f)
                .SetEase(Ease.Linear)
                .OnUpdate(() => timerText.text = "0" + Mathf.CeilToInt(currentTime).ToString() + ":00")
                .OnComplete(() => Debug.Log("UIManager: Weapon Timer Finished"));
        }
        else
        {
            Debug.LogWarning("UIManager: timerText is NULL!");
        }
    }

    public void UpdateRoundUI(int round, int p1Score, int p2Score)
    {
        if (roundText != null) roundText.text = "Round: " + round + "/3";
        if (progressBar != null) progressBar.DOFillAmount(round / 3f, 0.5f);
    }

    public void SetRoundComplete(int roundIndex)
    {
        if (roundCheckmarks != null && roundIndex >= 0 && roundIndex < roundCheckmarks.Length)
        {
            if (roundCheckmarks[roundIndex] != null)
                roundCheckmarks[roundIndex].gameObject.SetActive(true);
        }
    }

    public void ShowCharacterBattle() => ShowPanel(characterPanel);
    public void ShowWinScreen() => ShowPanel(winPanel);
    public void ShowLossScreen() => ShowPanel(lossPanel);

    // সেফটি চেক সহ প্যানেল পরিবর্তন
    private void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow == null)
        {
            Debug.LogError("UIManager: You are trying to show a NULL panel. Check the Inspector!");
            return;
        }

        Debug.Log($"UIManager: Activating Panel -> {panelToShow.name}");

        // null চেক করে প্যানেল অফ করা (যাতে কোনো প্যানেল লিঙ্ক না থাকলেও এরর না দেয়)
        if (appLoadingPanel != null) appLoadingPanel.SetActive(false);
        if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);
        if (gameSelectionPanel != null) gameSelectionPanel.SetActive(false);
        if (gameLoadingPanel != null) gameLoadingPanel.SetActive(false);
        if (weaponSelectPanel != null) weaponSelectPanel.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (lossPanel != null) lossPanel.SetActive(false);

        panelToShow.SetActive(true);
    }
}