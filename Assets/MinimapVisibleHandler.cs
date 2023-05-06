using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapVisibleHandler : MonoBehaviour {

    [SerializeField]
    LayerMask MinimapLegendLayer;

    [SerializeField]
    private MiniMapLegendScriptableObject.LegendType legendType;

    [SerializeField]
    GameObject legendPrefab;

    [SerializeField]
    SpriteRenderer legendSpriteRenderer;

    UserData.Team thisTeam;

    private void AddMiniMapLegendLayer() {
        MinimapLegendLayer = MiniMapManager.GetLegendLayer();
        legendSpriteRenderer.gameObject.layer = (int)Mathf.Log(MinimapLegendLayer, 2f);
        Debug.Log($"Set the Layer to {gameObject.layer}");
    }

    public void UpdateLegend(UserData.Team team = UserData.Team.None) {
        thisTeam = team;
        if(legendSpriteRenderer == null) return;
        legendSpriteRenderer.sprite = MiniMapManager.GetLegendTexture(legendType);
        if(team != UserData.Team.None) {
            bool isBlue = (team == UserData.Team.Blue);
            if(isBlue) {
                legendSpriteRenderer.color = MiniMapManager.GetBlueColor();
            } else {
                legendSpriteRenderer.color = MiniMapManager.GetRedColor();
            }
            Debug.Log($"Updated Color {legendSpriteRenderer.color}");
        }
        Debug.Log($"Updated Legend with {team}");
    }

    public void SetLegendActive(bool value) {
        legendSpriteRenderer.enabled = value;
    }

    public void UpdatePlayerWithFlagLegend(UserData.Team team) {
        if(legendSpriteRenderer == null) return;
        if(team != UserData.Team.None) {
            bool isBlue = (team == UserData.Team.Blue);
            if(isBlue) {
                legendSpriteRenderer.sprite = MiniMapManager.GetLegendTexture(MiniMapLegendScriptableObject.LegendType.BlueWithFlag);
            } else {
                legendSpriteRenderer.sprite = MiniMapManager.GetLegendTexture(MiniMapLegendScriptableObject.LegendType.RedWithFlag);
            }
            Debug.Log($"Updated Sprite {legendSpriteRenderer.sprite}");
        }
        legendSpriteRenderer.color = Color.white;
        Debug.Log($"Updated Player With Flag Legend {team}");
    }

    private void Awake() {
        GameObject legend = Instantiate(legendPrefab, transform);
        legend.name = $"{name} Legend";
        legend.transform.localPosition = Vector3.zero;
        legend.transform.localPosition = new Vector3(0, 100, 0);
        legendSpriteRenderer = legend.GetComponent<SpriteRenderer>();

        AddMiniMapLegendLayer();
        UpdateLegend();
    }
}
