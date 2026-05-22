using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public class MatchmakingManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    // Game-Selection-Panel এর PLAY বাটনে এটি লিঙ্ক করবেন
    public async void StartMatchmaking()
    {
        Debug.Log("Connecting to Fusion Server...");

        // NetworkRunner না থাকলে অবজেক্টে অ্যাড করে নেওয়া
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this); // কলব্যাকগুলো রেজিস্টার করা
        }

        // Shared Mode-এ গেম শুরু করা (PUN 2-এর মতোই সার্ভার কাজ করবে)
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "StoneHammerSawRoom", // সবাই একই রুমে জয়েন করবে
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("Successfully joined the Fusion room!");
        }
        else
        {
            Debug.LogError($"Failed to join: {result.ShutdownReason}");
        }
    }

    // ২ জন প্লেয়ার জয়েন করলে এই মেথড কল হবে
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Joined! Player ID: {player.PlayerId}");
        
        int playerCount = 0;
        foreach (var p in runner.ActivePlayers) playerCount++;

        if (playerCount == 2)
        {
            Debug.Log("Room is full! Ready to start the game...");
            // PUN 2 এর RPC এখানে কাজ করবে না, আমরা পরবর্তী ধাপে GameplayController আপডেট করব
            // FindAnyObjectByType<GameplayController>().TriggerGameLoadingRPC();
        }
    }

    // --- Fusion-এর জন্য অন্যান্য প্রয়োজনীয় (কিন্তু ফাঁকা) কলব্যাক মেথডগুলো ---
    // (এগুলো মুছে ফেললে কোডে এরর আসবে, তাই এগুলো নিচে রেখে দিন)
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