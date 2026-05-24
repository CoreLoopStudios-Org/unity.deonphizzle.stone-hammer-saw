using UnityEngine;
using Fusion;
using System.Linq;

public class GameplayController : NetworkBehaviour
{
    public static GameplayController Instance; 
    private UIManager uiManager;

    [Networked] private int round { get; set; }
    [Networked] private int masterScore { get; set; } 
    [Networked] private int clientScore { get; set; } 

    // সার্ভার-সাইড ট্র্যাকিং (শুধু মাস্টার জানবে কে কী সিলেক্ট করেছে)
    private int masterSelectedWeapon = -1;
    private int clientSelectedWeapon = -1;
    private int playersReadyForNextRound = 0;

    // লোকাল ট্র্যাকিং
    private int myWeaponIndex = -1;
    private int opponentWeaponIndex = -1;
    
    private bool isNetworkReady = false; 
    private bool iWonCurrentRound = false; 
    private bool isCurrentRoundDraw = false;
    private bool hasClickedNextRound = false; // বাটন স্প্যামিং প্রোটেকশন

    private bool isHostReady = false;
    private bool isClientReady = false;

    private void Awake()
    {
        Instance = this; 
        uiManager = FindObjectOfType<UIManager>(); 
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
            isHostReady = true;
            Debug.Log("Host is ready, waiting for client reporting...");

            TryStartGameLoading();
        }
        else
        {
            Debug.Log("Client reporting ready to Host...");
            RPC_ReportReadyToHost();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReportReadyToHost()
    {
        isClientReady = true;
        Debug.Log("Host received: Client is ready!");
        TryStartGameLoading();
    }

    private void TryStartGameLoading()
    {
        if (Object.HasStateAuthority && isHostReady && isClientReady)
        {
            Debug.Log("Both Host and Client are ready! Triggering synchronized loading bar...");
            RPC_StartLoadingForEveryone();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartLoadingForEveryone()
    {
        Debug.Log("Loading panel starting on this device...");
        if (uiManager != null)
        {
            uiManager.StartGameSpecificLoading(() => 
            {
                StartGameRound();
            });
        }
    }

    public void StartGameRound()
    {
        if (!isNetworkReady)
        {
            Invoke("StartGameRound", 0.5f); 
            return;
        }

        if (Object.HasStateAuthority)
        {
            RPC_StartRoundForEveryone();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartRoundForEveryone()
    {
        // লোকাল ভ্যালু ও বাটন স্ট্যাটাস রিসেট
        myWeaponIndex = -1;
        opponentWeaponIndex = -1;
        iWonCurrentRound = false;
        isCurrentRoundDraw = false;
        hasClickedNextRound = false;

        int myScore = Object.HasStateAuthority ? masterScore : clientScore;
        int enemyScore = Object.HasStateAuthority ? clientScore : masterScore;

        if (uiManager != null)
        {
            uiManager.ShowWeaponSelect();
            uiManager.UpdateRoundUI(round, myScore, enemyScore);
        }

        // Reset and start spin slot machine
        SlotMachineManager slotMachine = FindObjectOfType<SlotMachineManager>();
        if (slotMachine != null)
        {
            slotMachine.ResetAndStartSpin();
        }

        // সার্ভার-সাইড টাইমআউট (১০ সেকেন্ড পর অটোমেটিক রেজাল্ট, কেউ AFK থাকলে)
        if (Object.HasStateAuthority)
        {
            CancelInvoke("ServerForceEndRound");
            Invoke("ServerForceEndRound", 10f);
        }
    }

    public void SelectWeapon(int weaponIndex)
    {
        if (!isNetworkReady || myWeaponIndex != -1) return;
        
        myWeaponIndex = weaponIndex;
        Debug.Log($"Weapon Selected: {weaponIndex}. Sending to Server...");
        
        // স্টেট অথরিটির (Master) কাছে নিজের সিলেকশন পাঠানো
        RPC_SubmitWeapon(weaponIndex, Object.HasStateAuthority);
    }

    // =======================================================
    // সিকিউর সার্ভার লজিক
    // =======================================================

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitWeapon(int weaponIndex, bool isMasterClient)
    {
        if (isMasterClient) masterSelectedWeapon = weaponIndex;
        else clientSelectedWeapon = weaponIndex;

        // দুজনই সিলেক্ট করে ফেললে রেজাল্ট ক্যালকুলেট হবে
        if (masterSelectedWeapon != -1 && clientSelectedWeapon != -1)
        {
            ResolveRoundOnServer();
        }
    }

    private void ServerForceEndRound()
    {
        // টাইমআউট হলে যারা সিলেক্ট করেনি তাদের র‍্যান্ডম অস্ত্র দেওয়া হবে
        if (masterSelectedWeapon == -1) masterSelectedWeapon = Random.Range(0, 5);
        if (clientSelectedWeapon == -1) clientSelectedWeapon = Random.Range(0, 5);
        
        ResolveRoundOnServer();
    }

    private void ResolveRoundOnServer()
    {
        CancelInvoke("ServerForceEndRound");

        bool isDraw = masterSelectedWeapon == clientSelectedWeapon;
        bool masterWon = DetermineWinner(masterSelectedWeapon, clientSelectedWeapon);

        // ড্র হলে স্কোর আপডেট হবে না
        if (!isDraw)
        {
            if (masterWon) masterScore++;
            else clientScore++;
        }

        // রেজাল্ট দুজনকে একসাথে ব্রডকাস্ট করা
        RPC_BroadcastRoundResult(masterSelectedWeapon, clientSelectedWeapon);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastRoundResult(int finalMasterWeapon, int finalClientWeapon)
    {
        // সার্ভার ভ্যালু রিসেট
        if (Object.HasStateAuthority)
        {
            masterSelectedWeapon = -1;
            clientSelectedWeapon = -1;
            playersReadyForNextRound = 0;
        }

        // লোকাল ক্লায়েন্টকে তার এবং প্রতিপক্ষের অস্ত্র জানিয়ে দেওয়া
        if (Object.HasStateAuthority)
        {
            myWeaponIndex = finalMasterWeapon;
            opponentWeaponIndex = finalClientWeapon;
        }
        else
        {
            myWeaponIndex = finalClientWeapon;
            opponentWeaponIndex = finalMasterWeapon;
        }

        isCurrentRoundDraw = myWeaponIndex == opponentWeaponIndex;
        iWonCurrentRound = !isCurrentRoundDraw && DetermineWinner(myWeaponIndex, opponentWeaponIndex);

        // UI আপডেট
        uiManager.SetRoundComplete(round - 1);
        uiManager.ShowCharacterBattle();
        
        Invoke("ShowRoundResultPanel", 5f);
    }

    private void ShowRoundResultPanel()
    {
        if (isCurrentRoundDraw) uiManager.ShowDrawScreen(round, false);
        else if (iWonCurrentRound) uiManager.ShowWinScreen(round, false);
        else uiManager.ShowLossScreen(round, false);
    }

    // =======================================================
    // নেক্সট রাউন্ড সিঙ্ক লজিক
    // =======================================================

    public void TriggerNextRound()
    {
        // স্প্যাম ক্লিক বন্ধ করা হলো
        if (hasClickedNextRound) return;
        hasClickedNextRound = true;

        Debug.Log("Next Round triggered. Waiting for opponent...");
        RPC_SetReadyForNextRound();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReadyForNextRound()
    {
        playersReadyForNextRound++;
        
        // দুজন রেডি হলে তবেই নতুন রাউন্ড বা ফাইনাল রেজাল্ট আসবে
        if (playersReadyForNextRound == 2)
        {
            if (round < 3) 
            {
                round++; 
                RPC_StartNextRoundSynced();
            }
            else 
            {
                RPC_ShowFinalResult();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartNextRoundSynced()
    {
        Invoke("StartGameRound", 0.2f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowFinalResult()
    {
        bool iAmMaster = Object.HasStateAuthority;
        bool masterWonFinal = masterScore > clientScore;
        bool iWonFinal = iAmMaster ? masterWonFinal : !masterWonFinal;

        // যদি ফাইনাল স্কোর সমান হয় (Draw)
        if (masterScore == clientScore)
        {
            uiManager.ShowDrawScreen(round, true); 
        }
        else if (iWonFinal) 
        {
            uiManager.ShowWinScreen(round, true);
        }
        else 
        {
            uiManager.ShowLossScreen(round, true);
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