using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // Game-Selection-Panel এর Play বাটনে এটি লিঙ্ক করবেন
    public void StartMatchmaking()
    {
        Debug.Log("Connecting to Server...");
        if (!PhotonNetwork.IsConnected) 
            PhotonNetwork.ConnectUsingSettings();
        else 
            PhotonNetwork.JoinRandomRoom();
    }

    public override void OnConnectedToMaster() => PhotonNetwork.JoinRandomRoom();

    public override void OnJoinRandomFailed(short returnCode, string message) 
        => PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });

    public override void OnJoinedRoom() => CheckPlayers();

    public override void OnPlayerEnteredRoom(Player newPlayer) => CheckPlayers();

    private void CheckPlayers()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            // ২ জন জয়েন হলে গেম লোডিং শুরু করার জন্য RPC কল
            GetComponent<GameplayController>().photonView.RPC("TriggerGameLoadingRPC", RpcTarget.All);
        }
    }
}