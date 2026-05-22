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
        // গেম ওপেন হলে প্রথম লোডিং শুরু হবে
        ShowPanel(appLoadingPanel);
        StartAppLoading();
    }

    private void StartAppLoading()
    {
        appLoadingBar.fillAmount = 0f;
        
        // টেক্সট ব্লিঙ্ক (Blink) করার অ্যানিমেশন
        if(appLoadingText != null)
            appLoadingText.DOFade(0f, 0.5f).SetLoops(-1, LoopType.Yoyo);

        // ৩ সেকেন্ডের লোডিং
        appLoadingBar.DOFillAmount(1f, 3f).OnComplete(() =>
        {
            if(appLoadingText != null) appLoadingText.DOKill();
            ShowPanel(characterSelectionPanel); // লোডিং শেষে ক্যারেক্টার সিলেকশন
        });
    }

    // ক্যারেক্টার সিলেক্ট করার জন্য (০ থেকে ৫ পর্যন্ত ইনডেক্স দেবেন বাটনে)
    public void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        Debug.Log("Character Selected: " + index); // কনসোলে দেখার জন্য লগ
    }

    // CharacterSelectionPanel এর 'Next' বাটনের জন্য
    public void GoToGameSelection()
    {
        // নাম ইনপুট ফিল্ড ঠিকমতো কাজ করছে কি না চেক করা
        if (playerNameInput == null || string.IsNullOrEmpty(playerNameInput.text))
        {
            Debug.LogWarning("Please enter your name!");
            return;
        }

        // ক্যারেক্টার সিলেক্ট করা হয়েছে কি না চেক করা
        if (selectedCharacterIndex == -1)
        {
            Debug.LogWarning("Please select a character!");
            return;
        }

        // সব ঠিক থাকলে Game-Selection-Panel এ নিয়ে যাবে
        ShowPanel(gameSelectionPanel);
    }

    // ম্যাচমেকিং হওয়ার পর গেমের স্পেসিফিক লোডিং (মাস্টার ক্লায়েন্ট কল করবে)
    public void StartGameSpecificLoading(System.Action onComplete)
    {
        ShowPanel(gameLoadingPanel);
        gameLoadingBar.fillAmount = 0f;
        gameLoadingBar.DOFillAmount(1f, 3f).OnComplete(() =>
        {
            onComplete?.Invoke(); // ৩ সেকেন্ড পর গেমপ্লে শুরু হবে
        });
    }

    // --- আগের গেমপ্লে UI ফাংশনগুলো ---
    public void ShowWeaponSelect()
    {
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
                .OnUpdate(() => timerText.text = "0" + Mathf.CeilToInt(currentTime).ToString() + ":00");
        }
    }

    public void UpdateRoundUI(int round, int p1Score, int p2Score)
    {
        roundText.text = "Round: " + round + "/3";
        progressBar.DOFillAmount(round / 3f, 0.5f);
    }

    public void SetRoundComplete(int roundIndex)
    {
        if (roundIndex >= 0 && roundIndex < roundCheckmarks.Length)
            roundCheckmarks[roundIndex].gameObject.SetActive(true);
    }

    public void ShowCharacterBattle() => ShowPanel(characterPanel);
    public void ShowWinScreen() => ShowPanel(winPanel);
    public void ShowLossScreen() => ShowPanel(lossPanel);

    private void ShowPanel(GameObject panelToShow)
    {
        appLoadingPanel.SetActive(false);
        characterSelectionPanel.SetActive(false);
        gameSelectionPanel.SetActive(false);
        gameLoadingPanel.SetActive(false);
        weaponSelectPanel.SetActive(false);
        characterPanel.SetActive(false);
        winPanel.SetActive(false);
        lossPanel.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
    }
}