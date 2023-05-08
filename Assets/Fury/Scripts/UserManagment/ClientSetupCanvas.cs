using FirstGearGames.Utilities.Objects;
using FishNet.Managing;
using FishNet.Transporting;
using Fury.Managers.Gameplay.Canvases;
using Fury.Weapons;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClientSetupCanvas : MonoBehaviour {

    [SerializeField]
    TMP_InputField userNameInputField;

    [SerializeField]
    Button playButton;

    [SerializeField]
    Toggle isServerToggle;

    /// <summary>
    /// Found NetworkManager.
    /// </summary>
    private NetworkManager _networkManager;
    /// <summary>
    /// Current state of client socket.
    /// </summary>
    private LocalConnectionState _clientState = LocalConnectionState.Stopped;
    /// <summary>
    /// Current state of server socket.
    /// </summary>
    private LocalConnectionState _serverState = LocalConnectionState.Stopped;

    private CanvasGroup _canvasGroup;

    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.SetActive(true, true);
    }

    private static bool GetArg(string name) {
        var args = System.Environment.GetCommandLineArgs();
        string s = ($"SERRRR: Found Arguments = \n");
        for(int i = 0; i < args.Length; i++) {
            s += $"{args[i]}, ";
            if(args[i] == name) {
                Debug.Log(s);
                return true;
            }
        }
        Debug.Log(s);
        return false;
    }

    private void Start() {
        Application.targetFrameRate = 30;

        _networkManager = FindObjectOfType<NetworkManager>();
        if(_networkManager == null) {
            Debug.LogError("NetworkManager not found, Game Couldn't Start.");
            return;
        } else {
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        }

        if(GetArg("IsServer")) {
            _networkManager.ServerManager.StartConnection();
            GameplayCanvases.Instance.SetGameState(GameplayCanvases.GamePlayStates.Playing);
        }
        playButton.onClick.AddListener(OnClick_Play);
    }

    private void OnDestroy() {
        playButton.onClick.RemoveAllListeners();
    }

    public void OnUpdateInputField(string text) {
        if(!string.IsNullOrEmpty(text)) {
            playButton.interactable = true;
        } else {
            playButton.interactable = false;
        }
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj) {
        Debug.Log($"Client Obj connect State = {obj.ConnectionState}");
        _clientState = obj.ConnectionState;

        //if(_clientState == LocalConnectionState.Stopped) {
        //    Debug.Log($"Is Server Started {_networkManager.ServerManager.AnyServerStarted()}");

        //    if(!_networkManager.ServerManager.AnyServerStarted()) {
        //        _networkManager.ServerManager.StartConnection();
        //        //_networkManager.ClientManager.StartConnection();
        //        _canvasGroup.SetActive(false, true);
        //    }
        //}
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj) {
        Debug.Log($"Server Obj connect State = {obj.ConnectionState}");
        _serverState = obj.ConnectionState;
    }

    public void OnClick_Play() {
        if(_networkManager == null)
            return;

        string userName = userNameInputField.text;
        PlayerPrefs.SetString("user_name", userName);

        //bool shouldStartAsServer = isServerToggle.isOn;

        //try {
        //    if(shouldStartAsServer) {
        //        _networkManager.ServerManager.StartConnection();
        //    }
        //} catch(Exception e) {
        //    // If there is already a server running.
        //    Debug.LogWarning("FURY-LOG: Can't Start as Server, starting as a Client");
        //    Debug.LogError(e);
        //}

        // Start Client
        _networkManager.ClientManager.StartConnection();
        GameplayCanvases.Instance.SetGameState(GameplayCanvases.GamePlayStates.Playing);
    }

    private void Update() {
        //if(Application.isFocused ) {
        //    if(_clientState != LocalConnectionState.Stopped)
        //        _networkManager.ClientManager.StopConnection();
        //    else
        //        _networkManager.ClientManager.StartConnection();
        //}
    }
}
