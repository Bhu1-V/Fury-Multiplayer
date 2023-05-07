using FirstGearGames.Utilities.Objects;
using Fury.Managers.Gameplay.Canvases;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionInformationCanvas : MonoBehaviour {
    /// <summary>
    /// CanvasGroup on this object.
    /// </summary>
    private CanvasGroup _canvasGroup;

    public enum SessionInfoCanvasState {
        Playing,
        SessionEnd,
    }

    [SerializeField]
    private SessionInfoCanvasState sessionState;

    private bool isVisible;

    [SerializeField]
    private GameObject playerInfoLogGameObject;

    [SerializeField]
    private Transform redInfoParent;

    [SerializeField]
    private Transform blueInfoParent;

    [SerializeField]
    private TextMeshProUGUI teamWonText;

    [SerializeField]
    private Transform teamWonTransform;

    [SerializeField]
    private Button playAgainButton;

    private Dictionary<string, PlayerInfoEntry> sessionInfo = new();

    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.SetActive(false, true);
        SetSessionCanvasState(SessionInfoCanvasState.Playing);
    }

    private void OnEnable() {
        playAgainButton.onClick.AddListener(SessionManager.Instance.OnClickPlayAgain);
    }

    private void OnDisable() {
        playAgainButton.onClick.RemoveAllListeners();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Tab) &&
          (GameplayCanvases.Instance.GetGameState() == GameplayCanvases.GamePlayStates.Playing ||
           GameplayCanvases.Instance.GetGameState() == GameplayCanvases.GamePlayStates.Death ||
           GameplayCanvases.Instance.GetGameState() == GameplayCanvases.GamePlayStates.TabPressed)) {
            ToggleCanvas();
        }
    }

    public void RemoveSessionLog() {
        sessionInfo.Clear();
        new List<PlayerInfoEntry>(blueInfoParent.GetComponentsInChildren<PlayerInfoEntry>()).ForEach(x => Destroy(x.gameObject));
        new List<PlayerInfoEntry>(redInfoParent.GetComponentsInChildren<PlayerInfoEntry>()).ForEach(x => Destroy(x.gameObject));
    }

    /// <summary>
    /// Sets cursor visibility. //todo Test code. This needs to go somewhere else but I'm feeling lazy.
    /// </summary>
    /// <param name="visible"></param>
    private void SetCursorVisibility(bool visible) {
        CursorLockMode lockMode = (visible) ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.lockState = lockMode;
        Cursor.visible = visible;
    }

    private void ToggleCanvas() {
        Debug.Log($"Toggling Canvas");
        isVisible = !isVisible;
        if(isVisible) {
            GameplayCanvases.Instance.SetGameState(GameplayCanvases.GamePlayStates.TabPressed);
        } else {
            GameplayCanvases.Instance.SetGameState(GameplayCanvases.GamePlayStates.Playing);
        }
        SetCursorVisibility(isVisible);
        _canvasGroup.SetActive(isVisible, true);
    }

    public void AddRedPlayer(string userName, uint Xp) {
        if(sessionInfo.ContainsKey(userName)) {
            if(sessionInfo[userName])
                sessionInfo[userName].SetXp(Xp);
            return;
        }
        PlayerInfoEntry piEntry = Instantiate(playerInfoLogGameObject, redInfoParent).GetComponent<PlayerInfoEntry>();
        piEntry.Initalize(true, userName, Xp);
        sessionInfo[userName] = piEntry;
    }

    public void AddBluePlayer(string userName, uint Xp) {
        if(sessionInfo.ContainsKey(userName)) {
            sessionInfo[userName].SetXp(Xp);
            return;
        }
        PlayerInfoEntry piEntry = Instantiate(playerInfoLogGameObject, blueInfoParent).GetComponent<PlayerInfoEntry>();
        piEntry.Initalize(false, userName, Xp);
        sessionInfo[userName] = piEntry;
    }

    public void ClearPlayers() {
        foreach(string key in sessionInfo.Keys) {
            sessionInfo.Remove(key, out PlayerInfoEntry piE);
            Destroy(piE?.gameObject);
        }
    }

    public void RemovePlayerLog(string userName) {
        bool removed = sessionInfo.Remove(userName, out PlayerInfoEntry piEntry);
        Debug.Log($"Removing {userName}, removed = {removed}");
        if(removed) {
            Destroy(piEntry.gameObject);
        }
    }

    public void UpdateInfo(UserData userData) {
        if(userData && sessionInfo.ContainsKey(userData.userName)) {
            sessionInfo[userData.userName].SetXp(userData.score);
        }
    }

    public void SetSessionCanvasState(SessionInfoCanvasState state) {
        sessionState = state;
        switch(state) {
            case SessionInfoCanvasState.Playing: {
                    SetCanvasPlaying();
                    break;
                }
            case SessionInfoCanvasState.SessionEnd: {
                    SetCanvasSessionEnd();
                    break;
                }
        }
    }

    private void SetCanvasSessionEnd() {
        SetCursorVisibility(true);
        playAgainButton.gameObject.SetActive(true);
        teamWonTransform.gameObject.SetActive(true);
    }

    private void SetCanvasPlaying() {
        playAgainButton.gameObject.SetActive(false);
        teamWonTransform.gameObject.SetActive(false);
    }

    public void ResetLog() {
        sessionInfo.Clear();
        new List<PlayerInfoEntry>(blueInfoParent.GetComponentsInChildren<PlayerInfoEntry>()).ForEach(x => Destroy(x.gameObject));
        new List<PlayerInfoEntry>(redInfoParent.GetComponentsInChildren<PlayerInfoEntry>()).ForEach(x => Destroy(x.gameObject));
    }

    public void SetWinText(int status) {
        //
        // Status -1 => Red
        // Status 0  => Draw
        // Status 1  => Blue
        //
        if(status == 0) {
            teamWonText.text = "DRAW";
        } else {
            teamWonText.text = $"Team {((status < 0) ? ("Red") : ("Blue"))} Won";
        }
    }
}
