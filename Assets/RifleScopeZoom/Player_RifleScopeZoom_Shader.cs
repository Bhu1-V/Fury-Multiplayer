using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_RifleScopeZoom_Shader : MonoBehaviour {

    [SerializeField] private Material zoomMaterial;

    private void Awake() {
        Player_RifleScopeZoom_Base playerRifleScopeZoomBase = GetComponent<Player_RifleScopeZoom_Base>();
        playerRifleScopeZoomBase.OnRifleDown += PlayerRifleScopeZoomBase_OnRifleDown;
        playerRifleScopeZoomBase.OnRifleUp += PlayerRifleScopeZoomBase_OnRifleUp;
        playerRifleScopeZoomBase.OnZoomIn += PlayerRifleScopeZoomBase_OnZoomIn;
        playerRifleScopeZoomBase.OnZoomOut += PlayerRifleScopeZoomBase_OnZoomOut;
    }



    private void PlayerRifleScopeZoomBase_OnRifleUp(object sender, System.EventArgs e) {

    }

    private void PlayerRifleScopeZoomBase_OnRifleDown(object sender, System.EventArgs e) {

    }

    private void PlayerRifleScopeZoomBase_OnZoomOut(object sender, System.EventArgs e) {
        zoomMaterial.SetFloat("_ZoomAmount", .4f);
    }

    private void PlayerRifleScopeZoomBase_OnZoomIn(object sender, System.EventArgs e) {
        zoomMaterial.SetFloat("_ZoomAmount", .8f);
    }

}