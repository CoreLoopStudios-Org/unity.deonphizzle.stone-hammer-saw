using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;

public class MatchmakingManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Inspector-এ নতুন তৈরি করা "FusionRunner" গেম অবজেক্টটি এখানে ড্র্যাগ করে দিন
    public NetworkRunner runner;

    public async void StartMatchmaking()
    {
        Debug.Log("Connecting to Fusion Server...");

        if (runner == null)
        {
            runner = FindAnyObjectByType<NetworkRunner>();
        }

        if (runner == null)
        {
            Debug.Log("No NetworkRunner found in scene. Creating a new 'FusionRunner' dynamically...");
            GameObject runnerGo = new GameObject("FusionRunner");
            runner = runnerGo.AddComponent<NetworkRunner>();
        }

        // কলব্যাকগুলো রেজিস্টার করা
        runner.AddCallbacks(this);

        // Shared Mode-এ গেম শুরু করা
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = null,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
            Debug.Log("Successfully joined the Fusion room!");
        else
            Debug.LogError($"Failed to join: {result.ShutdownReason}");
    }

    // MatchmakingManager.cs এর OnPlayerJoined এ পরিবর্তন করুন:
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Joined! Total Players: {runner.ActivePlayers.Count()}");
    
        // রুম ফুল হলে গেম লোড করার RPC কল করা
        if (runner.IsSharedModeMasterClient && runner.ActivePlayers.Count() == 2)
        {
            var controller = FindAnyObjectByType<GameplayController>();
            if (controller != null && controller.Object != null && controller.Object.IsValid)
            {
                // মাস্টার ক্লায়েন্ট কল করবে, কিন্তু আমরা নিশ্চিত করছি এটি গেমপ্লের সব ক্লায়েন্টের কাছে যায়
                controller.RPC_TriggerGameLoading();
            }
            else
            {
                Debug.LogWarning("GameplayController is not yet initialized or spawned on the Master Client!");
            }
        }
    }

    // Fusion এর সব প্রয়োজনীয় কলব্যাক (কিছুই পরিবর্তন করবেন না)
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}