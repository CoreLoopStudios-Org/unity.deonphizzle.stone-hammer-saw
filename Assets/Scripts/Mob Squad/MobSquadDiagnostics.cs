using UnityEngine;
using Fusion;
using System.Linq; // এটি মাস্ট, কারণ এটি ছাড়া .Count() কাজ করবে না

public class MobSquadDiagnostics : MonoBehaviour
{
    private MobSquadGameManager manager;

    private void Start()
    {
        manager = GetComponent<MobSquadGameManager>();
        
        // Reference Check
        if (manager.playerPrefab == null) Debug.LogWarning("[Diagnostics] Player Prefab missing!");
        if (manager.spawnLineParent == null) Debug.LogWarning("[Diagnostics] Spawn Line Parent missing!");
        if (manager.tapToPlayPanel == null) Debug.LogWarning("[Diagnostics] Tap To Play Panel missing!");
    }

    private void OnGUI()
    {
        if (manager == null) return;
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        try
        {
            GUILayout.Label("--- MobSquad Debug ---");
        
            string gameActiveText = "Waiting for Spawned()...";
            if (manager.Object != null && manager.Object.IsValid)
            {
                // Use safe accessor to avoid InvalidOperationException before Spawned
                gameActiveText = manager.IsGameActiveSafe.ToString();
            }
            GUILayout.Label("Is Game Active: " + gameActiveText);
        
            var runner = FindObjectOfType<NetworkRunner>();
            string playerCount = (runner != null) ? runner.ActivePlayers.Count().ToString() : "No Runner";
            GUILayout.Label("Players Connected: " + playerCount);
        }
        finally
        {
            GUILayout.EndArea();
        }
    }
}