using UnityEngine;
using Photon.Pun;

public class GameplayController : MonoBehaviourPunCallbacks
{
    private UIManager uiManager;

    private int myWeaponIndex = -1;       // 0: Stone, 1: Saw, 2: Hammer
    private int opponentWeaponIndex = -1;

    private void Awake()
    {
        // UIManager খুঁজে বের করা
        uiManager = FindAnyObjectByType<UIManager>();
    }
    
    public void StartGameRound()
    {
        myWeaponIndex = -1;
        opponentWeaponIndex = -1;
        uiManager.ShowWeaponSelect();
    }
    
    public void SelectWeapon(int weaponIndex)
    {
        if (myWeaponIndex != -1) return; 

        myWeaponIndex = weaponIndex;
        
        photonView.RPC("ReceiveOpponentWeapon", RpcTarget.Others, weaponIndex);
        
        CheckRoundResult();
    }

    [PunRPC]
    public void ReceiveOpponentWeapon(int weaponIndex)
    {
        opponentWeaponIndex = weaponIndex;
        CheckRoundResult();
    }

    private void CheckRoundResult()
    {
        if (myWeaponIndex == -1 || opponentWeaponIndex == -1) return;
        
        uiManager.ShowCharacterBattle();
        
        if (myWeaponIndex == opponentWeaponIndex)
        {
            Debug.Log("Draw!");
            Invoke("StartGameRound", 2f);
        }
        else if ((myWeaponIndex == 0 && opponentWeaponIndex == 1) || // Stone > Saw
                 (myWeaponIndex == 1 && opponentWeaponIndex == 2) || // Saw > Hammer
                 (myWeaponIndex == 2 && opponentWeaponIndex == 0))   // Hammer > Stone
        {
            Invoke("TriggerWin", 2f);
        }
        else
        {
            Invoke("TriggerLoss", 2f);
        }
    }

    private void TriggerWin()
    {
        uiManager.ShowWinScreen();
    }

    private void TriggerLoss()
    {
        uiManager.ShowLossScreen();
    }
}