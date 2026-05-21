using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        // ২ জন প্লেয়ারের সিন যেন একসাথে লোড হয়
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // সার্ভারে কানেক্ট না থাকলে কানেক্ট করবে
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Server! Trying to join a random room...");
        PhotonNetwork.JoinRandomRoom(); 
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No empty room found. Creating a new room...");
        // সর্বোচ্চ ২ জনের জন্য একটি রুম তৈরি করা
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(null, roomOptions); 
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room Successfully! Waiting for opponent...");
        CheckPlayers();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("An opponent has joined the room!");
        CheckPlayers();
    }

    private void CheckPlayers()
    {
        // যদি রুমে ২ জন প্লেয়ার হয়ে যায় এবং এই প্লেয়ারটি মাস্টার ক্লায়েন্ট হয়
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Room is full! Loading Game Scene...");
            // "GameScene" এর জায়গায় আপনার ২য় সিনের (গেমপ্লে সিন) আসল নাম দেবেন
            PhotonNetwork.LoadLevel("GameScene"); 
        }
    }
}