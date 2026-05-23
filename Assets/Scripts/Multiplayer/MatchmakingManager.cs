using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;

public class MatchmakingManager : MonoBehaviour, INetworkRunnerCallbacks
{
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

        runner.AddCallbacks(this);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = null,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
            Debug.Log("Successfully joined the Fusion room!");
        else
            Debug.LogError($"Failed to join: {result.ShutdownReason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Joined! Total Players: {runner.ActivePlayers.Count()}");

        if (runner.ActivePlayers.Count() == 2)
        {
            Debug.Log("2 Players Connected! Starting Game Locally...");
            
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            GameplayController gameController = FindAnyObjectByType<GameplayController>();

            if (uiManager != null && gameController != null)
            {
                uiManager.StartGameSpecificLoading(() => 
                {
                    // Fusion পুরোপুরি রেডি হওয়ার জন্য সামান্য ডিলে দেওয়া হলো
                    Invoke(nameof(CallStartGameRound), 0.5f);
                });
            }
            else
            {
                // If UIManager or GameplayController is null, it means we are likely on the client where scene objects haven't finished spawning.
                // This is expected and will be handled by GameplayController's Spawned() callback.
                Debug.Log("[MatchmakingManager] UIManager or GameplayController is not ready in the scene yet. Waiting for Spawned()...");
            }
        }
    }

    private void CallStartGameRound()
    {
        GameplayController gameController = FindAnyObjectByType<GameplayController>();
        if(gameController != null) gameController.StartGameRound();
    }

    // Fusion এর অন্যান্য কলব্যাকগুলো
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