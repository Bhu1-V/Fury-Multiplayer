using FirstGearGames.Utilities.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionInformationCanvas : MonoBehaviour {
    /// <summary>
    /// CanvasGroup on this object.
    /// </summary>
    private CanvasGroup _canvasGroup;

    private bool isVisible;

    [SerializeField]
    private GameObject playerInfoLogGameObject;

    [SerializeField]
    private Transform redInfoParent;

    [SerializeField]
    private Transform blueInfoParent;

    private Dictionary<string, PlayerInfoEntry> sessionInfo = new();

    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.SetActive(false, true);
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Tab)) {
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
        isVisible = !isVisible;
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
}
