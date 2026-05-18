using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class DuelManager : MonoBehaviourPunCallbacks
{
    public static DuelManager Instance;

    public float selectionTime = 3f;
    private float timer;
    private bool isSelectionActive = false;

    private Dictionary<int, WeaponType> playerSelections = new Dictionary<int, WeaponType>();
    private Dictionary<int, double> playerCommitTimes = new Dictionary<int, double>();

    public DuelUIController uiController;

    private void Awake()
    {
        Instance = this;
    }

    [PunRPC]
    public void StartDuel()
    {
        playerSelections.Clear();
        playerCommitTimes.Clear();
        timer = selectionTime;
        isSelectionActive = true;
        Debug.Log("Duel Started! 3 seconds to select.");
        
        if (uiController != null)
        {
            uiController.OnDuelStarted();
        }
    }

    private void Update()
    {
        if (!isSelectionActive) return;

        if (PhotonNetwork.IsMasterClient)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                EndSelectionPhase();
            }
        }
    }

    public void CommitSelection(WeaponType weapon)
    {
        if (!isSelectionActive) return;
        
        photonView.RPC("RPC_SubmitSelection", RpcTarget.MasterClient, weapon, PhotonNetwork.ServerTimestamp);
        // UI: Feedback that selection is made
    }

    [PunRPC]
    private void RPC_SubmitSelection(WeaponType weapon, int timestamp, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int actorId = info.Sender.ActorNumber;
        if (!playerSelections.ContainsKey(actorId))
        {
            playerSelections[actorId] = weapon;
            playerCommitTimes[actorId] = (double)timestamp;
            Debug.Log($"Player {actorId} selected {weapon} at {timestamp}");
        }

        if (playerSelections.Count == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            EndSelectionPhase();
        }
    }

    private void EndSelectionPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        isSelectionActive = false;

        // Resolve winners
        ResolveResults();
    }

    private void ResolveResults()
    {
        // Simple 2-player resolution for now
        Player[] players = PhotonNetwork.PlayerList;
        if (players.Length < 2) return;

        int p1Id = players[0].ActorNumber;
        int p2Id = players[1].ActorNumber;

        WeaponType w1 = playerSelections.ContainsKey(p1Id) ? playerSelections[p1Id] : WeaponType.None;
        WeaponType w2 = playerSelections.ContainsKey(p2Id) ? playerSelections[p2Id] : WeaponType.None;

        int winnerId = -1; // -1 for Draw

        if (w1 == w2) winnerId = -1; // Draw on same weapon
        else winnerId = GetWinner(p1Id, w1, p2Id, w2);

        photonView.RPC("RPC_ShowResults", RpcTarget.All, winnerId);
    }

    private int GetWinner(int p1, WeaponType w1, int p2, WeaponType w2)
    {
        // Win Logic Implementation
        if (Beats(w1, w2)) return p1;
        if (Beats(w2, w1)) return p2;
        return -1; // Fallback to Draw
    }

    private bool Beats(WeaponType a, WeaponType b)
    {
        switch (a)
        {
            case WeaponType.MiniSaw:
                return b == WeaponType.MiniStone;
            case WeaponType.BigSaw:
                return b == WeaponType.MiniSaw || b == WeaponType.Hammer;
            case WeaponType.Hammer:
                return b == WeaponType.MiniSaw || b == WeaponType.MiniStone;
            case WeaponType.BigStone:
                return b == WeaponType.Hammer || b == WeaponType.MiniStone;
            default:
                return false;
        }
    }

    [PunRPC]
    private void RPC_ShowResults(int winnerId)
    {
        isSelectionActive = false;
        
        if (uiController != null)
        {
            uiController.OnResultReceived(winnerId);
        }

        if (winnerId == -1)
        {
            Debug.Log("Result: DRAW");
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == winnerId)
        {
            Debug.Log("Result: YOU WIN!");
            // UI: Show Win Panel
        }
        else
        {
            Debug.Log("Result: YOU LOSE!");
            // UI: Show Loss Panel
        }
    }
}
