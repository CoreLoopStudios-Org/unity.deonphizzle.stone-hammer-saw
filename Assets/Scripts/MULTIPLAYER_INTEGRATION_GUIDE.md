# Multiplayer Integration Guide - Stone Hammer Saw

## 🎯 Overview
This guide walks you through integrating **Photon PUN 2** networking into the Stone Hammer Saw game. I will help you implement each component step-by-step.

---

## 📋 Step 1: Initial Photon Setup (YOU DO THIS)

### 1.1 Install Photon PUN 2 Package
1. Open your Unity project
2. Go to **Window → TextMesh Pro → Import TMP Essential Resources** (if needed)
3. Go to **Assets → Import Package → Custom Package**
4. Download and import **Photon PUN 2** from the Unity Asset Store
   - Asset Store link: Search "Photon PUN 2" by Exit Games
   - OR use Package Manager (if available in your version)

### 1.2 Create Photon App ID
1. Go to https://www.photonengine.com
2. Create a free account (Free tier supports ~20 concurrent users)
3. Create a new app with "Photon PUN" type
4. Copy your **App ID**
5. In Unity, go to **Window → Photon PUN 2 → Highlight Server Settings**
6. Paste your App ID into the SerializationSettings

### 1.3 Verify Installation
Run this quick test (I can help you create this):
- Create a simple scene
- Add a GameObject with a `PhotonNetwork.ConnectUsingSettings()` call
- Check the Console for successful connection

---

## 🏗️ Step 2: Architecture Overview (I'LL CREATE THESE FILES)

### Core Components I Will Build:

```
Scripts/Networking/
├── NetworkSync.cs              # Handles Photon connection & room management
├── NetworkEvents.cs            # Manages network events and callbacks
└── PhotonPlayerSettings.cs     # Player data structure

Scripts/Game/
├── DuelManager.cs              # Game flow state machine
├── SelectionManager.cs         # Weapon selection logic
├── TimingEngine.cs             # AFTER timing implementation
├── WeaponSystem.cs             # Weapon dominance logic
├── ResultResolver.cs           # Winner calculation
└── PlayerState.cs              # Local player state tracking

Scripts/UI/
├── UIManager.cs                # Panel management & transitions
├── PanelController.cs          # Individual panel controllers
└── ConnectionUI.cs             # Lobby & matchmaking UI

Scripts/Managers/
├── GameController.cs           # Main orchestrator
└── AudioManager.cs             # Sound effects & feedback
```

---

## 🔄 Step 3: Multiplayer Flow (HOW IT WORKS)

### Connection Flow:
```
[Game Start]
    ↓
[Connect to Photon Server] ← NetworkSync.cs
    ↓
[Create/Join Room] ← NetworkSync.cs
    ↓
[Wait for Opponent] ← DuelManager.cs
    ↓
[Ready Phase] ← GameController.cs
    ↓
[Weapon Selection (3 sec)] ← SelectionManager.cs + RPC calls
    ↓
[Send Selection to Server] ← RPC_CommitWeapon()
    ↓
[MasterClient Validates & Calculates Winner] ← ResultResolver.cs
    ↓
[Broadcast Result] ← RPC_ShowResult()
    ↓
[Display Result] ← UIManager.cs
    ↓
[Return to Lobby or Next Round]
```

### Key Photon Concepts:
1. **PhotonNetwork** - Main networking class
2. **Room** - Multiplayer session (max 2 players for this game)
3. **MasterClient** - Server-authoritative player (handles calculations)
4. **RPC** - Remote Procedure Call (function calls across network)
5. **Custom Properties** - Persistent player/room data
6. **Serialization** - Continuous data sync

---

## 📊 Step 4: Complete Implementation Roadmap

### Phase 1: Basic Networking (3-4 hours)
**I will create these files:**
1. `NetworkSync.cs` - Connection, room creation/joining
2. `PhotonPlayerSettings.cs` - Player data structure
3. `GameController.cs` - Main orchestrator

**What you need to do:**
- Install Photon PUN 2
- Set up App ID
- Create scenes: Lobby, Game, Results

---

### Phase 2: Game Logic (4-5 hours)
**I will create these files:**
1. `WeaponSystem.cs` - Weapon enum & dominance logic
2. `TimingEngine.cs` - AFTER timing grading
3. `DuelManager.cs` - State machine
4. `SelectionManager.cs` - Selection & 3-second timer

**What you need to do:**
- Create UI buttons for weapon selection
- Set up scene transitions
- Test locally first

---

### Phase 3: Result Resolution (2-3 hours)
**I will create these files:**
1. `ResultResolver.cs` - Winner calculation on MasterClient
2. `NetworkEvents.cs` - Handle RPC callbacks

**What you need to do:**
- Configure MasterClient privileges in Photon settings
- Test winner calculations

---

### Phase 4: UI Integration (3-4 hours)
**I will create these files:**
1. `UIManager.cs` - Panel transitions
2. `PanelController.cs` - Individual panel logic
3. `ConnectionUI.cs` - Lobby UI logic

**What you need to do:**
- Create UI panels in Canvas
- Link UI elements to scripts
- Test full game flow

---

## 🤖 How I Can Help You

### I CAN DO (Automated):
✅ **Write all C# scripts** following best practices
✅ **Implement Photon networking layer** entirely
✅ **Build game logic** (weapon dominance, timing, state machine)
✅ **Create networking communication** (RPCs, Custom Properties)
✅ **Debug code issues** if something breaks
✅ **Optimize performance** where needed
✅ **Add comments and documentation** to explain every function
✅ **Create test scenarios** to validate logic

### YOU NEED TO DO:
📌 **Install Photon PUN 2** in Unity (one-time setup)
📌 **Get Photon App ID** from photonengine.com (one-time setup)
📌 **Create Unity Scenes** (Lobby, Game, Results)
📌 **Create UI Canvas & Buttons** (layout & button setup)
📌 **Link UI elements** to my scripts (final integration)
📌 **Test the game** with another player on same network
📌 **Handle edge cases** specific to your game design

---

## 🚀 QUICK START - What To Do NOW

### Option A: I Create Everything First (Recommended)
1. **Tell me:** "Create all core networking files"
2. **I will:** Write `NetworkSync.cs`, `WeaponSystem.cs`, `DuelManager.cs`, etc.
3. **You do:** Install Photon, create scenes/UI
4. **Then:** We integrate and test together

### Option B: Step-by-Step (More Learning)
1. **Tell me:** "Start with NetworkSync.cs"
2. **I will:** Create and explain that one file
3. **You test** it
4. **Then:** Move to next component
5. **Continue** until game is complete

### Option C: I Build Everything, You Just Integrate (Fastest)
1. **Tell me:** "Build complete multiplayer game"
2. **I will:** Create ALL scripts (10-15 files)
3. **You do:** Install Photon + create scenes + link UI
4. **Result:** Working multiplayer game in ~2 hours

---

## 📝 Required Information From You

To start implementation, please provide:

1. **Unity Version** - What version are you using? (e.g., 2021.3, 2022.3, 2023.3)
2. **Platform** - PC only, or also mobile? (affects networking config)
3. **Scene Names** - What do you want to call your scenes?
   - Example: "Lobby", "DuelArena", "ResultsScreen"
4. **UI Panel Names** - What are your exact panel names in Canvas?
   - Example: "LoadingPanel", "WeaponSelectPanel", etc.
5. **Preferred Approach** - Option A, B, or C above?

---

## 🔗 Photon PUN 2 Key Methods I'll Use

```csharp
// Connection
PhotonNetwork.ConnectUsingSettings();
PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, null);
PhotonNetwork.LeaveRoom();

// RPCs (Remote Procedure Calls)
photonView.RPC("RPC_CommitWeapon", RpcTarget.MasterClient, weaponType, timing);

// Custom Properties (Persistent Data)
player.SetCustomProperties(new Hashtable { { "Weapon", weapon } });

// Master Client Check
if (PhotonNetwork.IsMasterClient) { /* Calculate winner */ }

// Network Events
public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) { }
```

---

## ✨ What You'll Get

After full implementation:
- ✅ Working multiplayer game
- ✅ Server-authoritative validation (no cheating)
- ✅ Real-time weapon selection synchronization
- ✅ Accurate timing engine (AFTER timing)
- ✅ Winner calculation with weapon dominance
- ✅ Smooth UI transitions
- ✅ Full source code (well-commented)
- ✅ Ready to deploy

---

## 📞 Next Steps

**Reply with:**
1. Your Unity version
2. Scene and panel names
3. Which approach (A, B, or C)
4. Or just say: **"Build it all now!"** and I'll start immediately

I'm ready to build your multiplayer game! 🎮

