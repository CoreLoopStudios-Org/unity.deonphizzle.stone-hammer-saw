using UnityEngine;
using Photon.Pun;

public class GameplayController : MonoBehaviourPunCallbacks
{
    private UIManager uiManager;
    private int myWeaponIndex = -1, opponentWeaponIndex = -1;
    private int round = 1;
    private int p1Score = 0, p2Score = 0;

    private void Awake() => uiManager = FindAnyObjectByType<UIManager>();

    [PunRPC]
    public void StartGameRoundRPC()
    {
        myWeaponIndex = -1;
        opponentWeaponIndex = -1;
        uiManager.ShowWeaponSelect();
        uiManager.UpdateRoundUI(round, p1Score, p2Score);
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

        bool iWon = DetermineWinner(myWeaponIndex, opponentWeaponIndex);
        if (iWon) p1Score++; else p2Score++;

        uiManager.SetRoundComplete(round - 1);
        uiManager.ShowCharacterBattle();

        Invoke(iWon ? "TriggerWin" : "TriggerLoss", 2f);
    }

    private bool DetermineWinner(int mine, int opp)
    {
        // লজিক: 0=MiniSaw, 1=Hammer, 2=BigSaw, 3=MiniStone, 4=BigStone
        if (mine == opp) return false; // ড্র হলে লজিক অনুযায়ী হ্যান্ডেল করুন
        if ((mine == 2 && (opp == 0 || opp == 3 || opp == 1)) || 
            (mine == 4 && (opp == 3 || opp == 0)) ||
            (mine == 1 && opp == 0) || (mine == 0 && opp == 3)) return true;
        return false;
    }

    private void TriggerWin()
    {
        if (round < 3) { round++; Invoke("StartGameRoundRPC", 2f); }
        else uiManager.ShowWinScreen();
    }

    private void TriggerLoss()
    {
        if (round < 3) { round++; Invoke("StartGameRoundRPC", 2f); }
        else uiManager.ShowLossScreen();
    }
}