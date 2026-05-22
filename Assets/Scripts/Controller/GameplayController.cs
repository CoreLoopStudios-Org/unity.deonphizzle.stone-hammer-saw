using UnityEngine;
using Fusion;

// Fusion এ মাল্টিপ্লেয়ার স্ক্রিপ্ট NetworkBehaviour থেকে আসে
public class GameplayController : NetworkBehaviour
{
    private UIManager uiManager;
    private int myWeaponIndex = -1;
    private int opponentWeaponIndex = -1;
    private int round = 1;
    private int p1Score = 0, p2Score = 0;

    private void Awake() => uiManager = FindAnyObjectByType<UIManager>();

    // PUN 2 এর [PunRPC] এর বদলে Fusion এর [Rpc]
    // RpcSources.All মানে যে কেউ কল করতে পারবে, RpcTargets.All মানে সবার কাছে যাবে
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_TriggerGameLoading()
    {
        // ২য় লোডিং প্যানেল দেখাবে এবং ৩ সেকেন্ড পর গেম শুরু করবে
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

    // অস্ত্র সিলেক্ট করা (UI বাটন থেকে কল হবে)
    public void SelectWeapon(int weaponIndex)
    {
        if (myWeaponIndex != -1) return;
        myWeaponIndex = weaponIndex;
        
        // অন্য প্লেয়ারকে জানিয়ে দেওয়া (RpcTargets.Proxies মানে আমি ছাড়া বাকি সবাই)
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
        if (iWon) p1Score++; else p2Score++;

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