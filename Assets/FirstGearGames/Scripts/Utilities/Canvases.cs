﻿using UnityEngine;



namespace FirstGearGames.Utilities.Objects {


    public static class Canvases {

        /// <returns>Returns true part is within whole.</returns>
        public static void SetActive(this CanvasGroup group, bool active, bool setAlpha) {
            //Debug.Log($"{group.gameObject} Canvas Group Set Active Called with active = {active}, setAlpha = {setAlpha}");
            if(group == null)
                return;

            if(setAlpha) {
                if(active)
                    group.alpha = 1f;
                else
                    group.alpha = 0f;
            }

            group.interactable = active;
            group.blocksRaycasts = active;
        }
    }

}