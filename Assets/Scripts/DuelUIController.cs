using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class DuelUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loadingPanel;
    public GameObject weaponSelectPanel;
    public GameObject stoneSawHammerPanel;
    public GameObject characterPanel;
    public GameObject winPanel;
    public GameObject lossPanel;
    public GameObject drawPanel;

    [Header("HUD")]
    public TextMeshProUGUI timerText;

    private void Start()
    {
        ShowPanel(loadingPanel);
    }

    private void Update()
    {
        // Update timer if needed, but logic usually handled by DuelManager
    }

    public void ShowPanel(GameObject panelToShow)
    {
        loadingPanel.SetActive(panelToShow == loadingPanel);
        weaponSelectPanel.SetActive(panelToShow == weaponSelectPanel);
        stoneSawHammerPanel.SetActive(panelToShow == stoneSawHammerPanel);
        characterPanel.SetActive(panelToShow == characterPanel);
        winPanel.SetActive(panelToShow == winPanel);
        lossPanel.SetActive(panelToShow == lossPanel);
        if (drawPanel != null) drawPanel.SetActive(panelToShow == drawPanel);
    }

    // Called by UI Buttons
    public void OnWeaponSelected(int weaponIndex)
    {
        WeaponType selected = (WeaponType)weaponIndex;
        DuelManager.Instance.CommitSelection(selected);
        // Optionally disable buttons or show "Wait" state
    }

    public void OnDuelStarted()
    {
        ShowPanel(weaponSelectPanel);
    }

    public void OnResultReceived(int winnerActorId)
    {
        if (winnerActorId == -1)
        {
            ShowPanel(drawPanel);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == winnerActorId)
        {
            ShowPanel(winPanel);
        }
        else
        {
            ShowPanel(lossPanel);
        }
    }
}
