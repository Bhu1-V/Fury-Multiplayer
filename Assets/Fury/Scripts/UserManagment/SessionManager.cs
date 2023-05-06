using Fury.Characters.Vitals;
using FishNet;
using FishNet.Broadcast;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionManager : NetworkBehaviour {
    public static SessionManager Instance;

    [SyncVar] public int redCount = 0;
    [SyncVar] public int blueCount = 0;

    [SyncObject] private readonly SyncDictionary<string, UserData> redPlayers = new();
    [SyncObject] private readonly SyncDictionary<string, UserData> bluePlayers = new();

    public List<string> redHash = new();
    public List<string> blueHash = new();

    [SyncVar] public uint sessionID;

    [SyncVar(OnChange = nameof(OnRedTeamScoreChanged))] public int redTeamScore;
    [SyncVar(OnChange = nameof(OnBlueTeamScoreChanged))] public int blueTeamScore;

    [SyncObject]
    private readonly SyncTimer _timeRemaining = new SyncTimer();

    [SerializeField]
    private int sessionMinutes;

    [SerializeField]
    private int warmUpTimeSeconds;

    [SerializeField]
    TextMeshProUGUI sessionTimerText;

    [SerializeField]
    TextMeshProUGUI teamRedPlayersText;

    [SerializeField]
    TextMeshProUGUI teamBluePlayersText;

    [SerializeField]
    Transform gameLogParent;

    [SerializeField]
    GameObject gameLogEntryGameObject;

    [SerializeField]
    SessionInformationCanvas sessionInformationCanvas;

    [SerializeField]
    TextMeshProUGUI BlueTeamScoreText;

    [SerializeField]
    TextMeshProUGUI RedTeamScoreText;

    private void Awake() {
        if(Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
        if(sessionTimerText == null) sessionTimerText = GameObject.FindGameObjectWithTag("SessionTimer")?.GetComponent<TextMeshProUGUI>();

        Health.GlobalOnHealthChanged += OnHealthChanged;
        UserData.OnScoreUpdated += OnUserScoreUpdated;
        Flag.OnFlagCaptured += OnFlagCaptured;
        Flag.OnFlagSuccessfullCaptured += OnFlagSuccessfullCaptured;

        redPlayers.OnChange += _Players_onChange;
        bluePlayers.OnChange += _Players_onChange;
    }

    private void OnDestroy() {
        Health.GlobalOnHealthChanged -= OnHealthChanged;
        UserData.OnScoreUpdated -= OnUserScoreUpdated;
        Flag.OnFlagCaptured -= OnFlagCaptured;

        redPlayers.OnChange -= _Players_onChange;
        bluePlayers.OnChange -= _Players_onChange;
    }

    public override void OnStartServer() {
        base.OnStartServer();
        Debug.Log($"Session Manager Start Server");
        sessionID = (uint)Time.time.GetHashCode();
        redPlayers.Clear();
        bluePlayers.Clear();
        _timeRemaining.StartTimer((sessionMinutes * 60f) + warmUpTimeSeconds);
    }

    public override void OnStopServer() {
        base.OnStopServer();
        Debug.Log($"Session Manager Stopped Server");
        redPlayers.Clear();
        bluePlayers.Clear();
        _timeRemaining.StopTimer();
    }

    public override void OnStopClient() {
        base.OnStopClient();
        Debug.Log($"Session Manager Stopped Client");
        if(!IsOwner && IsServer) return;
        sessionInformationCanvas.RemoveSessionLog();
    }

    private void Update() {
        if(!_timeRemaining.Paused) {
            _timeRemaining.Update(Time.deltaTime);
        }
    }

    private void LateUpdate() {
        if(!_timeRemaining.Paused) {
            sessionTimerText.text = TimeSpan.FromSeconds(_timeRemaining.Remaining).ToString(@"mm\:ss");

            teamRedPlayersText.text = GetDictonaryData(redPlayers).Trim(' ');
            teamBluePlayersText.text = GetDictonaryData(bluePlayers).Trim(' ');
        }
    }

    [Server]
    public void PredictNextSpawnTeam(out UserData.Team nextSpawnTeam) {
        nextSpawnTeam = UserData.Team.None;
        if(redCount == 0 && blueCount == 0) {
            nextSpawnTeam = UserData.Team.Red;
        } else {
            if(redCount > blueCount) {
                nextSpawnTeam = UserData.Team.Blue;
            } else {
                nextSpawnTeam = UserData.Team.Red;
            }
        }
    }

    [Server]
    public void UpdatePlayerData(UserData data) {
        Debug.Log($"updating User Data of {data}");
        if(data.team == UserData.Team.Red) {
            redPlayers[data.userName] = data;
            redPlayers.Dirty(data.userName);
        } else {
            bluePlayers[data.userName] = data;
            bluePlayers.Dirty(data.userName);
        }
    }

    [ServerRpc]
    public void UpdatePlayerTeam(string userName, UserData.Team prev, UserData.Team current) {
        Debug.Log($"updating Player Team of {userName} from {prev} to {current}");
        if(prev == UserData.Team.Red) {
            redPlayers.Remove(userName);
            redPlayers.Dirty(userName);
        } else {
            bluePlayers.Remove(userName);
            bluePlayers.Dirty(userName);
        }

        if(current == UserData.Team.Red) {
            redPlayers[userName].team = current;
            redPlayers.Dirty(userName);
        } else {
            bluePlayers[userName].team = current;
            bluePlayers.Dirty(userName);
        }
    }

    [Server]
    public void AddPlayerToTeam(NetworkObject netObj) {
        if(!IsServer) return;
        Debug.Log($"Adding player to Team");

        UserData userData = netObj.GetComponent<UserData>();
        if(redCount == 0 && blueCount == 0) {
            // Assign to Team Red
            redCount++;
            Debug.Log($"Calling AssignTeam red At Start for {userData.userName} and Team = {userData.team}");

            userData.AssignTeam(UserData.Team.Red);

            redPlayers[userData.userName] = userData;
            redPlayers.Dirty(userData.userName);
            redHash.Clear();

            foreach(string userData1 in redPlayers.Keys) {
                redHash.Add(userData1);
            }

            redCount = redPlayers.Count;
            blueCount = bluePlayers.Count;
            return;
        }

        if(redCount > blueCount) {
            // Assign to Blue
            Debug.Log($"Calling AssignTeam Blue for {userData.userName} and Team = {userData.team}");
            userData.AssignTeam(UserData.Team.Blue);

            bluePlayers[userData.userName] = userData;
            bluePlayers.Dirty(userData.userName);

            blueHash.Clear();
            foreach(string userData1 in bluePlayers.Keys) {
                blueHash.Add(userData1);
            }
        } else {
            // Assign to Red
            Debug.Log($"Calling AssignTeam red for {userData.userName} and Team = {userData.team}");

            userData.AssignTeam(UserData.Team.Red);

            redPlayers[userData.userName] = userData;
            redPlayers.Dirty(userData.userName);

            redHash.Clear();
            foreach(string userData1 in redPlayers.Keys) {
                redHash.Add(userData1);
            }
        }

        redCount = redPlayers.Count;
        blueCount = bluePlayers.Count;
    }

    [Server]
    public void RemoveFromTeam(NetworkObject netObj) {
        if(!IsServer) return;

        UserData userData = netObj.GetComponent<UserData>();
        if(UserData.Team.Red == userData.team) {

            redPlayers.Remove(userData.userName);
            redPlayers.Dirty(userData.userName);

            Debug.Log($"Remove {userData.userName} from red players = {redPlayers.Count}");
            redHash.Clear();
            foreach(string ud in redPlayers.Keys) {
                redHash.Add(ud);
            }
        } else {
            bluePlayers.Remove(userData.userName);
            bluePlayers.Dirty(userData.userName);
            Debug.Log($"Remove {userData.userName} from blue players = {bluePlayers.Count}");
            blueHash.Clear();
            foreach(string ud in bluePlayers.Keys) {
                blueHash.Add(ud);
            }
        }
        redCount = redPlayers.Count;
        blueCount = bluePlayers.Count;
    }

    string GetDictonaryData(SyncDictionary<string, UserData> data) {
        string output = "";
        foreach(UserData element in data.Values) {
            output += element.GetData() + "\n";
        }
        return output;// Debug.Log(output);
    }

    private void OnEnable() {
        Debug.Log($"Session Manager is Enabled");
        InstanceFinder.ClientManager.RegisterBroadcast<GameLogEntry>(OnGameLogEntry);
    }

    private void OnDisable() {
        Debug.Log($"Session Manager is Disable");
        sessionInformationCanvas.RemoveSessionLog();
        InstanceFinder.ClientManager.UnregisterBroadcast<GameLogEntry>(OnGameLogEntry);
    }

    private Queue<GameObject> objectQueue;
    public int maxQueueSize = 5;

    private void Start() {
        objectQueue = new Queue<GameObject>(maxQueueSize);
    }

    public void OnGameLogEntry(GameLogEntry entry) {
        GameObject gb = Instantiate(gameLogEntryGameObject, gameLogParent);
        gb.GetComponent<Image>().color = entry.logColor == "Red" ? Color.red : Color.grey;
        AddToQueue(gb);
        gb.transform.SetAsFirstSibling();
        TextMeshProUGUI gameLogEntryText = gb.GetComponentInChildren<TextMeshProUGUI>();
        gameLogEntryText.text = $"{entry.logEntry}\n";
    }

    public void AddToQueue(GameObject objToAdd) {
        if(objectQueue.Count >= maxQueueSize) {
            Destroy(objectQueue.Dequeue());
        }
        objectQueue.Enqueue(objToAdd);
    }

    public struct GameLogEntry : IBroadcast {
        public string logEntry;
        public string logColor;
    }

    public void OnHealthChanged(Health health, int oldHealth, int newHealth, int max) {
        if(!IsServer) return;
        UserData userData = health.GetComponent<UserData>();
        string brodcastData = $"{userData.userName} got Damaged {newHealth - oldHealth}";
        ServerManager.Broadcast(new GameLogEntry { logEntry = brodcastData, logColor = "Red" });
    }

    public void OnUserScoreUpdated(string ID, int scoreAdd, uint score) {
        if(!IsServer) return;
        string broadcastData = $"User:{ID} scored +{scoreAdd} XP, Total XP: {score}";
        ServerManager.Broadcast(new GameLogEntry { logEntry = broadcastData, logColor = "Grey" });
    }

    UserData _capturedUserData;
    UserData.Team _capturedFlagTeam;

    public void OnFlagCaptured(UserData userData) {
        if(!IsServer) return;
        _capturedUserData = userData;
        _capturedFlagTeam = (userData.team == UserData.Team.Blue) ? UserData.Team.Red : UserData.Team.Blue;
        _capturedUserData.GetComponent<Health>().OnDeath += OnFlagedPlayerDeath;

        string broadcastData = $"User:{userData.userName} Captured {((userData.team == UserData.Team.Blue) ? UserData.Team.Red : UserData.Team.Blue)}'s Team";
        ServerManager.Broadcast(new GameLogEntry { logEntry = broadcastData, logColor = "Grey" });
    }

    public void OnFlagedPlayerDeath() {
        if(!IsServer) return;
        if(!_capturedUserData.IsCarringEnemyFlag) return;

        Debug.Log($"KKKKKKK: Brodcasting FlagDrop = {_capturedFlagTeam}");
        _capturedUserData.GetComponent<Health>().OnDeath -= OnFlagedPlayerDeath;
        _capturedUserData.IsCarringEnemyFlag = false;
        if(_capturedFlagTeam != UserData.Team.None) {
            ServerManager.Broadcast(new Flag.FlagDropData { droppedFlagTeam = _capturedFlagTeam });
        }

        string broadcastData = $"User:{_capturedUserData.userName} Dropped {((_capturedUserData.team == UserData.Team.Blue) ? UserData.Team.Red : UserData.Team.Blue)}'s Flag";
        ServerManager.Broadcast(new GameLogEntry { logEntry = broadcastData, logColor = "Red" });

        _capturedUserData = null;
        _capturedFlagTeam = UserData.Team.None;
    }

    public void OnRedTeamScoreChanged(int prev, int current, bool asServer) {
        Debug.Log($"Red Changed from {prev} to {current} and asServer {asServer} and isOwner = {IsOwner}");
        //if(!IsOwner) return;
        RedTeamScoreText.text = current.ToString();
    }

    public void OnBlueTeamScoreChanged(int prev, int current, bool asServer) {
        Debug.Log($"blue Changed from {prev} to {current} and asServer {asServer} and isOwner = {IsOwner}");
        //if(!IsOwner) return;
        BlueTeamScoreText.text = current.ToString();
    }
    public void OnFlagSuccessfullCaptured(UserData userData) {
        if(!IsServer) return;
        Debug.Log($"On Flag Successfull Capture and {userData.userName} captured {((userData.team == UserData.Team.Blue) ? UserData.Team.Red : UserData.Team.Blue)} and red = {redTeamScore} and blue = {blueTeamScore}");

        if(userData.team == UserData.Team.Red) {
            redTeamScore++;
        } else {
            blueTeamScore++;
        }

        string broadcastData = $"User:{userData.userName} Captured Successfully {((userData.team == UserData.Team.Blue) ? UserData.Team.Red : UserData.Team.Blue)}'s Team";
        ServerManager.Broadcast(new GameLogEntry { logEntry = broadcastData, logColor = "Orange" });
        Debug.Log($"On Flag Successfull Completed Updated Score, red ={redTeamScore} and blue = {blueTeamScore}");
    }

    private void _Players_onChange(SyncDictionaryOperation op, string key, UserData value, bool asServer) {
        Debug.Log($"Sync Dict. Changed OP = {op}, {key}, score = {value?.score} team = {value?.team}, {asServer}");
        switch(op) {
            case SyncDictionaryOperation.Add:
                if(value.team == UserData.Team.Red) {
                    sessionInformationCanvas.AddRedPlayer(value.userName, value.score);
                } else {
                    sessionInformationCanvas.AddBluePlayer(value.userName, value.score);
                }
                break;
            case SyncDictionaryOperation.Remove:
                sessionInformationCanvas.RemovePlayerLog(key);
                break;
            case SyncDictionaryOperation.Set:
                if(value.team == UserData.Team.Red) {
                    sessionInformationCanvas.AddRedPlayer(value.userName, value.score);
                } else {
                    sessionInformationCanvas.AddBluePlayer(value.userName, value.score);
                }
                break;
            /* All objects have been cleared. Index, oldValue,
            * and newValue are default. */
            case SyncDictionaryOperation.Clear:
                sessionInformationCanvas.ClearPlayers();
                break;
            /* When complete calls all changes have been
            * made to the collection. You may use this
            * to refresh information in relation to
            * the list changes, rather than doing so
            * after every entry change. Like Clear
            * Index, oldItem, and newItem are all default. */
            case SyncDictionaryOperation.Complete:
                sessionInformationCanvas.UpdateInfo(value);
                break;
        }
    }
}
