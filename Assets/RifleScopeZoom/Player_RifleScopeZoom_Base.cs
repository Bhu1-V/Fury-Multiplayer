using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_RifleScopeZoom_Base : MonoBehaviour {

    public event EventHandler OnRifleUp;
    public event EventHandler OnRifleDown;
    public event EventHandler OnZoomIn;
    public event EventHandler OnZoomOut;

    [SerializeField] private Animator animator;

    private void Update() {
        if (Input.GetMouseButtonDown(1)) {
            animator.SetBool("RifleDown", false);
            OnRifleUp?.Invoke(this, EventArgs.Empty);
        }
        if (Input.GetMouseButtonUp(1)) {
            animator.SetBool("RifleDown", true);
            OnRifleDown?.Invoke(this, EventArgs.Empty);
        }
        if (Input.GetKeyDown(KeyCode.T)) {
            OnZoomOut?.Invoke(this, EventArgs.Empty);
        }
        if (Input.GetKeyDown(KeyCode.Y)) {
            OnZoomIn?.Invoke(this, EventArgs.Empty);
        }
    }

}