using FirstGearGames.Utilities.Maths;

using TMPro;
using UnityEngine;

namespace Fury.Managers.Gameplay.Canvases {

    public class FrameRateCanvas : MonoBehaviour {
        #region Serialized.
        /// <summary>
        /// Text to show ping.
        /// </summary>
        [Tooltip("Text to show ping.")]
        [SerializeField]
        private TextMeshProUGUI _fpsText;
        #endregion

        #region Private.
        /// <summary>
        /// FrameRateCalculator.
        /// </summary>
        private FrameRateCalculator _fpsCalculator = new FrameRateCalculator();
        #endregion

        private void Update() {
            _fpsCalculator.Update(Time.unscaledDeltaTime);
        }

        private void FixedUpdate() {
            _fpsText.text = _fpsCalculator.GetIntFrameRate().ToString() + " FPS";
        }

    }


}