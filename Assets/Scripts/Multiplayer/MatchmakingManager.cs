using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    private bool isConnectingToPlay = false;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // গেম স্টার্ট হওয়ার সাথে সাথেই ব্যাকগ্রাউন্ডে কানেকশন শুরু করবে
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // Game-Selection-Panel এর Play বাটনে এটি লিঙ্ক করা আছে
    public void StartMatchmaking()
    {
        // সার্ভার যদি পুরোপুরি রেডি থাকে, তবে সরাসরি রুমে জয়েন করবে
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Server is ready. Joining Room...");
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            // কোনো কারণে ডিসকানেক্ট হয়ে গেলে আবার কানেক্ট করবে
            Debug.Log("Connecting to Server first...");
            isConnectingToPlay = true;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server.");
        
        // যদি Play বাটনে ক্লিক করার কারণে কানেক্ট হয়ে থাকে, তবেই রুমে ঢুকবে
        if (isConnectingToPlay)
        {
            PhotonNetwork.JoinRandomRoom();
            isConnectingToPlay = false;
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message) 
    {
        Debug.Log("No empty room found. Creating a new one...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom() 
    {
        Debug.Log("Joined Room Successfully!");
        CheckPlayers();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) 
    {
        Debug.Log("An opponent has joined!");
        CheckPlayers();
    }

    private void CheckPlayers()
    {
        // ২ জন প্লেয়ার হলে গেম লোডিং শুরু করার সিগন্যাল দেবে
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            GetComponent<GameplayController>().photonView.RPC("TriggerGameLoadingRPC", RpcTarget.All);
        }
    }
}