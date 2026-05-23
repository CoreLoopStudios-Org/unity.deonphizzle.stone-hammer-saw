using UnityEngine;
using Fusion;

public class GameplayController : NetworkBehaviour
{
    public static GameplayController Instance; 
    private UIManager uiManager;

    [Networked] private int round { get; set; }
    [Networked] private int p1Score { get; set; }
    [Networked] private int p2Score { get; set; }

    private int myWeaponIndex = -1;
    private int opponentWeaponIndex = -1;

    private void Awake()
    {
        Instance = this; 
        uiManager = FindAnyObjectByType<UIManager>(); 
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            round = 1;
            p1Score = 0;
            p2Score = 0;
        }
    }

    // এখন এটি public, তাই MatchmakingManager সরাসরি একে কল করতে পারবে
    public void StartGameRound()
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
        RPC_ReceiveOpponentWeapon(weaponIndex);
        CheckRoundResult();
    }

    [Rpc(RpcSources.All, RpcTargets.Proxies)]
    public void RPC_ReceiveOpponentWeapon(int weaponIndex)
    {
        opponentWeaponIndex = weaponIndex;
        CheckRoundResult();
    }

    private void CheckRoundResult()
    {
        if (myWeaponIndex == -1 || opponentWeaponIndex == -1) return;

        bool iWon = DetermineWinner(myWeaponIndex, opponentWeaponIndex);
        
        if (Object.HasStateAuthority)
        {
            if (iWon) p1Score++; else p2Score++;
        }

        uiManager.SetRoundComplete(round - 1);
        uiManager.ShowCharacterBattle();
        Invoke(iWon ? "TriggerWin" : "TriggerLoss", 2f);
    }

    private bool DetermineWinner(int mine, int opp)
    {
        if (mine == opp) return false; 
        if ((mine == 2 && (opp == 0 || opp == 3 || opp == 1)) || 
            (mine == 4 && (opp == 3 || opp == 0)) ||
            (mine == 1 && opp == 0) || (mine == 0 && opp == 3)) return true;
        return false;
    }

    private void TriggerWin()
    {
        if (round < 3) { round++; Invoke("StartGameRound", 2f); }
        else uiManager.ShowWinScreen();
    }

    private void TriggerLoss()
    {
        if (round < 3) { round++; Invoke("StartGameRound", 2f); }
        else uiManager.ShowLossScreen();
    }
}