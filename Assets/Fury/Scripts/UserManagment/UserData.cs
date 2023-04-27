using Fury.Managers.Gameplay;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UserData : NetworkBehaviour {
    [Serializable]
    public enum Team {
        None = -1,
        Red,
        Blue
    }

    [SyncVar]
    public string userName = "";

    [SyncVar]
    public uint score = 0;

    [SyncVar(OnChange = nameof(TeamChanged))]
    public Team _team = Team.None;
    public Team team {
        get {
            return _team;
        }

        set {
            if(!IsOwner) return;
            _team = value;
        }
    }

    [SerializeField]
    SkinnedMeshRenderer bodySurface;

    [SerializeField]
    SkinnedMeshRenderer armSurface;

    [SerializeField]
    Material redMaterial;

    [SerializeField]
    Material blueMaterial;

    [field: SyncVar(ReadPermissions = ReadPermission.ExcludeOwner)]
    public bool IsCarringEnemyFlag { get; [ServerRpc(RunLocally = true)] set; }

    public override void OnStartClient() {
        base.OnStartClient();
        if(!IsOwner) return;
        string userNameText = PlayerPrefs.GetString("user_name", "");
        Debug.Log($"OnClient Started And Calling Server RPC to Assign Team Setting UserName = {userName}");
        CmdAssignTeam(this, userNameText);
    }

    [ServerRpc]
    public void CmdAssignTeam(UserData clientUserData, string userName) {
        Debug.Log($"Server RPC Called to Assign Team with UserData = {clientUserData.userName}, {clientUserData.team}");
        // if userName is null or Empty generate Random string
        if(string.IsNullOrEmpty(userName)) {
            userName = Helper.GenerateRandomString(10);
        }
        clientUserData.userName = userName;
        score = 0;
        SessionManager.Instance.AddPlayerToTeam(NetworkObject);
    }

    public override void OnStopServer() {
        base.OnStopServer();
        if(!IsServer) return;
        Debug.Log($"Removing Server {this.userName} from {team}");
        SessionManager.Instance.RemoveFromTeam(NetworkObject);

        if(IsCarringEnemyFlag) {
            InstanceFinder.ServerManager.Broadcast(new Flag.FlagDropData { droppedFlagTeam = ((team == Team.Red) ? Team.Blue : Team.Red) });
        }
    }

    public override void OnStopClient() {
        base.OnStopClient();
        if(IsOwner && !IsServer) {
            Debug.Log($"Removing Client {userName} from {team}");
            SessionManager.Instance.RemoveFromTeam(NetworkObject);
        }
    }

    private void OnDestroy() {
        if(IsServer) {
            SessionManager.Instance.RemoveFromTeam(NetworkObject);
        }
    }

    [Server]
    public void AssignTeam(Team team) {
        if(!IsServer) return;
        Debug.Log($"Assigning Team");
        this._team = team;
    }

    public void TeamChanged(Team oldValue, Team newValue, bool asServer) {
        Debug.Log($"Team got Changed from {oldValue} to {newValue} and asServer = {asServer}");
        if(team == Team.Blue) {
            bodySurface.material = blueMaterial;
            armSurface.material = blueMaterial;
        } else {
            bodySurface.material = redMaterial;
            armSurface.material = redMaterial;
        }
        if(!IsOwner) return;
        if(SessionManager.Instance.IsOwner) {
            SessionManager.Instance.UpdatePlayerTeam(userName, oldValue, newValue);
        }
        GameObject.FindGameObjectWithTag("crosshair").GetComponent<Image>().color = (team == Team.Blue) ? Color.blue : Color.red;
    }

    public string GetData() {
        return $"ID = {userName}, team = {team}, score = {score}";
    }

    public static event Action<string, int, uint> OnScoreUpdated;

    public void UpdateScore(int scoreAdd) {
        if(!IsServer) return;
        Debug.Log($"Updating Score adding {scoreAdd}");
        score += (uint)scoreAdd;
        SessionManager.Instance.UpdatePlayerData(this);
        OnScoreUpdated?.Invoke(userName, scoreAdd, score);
    }
}
