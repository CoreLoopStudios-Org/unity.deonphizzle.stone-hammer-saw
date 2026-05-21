using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loadingPanel;
    public GameObject weaponSelectPanel;
    public GameObject characterPanel; // Battle/Showdown panel
    public GameObject winPanel;
    public GameObject lossPanel;

    private void Start()
    {
      
        ShowPanel(loadingPanel);
    }

    public void ShowWeaponSelect()
    {
        ShowPanel(weaponSelectPanel);
    }

    public void ShowCharacterBattle()
    {
        ShowPanel(characterPanel);
    }

    public void ShowWinScreen()
    {
        ShowPanel(winPanel);
    }

    public void ShowLossScreen()
    {
        ShowPanel(lossPanel);
    }
    
    private void ShowPanel(GameObject panelToShow)
    {
        loadingPanel.SetActive(false);
        weaponSelectPanel.SetActive(false);
        characterPanel.SetActive(false);
        winPanel.SetActive(false);
        lossPanel.SetActive(false);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
    }
}