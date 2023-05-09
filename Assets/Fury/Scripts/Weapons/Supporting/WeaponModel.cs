using Fury.Characters.Cameras;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fury.Weapons {


    public class WeaponModel : MonoBehaviour {
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Exit point for this weapon.")]
        [SerializeField]
        private Transform _exitPoint;

        [Tooltip("ADS point for this weapon.")]
        [SerializeField]
        private Transform _adsPoint;
        /// <summary>
        /// Exit point for this weapon.
        /// </summary>
        public Transform ExitPoint { get { return _exitPoint; } }
        public Transform ADSPoint { get { return _adsPoint; } }

        [SerializeField]
        private Transform tempTransform;
        private bool isAds = false;

        private float adsFOV = 0;
        private float normalFOV = 0;

        private float adsTime = 0.3f;


        IEnumerator AnimateAim(float oldValue, float newValue) {
            fpsCamera = GetComponentInParent<FPSCamera>();
            cameras = fpsCamera.GetComponentsInChildren<Camera>();
            Transform superParentTransform = GetComponentInParent<Animator>().transform;
            float step = 0;
            while(step < adsTime) {
                step += Time.deltaTime;
                yield return null;
                float lerpValue = Mathf.Lerp(oldValue, newValue, 1 - (adsTime - step));
                Array.ForEach(cameras, x => x.fieldOfView = lerpValue);
            }
        }

        FPSCamera fpsCamera;
        Camera[] cameras;

        public void SetADS() {
            if(adsFOV == 0 || normalFOV == 0) {
                fpsCamera = GetComponentInParent<FPSCamera>();
                cameras = fpsCamera.GetComponentsInChildren<Camera>();
                if(cameras.Length > 0) {
                    normalFOV = cameras[0].fieldOfView;
                    adsFOV = normalFOV - 43f;
                }
            }

            if(!isAds) {
                tempTransform.localPosition = transform.localPosition;
                tempTransform.localRotation = transform.localRotation;
                tempTransform.localScale = transform.localScale;

                transform.localPosition = _adsPoint.localPosition;
                transform.localRotation = _adsPoint.localRotation;
                transform.localScale = _adsPoint.localScale;

                StopAllCoroutines();
                StartCoroutine(AnimateAim(normalFOV, adsFOV));
                isAds = true;
            }
        }

        public void ResetADS() {
            if(isAds) {
                transform.localPosition = tempTransform.localPosition;
                transform.localRotation = tempTransform.localRotation;
                transform.localScale = tempTransform.localScale;

                StopAllCoroutines();
                StartCoroutine(AnimateAim(adsFOV, normalFOV));
                isAds = false;
            }
        }


    }
}