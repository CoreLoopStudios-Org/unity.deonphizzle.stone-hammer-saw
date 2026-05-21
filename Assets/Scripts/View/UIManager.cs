using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loadingPanel, weaponSelectPanel, characterPanel, winPanel, lossPanel;
    
    [Header("Progress & UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText; 
    public Image progressBar; 
    public Image[] roundCheckmarks; 

    private void Start() => ShowPanel(loadingPanel);

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

    // নতুন ফিচার: রাউন্ড এবং প্রগ্রেস বার আপডেট
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
        loadingPanel.SetActive(false);
        weaponSelectPanel.SetActive(false);
        characterPanel.SetActive(false);
        winPanel.SetActive(false);
        lossPanel.SetActive(false);
        if (panelToShow != null) panelToShow.SetActive(true);
    }
}