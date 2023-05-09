using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_RifleScopeZoom_RenderTexture : MonoBehaviour {

    [SerializeField] private Camera playerCamera;

    private float targetFieldOfView = 10;

    private void Awake() {
        Player_RifleScopeZoom_Base playerRifleScopeZoomBase = GetComponent<Player_RifleScopeZoom_Base>();
        playerRifleScopeZoomBase.OnRifleDown += PlayerRifleScopeZoomBase_OnRifleDown;
        playerRifleScopeZoomBase.OnRifleUp += PlayerRifleScopeZoomBase_OnRifleUp;
        playerRifleScopeZoomBase.OnZoomIn += PlayerRifleScopeZoomBase_OnZoomIn;
        playerRifleScopeZoomBase.OnZoomOut += PlayerRifleScopeZoomBase_OnZoomOut;
    }

     
    private void Update() {
        float speed = 10f;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFieldOfView, Time.deltaTime * speed);
    }

    private void PlayerRifleScopeZoomBase_OnRifleUp(object sender, System.EventArgs e) {

    }

    private void PlayerRifleScopeZoomBase_OnRifleDown(object sender, System.EventArgs e) {

    }

    private void PlayerRifleScopeZoomBase_OnZoomOut(object sender, System.EventArgs e) {
        targetFieldOfView = 10;
    }

    private void PlayerRifleScopeZoomBase_OnZoomIn(object sender, System.EventArgs e) {
        targetFieldOfView = 3;
    }

}