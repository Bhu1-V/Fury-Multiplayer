using FirstGearGames.Utilities.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fury.Managers.Gameplay.Canvases {
    public class GameplayCanvases : MonoBehaviour {

        [Serializable]
        public enum GamePlayStates {
            None,
            StartScreen,
            Playing,
            Death,
            TabPressed,
            SessionEnd
        }

        public static GameplayCanvases Instance;

        [SerializeField] List<CanvasGroup> StartScreenCanvases;
        [SerializeField] List<CanvasGroup> PlayingCanvases;
        [SerializeField] List<CanvasGroup> DeathCanvases;
        [SerializeField] List<CanvasGroup> TabPressedCanvases;
        [SerializeField] List<CanvasGroup> SessionEndCanvases;

        [SerializeField]
        private GamePlayStates gameState;

        private void Awake() {
            if(Instance == null) {
                Instance = this;
                gameState = GamePlayStates.None;
                DisableCanvases(gameState);
            } else {
                Destroy(this);
            }
        }

        private void Start() {
            SetGameState(GamePlayStates.StartScreen);
        }

        public void DisableCanvases(GamePlayStates state) {

            void DisableCanvasGroups(List<CanvasGroup> canvasGroups) {
                foreach(CanvasGroup can in canvasGroups) {
                    can.SetActive(false, true);
                }
            }

            switch(state) {
                case GamePlayStates.StartScreen: {
                        DisableCanvasGroups(StartScreenCanvases);
                        break;
                    }
                case GamePlayStates.Playing: {
                        DisableCanvasGroups(PlayingCanvases);
                        break;
                    }
                case GamePlayStates.Death: {
                        DisableCanvasGroups(DeathCanvases);
                        break;
                    }
                case GamePlayStates.TabPressed: {
                        DisableCanvasGroups(TabPressedCanvases);
                        break;
                    }
                case GamePlayStates.SessionEnd: {
                        DisableCanvasGroups(SessionEndCanvases);
                        break;
                    }
                case GamePlayStates.None: {
                        DisableCanvasGroups(StartScreenCanvases);
                        DisableCanvasGroups(PlayingCanvases);
                        DisableCanvasGroups(TabPressedCanvases);
                        DisableCanvasGroups(SessionEndCanvases);
                        break;
                    }
            }
        }

        public void EnableCanvases(GamePlayStates state) {

            void EnableCanvasGroups(List<CanvasGroup> canvasGroups) {
                foreach(CanvasGroup can in canvasGroups) {
                    can.SetActive(true, true);
                }
            }

            switch(state) {
                case GamePlayStates.StartScreen: {
                        EnableCanvasGroups(StartScreenCanvases);
                        break;
                    }
                case GamePlayStates.Playing: {
                        EnableCanvasGroups(PlayingCanvases);
                        break;
                    }
                case GamePlayStates.Death: {
                        EnableCanvasGroups(DeathCanvases);
                        break;
                    }
                case GamePlayStates.TabPressed: {
                        EnableCanvasGroups(TabPressedCanvases);
                        break;
                    }
                case GamePlayStates.SessionEnd: {
                        EnableCanvasGroups(SessionEndCanvases);
                        break;
                    }
            }
        }

        public GamePlayStates GetGameState() {
            return gameState;
        }

        public void SetGameState(GamePlayStates newGameState) {
            if(gameState == newGameState) return;

            DisableCanvases(gameState);
            gameState = newGameState;
            if(gameState != GamePlayStates.Playing) {
                SessionManager.Instance.SetPause(true);
            } else {
                SessionManager.Instance.GetSessionInformationCanvas().SetSessionCanvasState(SessionInformationCanvas.SessionInfoCanvasState.Playing);
                SessionManager.Instance.SetPause(false);
            }
            EnableCanvases(gameState);
        }
    }
}


