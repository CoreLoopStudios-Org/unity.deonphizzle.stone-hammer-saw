using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("App Flow Panels")]
    public GameObject appLoadingPanel;         
    public GameObject characterSelectionPanel; 
    public GameObject gameSelectionPanel;      
    public GameObject gameLoadingPanel;        

    [Header("Game Panels")]
    public GameObject weaponSelectPanel;
    public GameObject characterPanel;
    public GameObject winPanel;
    public GameObject lossPanel;

    [Header("UI Elements")]
    public Image appLoadingBar;       
    public TextMeshProUGUI appLoadingText; 
    public TMP_InputField playerNameInput; 
    public Image gameLoadingBar;      

    [Header("Gameplay UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public Image progressBar;
    public Image[] roundCheckmarks;
    
    [Header("Round Result Texts")]
    public TextMeshProUGUI winRoundText;  // Win Panel-এর উপরের Text (e.g., Round 1 completed)
    public TextMeshProUGUI lossRoundText; // Loss Panel-এর উপরের Text

    // Character Panel-এ স্কোর দেখানোর জন্য একটি টেক্সট রাখতে পারেন (ঐচ্ছিক)
    public TextMeshProUGUI characterPanelScoreText; 

    private int selectedCharacterIndex = -1;

    private void Start()
    {
        ShowPanel(appLoadingPanel);
        StartAppLoading();
    }

    private void StartAppLoading()
    {
        if (appLoadingBar == null) return;
        appLoadingBar.fillAmount = 0f;
        
        if(appLoadingText != null)
            appLoadingText.DOFade(0f, 0.5f).SetLoops(-1, LoopType.Yoyo);

        appLoadingBar.DOFillAmount(1f, 3f).OnComplete(() =>
        {
            if(appLoadingText != null) appLoadingText.DOKill();
            ShowPanel(characterSelectionPanel); 
        });
    }

    public void SelectCharacter(int index) => selectedCharacterIndex = index;

    public void GoToGameSelection()
    {
        if (playerNameInput == null || string.IsNullOrEmpty(playerNameInput.text) || selectedCharacterIndex == -1) return;
        ShowPanel(gameSelectionPanel);
    }

    public void StartGameSpecificLoading(System.Action onComplete)
    {
        ShowPanel(gameLoadingPanel);
        if (gameLoadingBar != null)
        {
            gameLoadingBar.fillAmount = 0f;
            gameLoadingBar.DOFillAmount(1f, 3f).OnComplete(() => onComplete?.Invoke());
        }
        else onComplete?.Invoke(); 
    }

    public void ShowWeaponSelect()
    {
        ShowPanel(weaponSelectPanel);
        StartTimer();
    }

    private void StartTimer()
    {
        if (timerText != null)
        {
            float currentTime = 5f; // টাইমার ৩ থেকে ৫ সেকেন্ড করা হলো
            DOTween.To(() => currentTime, x => currentTime = x, 0f, 5f)
                .SetEase(Ease.Linear)
                .OnUpdate(() => timerText.text = "0" + Mathf.CeilToInt(currentTime).ToString() + ":00");
        }
    }

    // লোকাল প্লেয়ার সবসময় বামে (P1) থাকবে, তাই স্কোর সেভাবেই ফরম্যাট করা হবে
    public void UpdateRoundUI(int round, int myScore, int enemyScore)
    {
        if (roundText != null) roundText.text = "Round: " + round + "/3";
        if (progressBar != null) progressBar.DOFillAmount(round / 3f, 0.5f);
        
        if (characterPanelScoreText != null) 
            characterPanelScoreText.text = $"P1: {myScore}   P2: {enemyScore}";
    }

    public void SetRoundComplete(int roundIndex)
    {
        if (roundCheckmarks != null && roundIndex >= 0 && roundIndex < roundCheckmarks.Length)
            if (roundCheckmarks[roundIndex] != null) roundCheckmarks[roundIndex].gameObject.SetActive(true);
    }

    public void ShowCharacterBattle() => ShowPanel(characterPanel);

    // রাউন্ড কমপ্লিট টেক্সট আপডেট করে Win/Loss প্যানেল দেখানো
    public void ShowWinScreen(int round, bool isFinal = false) 
    { 
        ShowPanel(winPanel); 
        if (winRoundText != null) winRoundText.text = isFinal ? "YOU WON THE MATCH!" : $"Round {round} completed ✓";
    }

    public void ShowLossScreen(int round, bool isFinal = false) 
    { 
        ShowPanel(lossPanel); 
        if (lossRoundText != null) lossRoundText.text = isFinal ? "YOU LOST THE MATCH!" : $"Round {round} completed ✓";
    }

    // NEXT ROUND বাটনে ক্লিক করলে এটি কল হবে
    public void OnNextRoundButtonClicked()
    {
        if(GameplayController.Instance != null)
            GameplayController.Instance.TriggerNextRound();
    }

    private void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow == null) return;
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