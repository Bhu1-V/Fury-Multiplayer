using System;
using UnityEngine;
using static MiniMapLegendScriptableObject;

public class MiniMapManager : MonoBehaviour {

    [SerializeField]
    MiniMapLegendScriptableObject miniMapLenged;

    public static MiniMapManager Instance;

    private void Awake() {
        if(Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
    }

    public static Sprite GetLegendTexture(LegendType type) {
        return Instance?.miniMapLenged.GetLegendTexture(type);
    }

    public static Color GetRedColor() {
        return Instance.miniMapLenged.GetRedColor();
    }

    public static LayerMask GetLegendLayer() {
        return Instance.miniMapLenged.GetLegendLayer();
    }

    public static Color GetBlueColor() {
        return Instance.miniMapLenged.GetBlueColor();
    }
}
