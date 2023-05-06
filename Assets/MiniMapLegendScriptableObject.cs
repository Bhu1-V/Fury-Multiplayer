using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MiniMapLegends", menuName = "Fury/MiniMapLegnedManager", order = 1)]
public class MiniMapLegendScriptableObject : ScriptableObject {
    [Serializable]
    public enum LegendType {
        None = -1,
        Player,
        Flag,
        RedWithFlag,
        BlueWithFlag
    }

    [Serializable]
    public class MiniMapLegend {
        public LegendType type;
        public Sprite texture;
    }

    [SerializeField]
    LayerMask MinimapLegendLayer;

    [SerializeField]
    private List<MiniMapLegend> miniMapLegends;

    [SerializeField]
    private Color blueColor;


    [SerializeField]
    private Color redColor;

    public LayerMask GetLegendLayer() {
        return MinimapLegendLayer;
    }

    public Sprite GetLegendTexture(LegendType type) {
        if(miniMapLegends.Count < 1) return null;

        MiniMapLegend legend = miniMapLegends.Find(x => x.type == type);
        if(legend == null) return null;
        else return legend.texture;
    }

    public Color GetRedColor() {
        return redColor;
    }

    public Color GetBlueColor() {
        return blueColor;
    }
}