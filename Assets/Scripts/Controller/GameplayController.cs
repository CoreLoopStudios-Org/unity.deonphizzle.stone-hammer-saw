using UnityEngine;
using Fusion;

public class GameplayController : NetworkBehaviour
{
    private UIManager uiManager;

    // Fusion এ ভেরিয়েবলগুলো সিঙ্ক করার জন্য [Networked] ব্যবহার করা হয়
    [Networked] private int round { get; set; }
    [Networked] private int p1Score { get; set; }
    [Networked] private int p2Score { get; set; }

    private int myWeaponIndex = -1;
    private int opponentWeaponIndex = -1;

    private void Awake() => uiManager = FindAnyObjectByType<UIManager>();

    public override void Spawned()
    {
        // গেম শুরুর সময় স্কোর রিসেট
        if (Object.HasStateAuthority)
        {
            round = 1;
            p1Score = 0;
            p2Score = 0;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_TriggerGameLoading()
    {
        uiManager.StartGameSpecificLoading(() => 
        {
            StartGameRound();
        });
    }

    private void StartGameRound()
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
        // দুইজনের অস্ত্র সিলেক্ট না হলে রেজাল্ট ক্যালকুলেট হবে না
        if (myWeaponIndex == -1 || opponentWeaponIndex == -1) return;

        bool iWon = DetermineWinner(myWeaponIndex, opponentWeaponIndex);
        
        // শুধুমাত্র মাস্টার ক্লায়েন্ট স্কোর আপডেট করবে
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