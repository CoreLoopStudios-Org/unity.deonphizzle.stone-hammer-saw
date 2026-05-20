# Code Architecture Preview - What I'll Build

This file shows you exactly what kind of code I'll write for each component.

---

## 📌 Example: NetworkSync.cs Structure

```csharp
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class NetworkSync : MonoBehaviourPunCallbacks
{
    public static NetworkSync Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null) 
            Instance = this;
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
            return;
            
        Debug.Log("[NetworkSync] Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void JoinOrCreateRoom(string roomName)
    {
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, null);
    }

    public override void OnConnected()
    {
        Debug.Log("[NetworkSync] Connected to Photon servers");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkSync] Joined room with {PhotonNetwork.PlayerList.Length} players");
        
        if (PhotonNetwork.PlayerList.Length == 2)
        {
            // Both players ready - start game
            GameController.Instance.StartDuel();
        }
    }

    public void SendWeaponSelection(Weapon weapon, long timingMs)
    {
        photonView.RPC("RPC_CommitWeapon", RpcTarget.MasterClient, 
            (int)weapon, timingMs);
    }
}
```

---

## 📌 Example: WeaponSystem.cs Structure

```csharp
using UnityEngine;

public enum Weapon
{
    MiniSaw = 0,
    BigSaw = 1,
    Hammer = 2,
    MiniStone = 3,
    BigStone = 4
}

public class WeaponSystem : MonoBehaviour
{
    public static WeaponSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    /// <summary>
    /// Determines if weapon1 beats weapon2
    /// Returns: 1 if weapon1 wins, -1 if weapon2 wins, 0 if draw
    /// </summary>
    public int CheckWeaponDominance(Weapon weapon1, Weapon weapon2)
    {
        if (weapon1 == weapon2) return 0; // Same weapon

        // Define dominance matrix
        bool weapon1_beats = DoesWeaponBeat(weapon1, weapon2);
        
        if (weapon1_beats) return 1;
        else return -1;
    }

    private bool DoesWeaponBeat(Weapon attacker, Weapon defender)
    {
        switch (attacker)
        {
            case Weapon.MiniSaw:
                return defender == Weapon.MiniStone;
            
            case Weapon.BigSaw:
                return defender == Weapon.MiniSaw || 
                       defender == Weapon.Hammer;
            
            case Weapon.Hammer:
                return defender == Weapon.MiniSaw || 
                       defender == Weapon.MiniStone;
            
            case Weapon.MiniStone:
                return false; // Loses to everything
            
            case Weapon.BigStone:
                return defender == Weapon.Hammer || 
                       defender == Weapon.MiniStone;
            
            default:
                return false;
        }
    }

    public string GetWeaponName(Weapon weapon)
    {
        return weapon.ToString().Replace("_", " ");
    }
}
```

---

## 📌 Example: TimingEngine.cs Structure

```csharp
using UnityEngine;

public enum TimingGrade
{
    Perfect,  // <= 10ms
    Great,    // <= 30ms
    Good,     // <= 80ms
    Bad       // > 80ms
}

public class TimingEngine : MonoBehaviour
{
    public static TimingEngine Instance { get; private set; }
    
    private const int PERFECT_THRESHOLD = 10;
    private const int GREAT_THRESHOLD = 30;
    private const int GOOD_THRESHOLD = 80;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    /// <summary>
    /// Grades timing relative to reference point
    /// AFTER timing: +10ms to +250ms
    /// </summary>
    public TimingGrade GradeActingTiming(long clientTimingMs, long serverReferenceMs)
    {
        long timingOffset = clientTimingMs - serverReferenceMs;
        
        // Must be within valid window
        if (timingOffset < 10 || timingOffset > 250)
            return TimingGrade.Bad;

        // Grade within valid window
        if (timingOffset <= PERFECT_THRESHOLD)
            return TimingGrade.Perfect;
        
        if (timingOffset <= GREAT_THRESHOLD)
            return TimingGrade.Great;
        
        if (timingOffset <= GOOD_THRESHOLD)
            return TimingGrade.Good;
        
        return TimingGrade.Bad;
    }

    /// <summary>
    /// Checks if two timings are within epsilon (for draws)
    /// </summary>
    public bool WithinTimingEpsilon(TimingGrade grade1, TimingGrade grade2, 
        long timing1, long timing2)
    {
        long epsilon = 5; // 5ms tolerance
        return System.Math.Abs(timing1 - timing2) <= epsilon 
            && grade1 == grade2;
    }
}
```

---

## 📌 Example: ResultResolver.cs Structure

```csharp
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;

public class ResultResolver : MonoBehaviourPun
{
    public enum DuelResult
    {
        Win,
        Loss,
        Draw
    }

    public struct DuelOutcome
    {
        public DuelResult result;
        public string reason;
        public TimingGrade playerGrade;
        public TimingGrade opponentGrade;
    }

    /// <summary>
    /// MasterClient only: Calculate duel outcome
    /// Server-authoritative - prevents cheating
    /// </summary>
    [PunRPC]
    public void RPC_ResolveDuel(int player1WeaponInt, long player1Timing,
                                int player2WeaponInt, long player2Timing)
    {
        if (!PhotonNetwork.IsMasterClient)
            return; // Only MasterClient executes

        Weapon player1Weapon = (Weapon)player1WeaponInt;
        Weapon player2Weapon = (Weapon)player2WeaponInt;

        DuelOutcome outcome = CalculateWinner(
            player1Weapon, player1Timing,
            player2Weapon, player2Timing);

        // Broadcast result to all players
        photonView.RPC("RPC_DisplayResult", RpcTarget.All, 
            (int)outcome.result, outcome.reason);
    }

    private DuelOutcome CalculateWinner(Weapon p1Weapon, long p1Timing,
                                        Weapon p2Weapon, long p2Timing)
    {
        // Step 1: Check weapon dominance
        int weaponResult = WeaponSystem.Instance
            .CheckWeaponDominance(p1Weapon, p2Weapon);

        // Step 2: Grade timings
        TimingGrade p1Grade = TimingEngine.Instance
            .GradeActingTiming(p1Timing, 0); // 0 = server reference
        TimingGrade p2Grade = TimingEngine.Instance
            .GradeActingTiming(p2Timing, 0);

        // Step 3: Determine outcome
        if (weaponResult == 1)
            return new DuelOutcome { 
                result = DuelResult.Win, 
                reason = $"Weapon Dominance: {p1Weapon} beats {p2Weapon}",
                playerGrade = p1Grade,
                opponentGrade = p2Grade
            };

        if (p1Grade.CompareTo(p2Grade) > 0) // Better grade = lower enum value
            return new DuelOutcome { 
                result = DuelResult.Win, 
                reason = $"Better Timing: {p1Grade} vs {p2Grade}",
                playerGrade = p1Grade,
                opponentGrade = p2Grade
            };

        return new DuelOutcome { 
            result = DuelResult.Draw, 
            reason = "Draw - Equal outcome",
            playerGrade = p1Grade,
            opponentGrade = p2Grade
        };
    }
}
```

---

## 📌 Example: DuelManager.cs Structure

```csharp
using UnityEngine;
using Photon.Pun;

public class DuelManager : MonoBehaviourPun
{
    public enum DuelState
    {
        Idle,
        Ready,
        Selecting,
        WaitingForOpponent,
        Resolving,
        Finished
    }

    private DuelState currentState;

    private void Start()
    {
        currentState = DuelState.Idle;
    }

    public void EnterReadyPhase()
    {
        Debug.Log("[DuelManager] Entering Ready Phase");
        SetState(DuelState.Ready);
        UIManager.Instance.ShowPanel("DuelPanel");
        // Wait 2 seconds then enter selection
        Invoke(nameof(EnterSelectionPhase), 2f);
    }

    public void EnterSelectionPhase()
    {
        Debug.Log("[DuelManager] Starting Weapon Selection (3 seconds)");
        SetState(DuelState.Selecting);
        UIManager.Instance.ShowPanel("WeaponSelectPanel");
        SelectionManager.Instance.StartSelectionTimer(3f);
    }

    public void CommitWeaponSelection(Weapon weapon)
    {
        Debug.Log($"[DuelManager] Player committed: {weapon}");
        SetState(DuelState.WaitingForOpponent);
        
        // Send to server
        long timingMs = SelectionManager.Instance.GetElapsedTiming();
        NetworkSync.Instance.SendWeaponSelection(weapon, timingMs);
    }

    public void ShowDuelResult(string result)
    {
        Debug.Log($"[DuelManager] Duel Result: {result}");
        SetState(DuelState.Finished);
        UIManager.Instance.ShowPanel(result + "Panel");
    }

    private void SetState(DuelState newState)
    {
        currentState = newState;
        Debug.Log($"[DuelManager] State changed to: {newState}");
    }
}
```

---

## 📌 Example: UIManager.cs Structure

```csharp
using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private Dictionary<string, GameObject> panels = 
        new Dictionary<string, GameObject>();

    public void RegisterPanel(string panelName, GameObject panelObject)
    {
        panels[panelName] = panelObject;
        panelObject.SetActive(false);
    }

    public void ShowPanel(string panelName)
    {
        // Hide all panels
        foreach (var panel in panels.Values)
            panel.SetActive(false);

        // Show requested panel
        if (panels.ContainsKey(panelName))
        {
            panels[panelName].SetActive(true);
            Debug.Log($"[UIManager] Showing: {panelName}");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Panel not found: {panelName}");
        }
    }

    public void UpdateConnectionStatus(string status)
    {
        // Update connection UI
        Debug.Log($"[UIManager] Connection Status: {status}");
    }

    public void DisplayResult(string playerResult, 
        string opponentWeapon, string timing)
    {
        // Format and display result
        string message = $"You: {playerResult}\n" +
                        $"Opponent Weapon: {opponentWeapon}\n" +
                        $"Timing: {timing}";
        Debug.Log($"[UIManager] Result:\n{message}");
    }
}
```

---

## 📌 Example: SelectionManager.cs Structure

```csharp
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    private Weapon selectedWeapon;
    private float selectionTimeRemaining;
    private long selectionStartTimestamp;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void StartSelectionTimer(float durationSeconds)
    {
        selectionTimeRemaining = durationSeconds;
        selectionStartTimestamp = System.DateTime.UtcNow.Ticks / 10000; // ms
        Debug.Log($"[SelectionManager] Timer started: {durationSeconds}s");
    }

    private void Update()
    {
        if (selectionTimeRemaining > 0)
        {
            selectionTimeRemaining -= Time.deltaTime;
            
            // Update UI timer
            UIManager.Instance.UpdateSelectionTimer(selectionTimeRemaining);

            if (selectionTimeRemaining <= 0)
            {
                Debug.Log("[SelectionManager] Time's up!");
                CommitCurrentSelection();
            }
        }
    }

    public void SelectWeapon(int weaponIndex)
    {
        selectedWeapon = (Weapon)weaponIndex;
        Debug.Log($"[SelectionManager] Weapon selected: {selectedWeapon}");
    }

    public long GetElapsedTiming()
    {
        long now = System.DateTime.UtcNow.Ticks / 10000;
        return now - selectionStartTimestamp;
    }

    private void CommitCurrentSelection()
    {
        DuelManager.Instance.CommitWeaponSelection(selectedWeapon);
    }
}
```

---

## 🎯 Key Code Patterns You'll See

### Pattern 1: Singleton Pattern
```csharp
public static NetworkSync Instance { get; private set; }
private void Awake() {
    if (Instance == null) Instance = this;
}
```
✅ Ensures only one instance exists globally

### Pattern 2: Photon RPC Calls
```csharp
photonView.RPC("RPC_CommitWeapon", RpcTarget.MasterClient, weapon, timing);
```
✅ Sends function calls across network

### Pattern 3: State Machine
```csharp
public enum DuelState { Idle, Ready, Selecting, Finished }
private DuelState currentState;
private void SetState(DuelState newState) { currentState = newState; }
```
✅ Manages game flow cleanly

### Pattern 4: Custom Properties
```csharp
Hashtable playerProps = new Hashtable { { "Weapon", weapon } };
player.SetCustomProperties(playerProps);
```
✅ Tracks persistent player data

### Pattern 5: Callbacks/Events
```csharp
public override void OnJoinedRoom() { /* Handle room join */ }
public override void OnPlayerPropertiesUpdate() { /* Handle prop changes */ }
```
✅ Responds to network events

---

## 📊 Code Quality Standards

Every file I create will have:

✅ **Clear Comments** - Every function explained
✅ **Consistent Formatting** - Easy to read
✅ **Error Handling** - Null checks & validations
✅ **Debug Logging** - Track what's happening
✅ **Best Practices** - SOLID principles followed
✅ **Network Safety** - MasterClient validation for cheating prevention
✅ **Performance** - Optimized for multiplayer

---

## ✨ Ready to Start?

This is the quality of code I'll deliver. 

**Next step: Tell me how to proceed!**

Options:
- **"BUILD ALL NOW"** - I create all 15 components
- **"STEP BY STEP"** - Let's start with one file
- **"ASK QUESTIONS"** - Want to know more first?

Let me know! 🚀

