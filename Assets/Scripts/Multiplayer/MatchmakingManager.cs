using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
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
            // RPC এর মাধ্যমে গেমে সবাইকে গেম রাউন্ড শুরু করার সিগন্যাল দিচ্ছে
            GetComponent<GameplayController>().photonView.RPC("StartGameRoundRPC", RpcTarget.All);
        }
    }
}