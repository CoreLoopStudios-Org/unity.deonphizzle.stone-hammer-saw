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
    public GameObject mobSquadLoadingPanel;
    public Image mobSquadLoadingBar;

    [Header("Gameplay UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public Image progressBar;
    public Image[] roundCheckmarks;
    public GameObject[] roundCompleteIcons; // Added for Issue 5 progress bar pop animation
    
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
        if (progressBar != null) progressBar.fillAmount = (round - 1) / 3f;
        
        if (characterPanelScoreText != null) 
            characterPanelScoreText.text = $"P1: {myScore}   P2: {enemyScore}";
    }

    public void UpdateProgressBar(int currentRoundCompleted)
    {
        if (progressBar == null) return;

        float targetFill = currentRoundCompleted / 3f;
        progressBar.DOKill();
        progressBar.DOFillAmount(targetFill, 1.0f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                int checkmarkIndex = currentRoundCompleted - 1;

                // Try roundCompleteIcons array first (GameObject[])
                if (roundCompleteIcons != null && checkmarkIndex >= 0 && checkmarkIndex < roundCompleteIcons.Length)
                {
                    GameObject checkmarkGo = roundCompleteIcons[checkmarkIndex];
                    if (checkmarkGo != null)
                    {
                        checkmarkGo.SetActive(true);
                        checkmarkGo.transform.localScale = Vector3.zero;
                        checkmarkGo.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                    }
                }
                // Fallback to roundCheckmarks array (Image[])
                else if (roundCheckmarks != null && checkmarkIndex >= 0 && checkmarkIndex < roundCheckmarks.Length)
                {
                    Image checkmarkImg = roundCheckmarks[checkmarkIndex];
                    if (checkmarkImg != null)
                    {
                        checkmarkImg.gameObject.SetActive(true);
                        checkmarkImg.transform.localScale = Vector3.zero;
                        checkmarkImg.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                    }
                }
            });
    }

    public void SetRoundComplete(int roundIndex)
    {
        UpdateProgressBar(roundIndex + 1);
    }

    public void ShowCharacterBattle() => ShowPanel(characterPanel);

    // রাউন্ড কমপ্লিট টেক্সট আপডেট করে Win/Loss/Draw প্যানেল দেখানো
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

    public void ShowDrawScreen(int round, bool isFinal = false) 
    { 
        ShowPanel(winPanel); // Reuses winPanel but sets custom draw text
        if (winRoundText != null) winRoundText.text = isFinal ? "IT'S A DRAW!" : $"Round {round} ended in a Draw!";
    }

    // NEXT ROUND বাটনে ক্লিক করলে এটি কল হবে
    public void OnNextRoundButtonClicked()
    {
        if(GameplayController.Instance != null)
            GameplayController.Instance.TriggerNextRound();
    }

    // Pony Pack বাটনে ক্লিক করলে এটি কল হবে
    public void OnPonyPackButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PonyPackScene");
    }

    // Mob Squad বাটনে ক্লিক করলে এটি কল হবে
    public void OnMobSquadButtonClicked()
    {
        if (mobSquadLoadingPanel != null)
        {
            ShowPanel(mobSquadLoadingPanel);
            if (mobSquadLoadingBar != null)
            {
                mobSquadLoadingBar.fillAmount = 0f;
                mobSquadLoadingBar.DOFillAmount(1f, 3f).OnComplete(() =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Mob Squad 3d world scene");
                });
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Mob Squad 3d world scene");
            }
        }
        else
        {
            // Fallback: use gameLoadingPanel
            StartGameSpecificLoading(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Mob Squad 3d world scene");
            });
        }
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
        if (mobSquadLoadingPanel != null) mobSquadLoadingPanel.SetActive(false);

        panelToShow.SetActive(true);
    }
}