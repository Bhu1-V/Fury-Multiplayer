using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

public class Flag : NetworkBehaviour {
    [SerializeField]
    private UserData.Team flagTeam;
    public UserData.Team FlagTeam { get { return flagTeam; } private set { flagTeam = value; } }

    [SyncVar]
    public bool isCaptured;

    public static event Action<UserData> OnFlagCaptured;
    public static event Action<UserData> OnFlagSuccessfullCaptured;

    public const int FLAG_CAPTURE_ADD_SCORE = 1000;

    public string FLAG_CAPTURE_SUCCESS = "flag_Capture_Success";
    public string FLAG_CAPTURE_START = "flag_Capture_Start";

    public MinimapVisibleHandler miniMapVisiblehandler;

    private void OnEnable() {
        InstanceFinder.ClientManager.RegisterBroadcast<FlagCaptureData>(OnFlagCaptureDataReceived);
        InstanceFinder.ClientManager.RegisterBroadcast<FlagDropData>(OnFlagDropDataReceived);
        InstanceFinder.ServerManager.RegisterBroadcast<FlagDropData>(OnClientDropDataReceive);
    }

    private void Start() {
        Debug.Log($"Updating Flags Map Legend {flagTeam}");
        if(miniMapVisiblehandler == null) miniMapVisiblehandler = GetComponent<MinimapVisibleHandler>();
        miniMapVisiblehandler.UpdateLegend(flagTeam);
    }

    private void OnClientDropDataReceive(NetworkConnection conn, FlagDropData flagDropData) {
        InstanceFinder.ServerManager.Broadcast(flagDropData);
    }

    public override void OnStartClient() {
        base.OnStartClient();
        Activate(!isCaptured);
    }

    private void OnDisable() {
        InstanceFinder.ClientManager?.UnregisterBroadcast<FlagCaptureData>(OnFlagCaptureDataReceived);
        InstanceFinder.ClientManager?.UnregisterBroadcast<FlagDropData>(OnFlagDropDataReceived);
        InstanceFinder.ServerManager?.UnregisterBroadcast<FlagDropData>(OnClientDropDataReceive);
    }

    public struct FlagDropData : IBroadcast {
        public UserData.Team droppedFlagTeam;
    }

    public struct FlagCaptureData : IBroadcast {
        public string flagCaptureDataType;
        public readonly UserData collidedUserData;
        public UserData.Team collidedFlagTeam;

        public FlagCaptureData(string _flagCaptureDataType, UserData _user, UserData.Team _collidedFlagTeam) {
            flagCaptureDataType = _flagCaptureDataType;
            collidedUserData = _user;
            collidedFlagTeam = _collidedFlagTeam;
        }
    }

    public void OnFlagDropDataReceived(FlagDropData flagDropData) {
        Debug.Log($"Received Brodcast = by {flagTeam} of {flagDropData.droppedFlagTeam}'s team flag Dropped");
        if(flagTeam == flagDropData.droppedFlagTeam) {
            FlagDrop();
        }
    }

    public void OnFlagCaptureDataReceived(FlagCaptureData flagCaptureData) {
        Debug.Log($"Received Brodcast = {flagCaptureData.flagCaptureDataType} and {flagCaptureData.collidedUserData.userName} collided with {flagCaptureData.collidedFlagTeam} and this Flag {this.flagTeam}");

        // Success Flag Capture
        if(flagCaptureData.flagCaptureDataType == FLAG_CAPTURE_SUCCESS) {
            flagCaptureData.collidedUserData.IsCarringEnemyFlag = false;
            FlagDropData flagdropData = new FlagDropData {
                droppedFlagTeam = ((UserData.Team.Red == flagCaptureData.collidedFlagTeam) ? UserData.Team.Blue : UserData.Team.Red)
            };
            if(IsServer) {
                ServerManager.Broadcast(flagdropData);
            } else if(IsClient) {
                ClientManager.Broadcast(flagdropData);
            }
            return;
        }

        if(flagCaptureData.flagCaptureDataType == FLAG_CAPTURE_START) {
            flagCaptureData.collidedUserData.IsCarringEnemyFlag = true;
            if(this.flagTeam == flagCaptureData.collidedFlagTeam) {
                FlagCapture();
            }
            return;
        }
    }

    private void OnTriggerEnter(Collider other) {
        //Debug.Log($"Collision Triggered = {System.Convert.ToString(other.gameObject.layer, 2)}");
        if(IsServer) {
            UserData userData = other.GetComponent<UserData>();
            if(userData) {
                string flagCaptureType = "";
                if(userData && userData.IsCarringEnemyFlag && userData.team == flagTeam && !isCaptured) {
                    flagCaptureType = FLAG_CAPTURE_SUCCESS;

                    userData.UpdateScore(FLAG_CAPTURE_ADD_SCORE);
                    OnFlagSuccessfullCaptured?.Invoke(userData);
                }

                if(userData && !userData.IsCarringEnemyFlag && userData.team != flagTeam && !isCaptured) {
                    flagCaptureType = FLAG_CAPTURE_START;

                    OnFlagCaptured?.Invoke(userData);
                }

                if(!string.IsNullOrEmpty(flagCaptureType)) {

                    Debug.Log($"Brodcating {flagCaptureType} and userName = {userData.userName} and collided Team = {flagTeam}");
                    InstanceFinder.ServerManager.Broadcast(new FlagCaptureData(flagCaptureType, userData, flagTeam));
                }
            }
        }
    }

    //[Server]
    //public void CmdCheckSuccessFlagCapture(UserData userData) {
    //    if(userData && userData.isCarringEnemyFlag && userData.team == flagTeam && !isCaptured) {

    //        userData.isCarringEnemyFlag = false;
    //        userData.enemyFlag.FlagDrop();

    //        Debug.Log($"Server RPC => {userData.userName} Succesfully Captured {flagTeam}'s Flag");
    //        userData.UpdateScore(FLAG_CAPTURE_ADD_SCORE);
    //        OnFlagSuccessfullCaptured?.Invoke(userData, userData.enemyFlag);
    //    }
    //    UpdateSucessToObservers(userData);
    //}

    //[ObserversRpc]
    //public void UpdateSucessToObservers(UserData userData) {
    //    if(userData && userData.isCarringEnemyFlag && userData.team == flagTeam && !isCaptured) {

    //        userData.isCarringEnemyFlag = false;
    //        userData.enemyFlag.FlagDrop();

    //        Debug.Log($"Observer RPC => {userData.userName} Captured {flagTeam}'s Flag");
    //    }
    //}

    //[Server]
    //public void CmdCheckCaptureFlag(UserData userData) {
    //    if(userData && !userData.isCarringEnemyFlag && userData.team != flagTeam && !isCaptured) {

    //        userData.isCarringEnemyFlag = true;
    //        userData.enemyFlag = this;
    //        Activate(false);
    //        isCaptured = true;

    //        Debug.Log($"Server RPC => {userData.userName} Captured {flagTeam}'s Flag");
    //        OnFlagCaptured?.Invoke(userData, this);
    //    }

    //    UpdateToObservers(userData);
    //}

    //[ObserversRpc]
    //public void UpdateToObservers(UserData userData) {
    //    if(userData && !userData.isCarringEnemyFlag && userData.team != flagTeam && !isCaptured) {

    //        userData.isCarringEnemyFlag = true;
    //        userData.enemyFlag = this;
    //        Activate(false);
    //        isCaptured = true;

    //        Debug.Log($"Observer RPC => {userData.userName} Captured {flagTeam}'s Flag");
    //    }
    //}

    [Client(Logging = LoggingType.Off)]
    public void FlagCapture() {
        Activate(false);
        isCaptured = true;
    }

    [Client(Logging = LoggingType.Off)]
    public void FlagDrop() {
        Activate(true);
        isCaptured = false;
    }

    private void Activate(bool value) {
        GetComponent<Collider>().enabled = value;
        GetComponent<MeshRenderer>().enabled = value;
        if(miniMapVisiblehandler) {
            miniMapVisiblehandler.SetLegendActive(value);
            miniMapVisiblehandler.UpdateLegend(flagTeam);
        }
    }
}
