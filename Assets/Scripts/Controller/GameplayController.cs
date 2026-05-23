using UnityEngine;
using Fusion;

public class GameplayController : NetworkBehaviour
{
    public static GameplayController Instance; 
    private UIManager uiManager;

    [Networked] private int round { get; set; }
    
    // লোকাল প্লেয়ারকে সবসময় বামে (P1) দেখানোর জন্য স্কোর আলাদা করা হলো
    [Networked] private int masterScore { get; set; } 
    [Networked] private int clientScore { get; set; } 

    private int myWeaponIndex = -1;
    private int opponentWeaponIndex = -1;
    
    private bool isNetworkReady = false; 
    private bool iWonCurrentRound = false; // বর্তমান রাউন্ড কে জিতেছে তা মনে রাখার জন্য

    private void Awake()
    {
        Instance = this; 
        uiManager = FindAnyObjectByType<UIManager>(); 
    }

    public override void Spawned()
    {
        isNetworkReady = true; 
        Debug.Log("✅ Fusion is READY! Network Object Spawned Successfully.");
        
        if (Object.HasStateAuthority)
        {
            round = 1;
            masterScore = 0;
            clientScore = 0;
        }
    }

    public void StartGameRound()
    {
        if (!isNetworkReady)
        {
            Debug.LogWarning("⏳ Fusion is NOT READY yet! Waiting for 0.5 seconds to start the round...");
            Invoke("StartGameRound", 0.5f); 
            return;
        }

        myWeaponIndex = -1;
        opponentWeaponIndex = -1;

        // আমি মাস্টার হলে আমার স্কোর masterScore, নাহলে clientScore
        int myScore = Object.HasStateAuthority ? masterScore : clientScore;
        int enemyScore = Object.HasStateAuthority ? clientScore : masterScore;

        uiManager.ShowWeaponSelect();
        uiManager.UpdateRoundUI(round, myScore, enemyScore);
    }

    public void SelectWeapon(int weaponIndex)
    {
        if (!isNetworkReady)
        {
            Debug.LogWarning("⚠️ Cannot select weapon! Fusion is still loading...");
            return;
        }

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

        bool iAmMaster = Object.HasStateAuthority;
        bool iWon = DetermineWinner(myWeaponIndex, opponentWeaponIndex);
        iWonCurrentRound = iWon;
        
        // শুধু মাস্টার ক্লায়েন্ট গ্লোবাল স্কোর আপডেট করবে
        if (iAmMaster)
        {
            if (iWon) masterScore++; else clientScore++;
        }

        uiManager.SetRoundComplete(round - 1);
        uiManager.ShowCharacterBattle();
        
        // Character Panel ঠিক ৫ সেকেন্ড দেখানোর পর Win/Loss প্যানেল আসবে
        Invoke("ShowRoundResultPanel", 5f);
    }

    private void ShowRoundResultPanel()
    {
        if (iWonCurrentRound) uiManager.ShowWinScreen(round, false);
        else uiManager.ShowLossScreen(round, false);
    }

    // UIManager-এর "NEXT ROUND" বাটনে ক্লিক করলে এটি কল হবে
    public void TriggerNextRound()
    {
        if (round < 3) 
        {
            // রাউন্ড আপডেট শুধু মাস্টার করবে
            if (Object.HasStateAuthority) round++; 
            Invoke("StartGameRound", 0.2f); // একটু ডিলে দিয়ে নতুন রাউন্ড শুরু
        }
        else 
        {
            // ৩ রাউন্ড শেষ! ফাইনাল উইনার চেক করা হচ্ছে
            bool iAmMaster = Object.HasStateAuthority;
            bool masterWonFinal = masterScore > clientScore;
            
            bool iWonFinal = iAmMaster ? masterWonFinal : !masterWonFinal;

            // ফাইনাল রেজাল্ট প্যানেল দেখানো
            if (iWonFinal) uiManager.ShowWinScreen(round, true);
            else uiManager.ShowLossScreen(round, true);
        }
    }

    private bool DetermineWinner(int mine, int opp)
    {
        if (mine == opp) return false; 
        if ((mine == 2 && (opp == 0 || opp == 3 || opp == 1)) || 
            (mine == 4 && (opp == 3 || opp == 0)) ||
            (mine == 1 && opp == 0) || (mine == 0 && opp == 3)) return true;
        return false;
    }
}