﻿using Fury.Characters.Vitals;
using Fury.Characters.Weapons;
using Fury.Managers.Gameplay;
using Fury.Weapons;
using FirstGearGames.Managers.Global;
using FirstGearGames.Utilities.Maths;
using FirstGearGames.Utilities.Objects;
using FishNet.Managing.Logging;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Serializing.Helping;
using FishNet.Transporting;
using System.Linq;
using UnityEngine;
using Fury.Managers.Gameplay.Canvases;

namespace Fury.Characters.Motors {

    /// <summary>
    /// Controls server authoritative movement.
    /// </summary>
    public class Motor : NetworkBehaviour {
        [Header("Motor")]

        [Tooltip("How much to dampen external forces by per fixed update. This is applied at the beginning of fixed update.")]
        [SerializeField]
        private float _forceDampening = 5f;

        [Tooltip("How fast to move.")]
        [SerializeField]
        private float _baseMoveRate = 1.65f;

        [Space(5f)]
        [Tooltip("How high to jump.")]
        [SerializeField]
        private float _jumpHeight = 2f;

        [Tooltip("How often player can jump.")]
        [SerializeField]
        private float _jumpInterval = 1f;

        [Tooltip("Maximum amount to dampen air movement when moving in the opposite direction of initial jump direction.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _maximumAirDampening = 0.75f;

        [Header("Audio")]

        [Tooltip("Audio to play when landing.")]
        [SerializeField]
        private GameObject _landingAudioPrefab;

        [Tooltip("Audio to play when running.")]
        [SerializeField]
        private GameObject _runningAudioPrefab;


        private CharacterController _controller;
        private AnimatorController _animatorController;
        private float _verticalVelocity;
        private float _nextAllowedJumpTime = 0f;
        private bool _jump = false;
        private float _defaultStepOffset;
        private bool _isGrounded;
        private Vector3 _externalForces = Vector3.zero;
        private Vector3 _lastJumpDirection = Vector3.zero;
        private bool _running = false;
        private float _nextRunningAudioTime;
        private bool _groundedChanged = false;
        private WeaponHandler _weaponHandler;
        private float _lastLandAudioTime = -1f;
        private float _lastOwnerRotation;
        private const float WALK_CROUCH_PERCENT = 0.525f;
        private const float RUNNING_AUDIO_INTERVAL = 0.35f;
        private const float GRAVITY_MULTIPLIER = 2f;

        private void Awake() {
            this.enabled = false;
        }

        public override void OnStartNetwork() {
            base.OnStartNetwork();
            base.TimeManager.OnTick += TimeManager_OnTick;
            NetworkInitialize(base.Owner.IsLocalClient || base.IsServer);
        }

        public override void OnStopNetwork() {
            base.OnStopNetwork();
            base.TimeManager.OnTick -= TimeManager_OnTick;
        }

        /// Phsyics update step. Called before Update.
        private void TimeManager_OnTick() {
            //Authoritive client or server.
            if(base.IsOwner || base.IsServer) {
                CheckPlayLandedAudio();
            }

            //Authoritive client.
            if(base.IsOwner) {
                Reconcile(default, false);
                CheckInput(out ReplicateData rd);
                Replicate(rd, false);
            }
            //If server, and not owner.
            if(base.IsServer) {
                Replicate(default, true);
                ReconcileData rd = new ReconcileData(transform.position, _verticalVelocity, _externalForces);
                Reconcile(rd, true);
                //Send running state to other clients so they can play running vfx/sounds locally.
                ObserversSetRunning(_running);
            }
        }


        /// Frame update step.
        private void Update() {
            //If owner.
            if(base.IsOwner) {
                CheckJump();
            }
            //If any client.
            if(base.IsClient) {
                CheckPlayRunningAudio();
            }
        }


        /// Checks if landing audio should be played on owner.
        private void CheckPlayLandedAudio() {
            if(!_groundedChanged)
                return;
            _groundedChanged = false;

            //If velocity was low enough then play audio.
            if(_verticalVelocity < -5f)
                PlayLandingAudio();
        }

        /// Initializes this script for anyone with authority or if the server.
        private void NetworkInitialize(bool authoritiveOrServer) {
            _controller = GetComponent<CharacterController>();
            if(authoritiveOrServer) {
                _weaponHandler = GetComponent<WeaponHandler>();
                _animatorController = GetComponent<AnimatorController>();
                _defaultStepOffset = _controller.stepOffset;

                Health health = GetComponent<Health>();
                health.OnDeath += Health_OnDeath;
                health.OnRespawned += Health_OnRespawned;
            } else {
                _controller.enabled = false;
            }

            this.enabled = true;
        }

        /// Plays audio used for landing. Broadcast to other players afterwards.
        private void PlayLandingAudio() {
            float groundAudioInterval = 0.250f;
            if(Time.time - _lastLandAudioTime < groundAudioInterval)
                return;
            _lastLandAudioTime = Time.time;

            //If not server only then play audio.
            if(!base.IsServerOnly) {
                //First person if owner.
                if(base.IsOwner)
                    OfflineGameplayDependencies.AudioManager.PlayFirstPerson(_landingAudioPrefab);
                else
                    OfflineGameplayDependencies.AudioManager.PlayAtPoint(_landingAudioPrefab, transform.position);
            }

            //If server then send RPC to play audio.
            if(base.IsServer) {
                ObserversPlayLandingAudio();
            }
        }

        /// Plays landing audio over clients.
        [ObserversRpc]
        private void ObserversPlayLandingAudio() {
            //Ignore if owner or server as it was played locally.
            if(base.IsOwner || base.IsServer)
                return;

            PlayLandingAudio();
        }

        /// Received when character is respawned.
        private void Health_OnRespawned() {
            this.enabled = true;
            _controller.enabled = true;
        }

        /// Received when character is dead.
        private void Health_OnDeath() {
            _verticalVelocity = 0f;
            _controller.enabled = false;
            this.enabled = false;
        }

        /// Sets velocity to 0f when grounded.
        private void SetGroundedVelocity(float deltaTime, bool asServer, bool replaying) {
            /* Rapidly reduce gravity when grounded so falls aren't sudden when moving
             * off edges. Also move towards this gravity amount over time so gravity
             * isn't immediately reset upon landing, but rather is gradually to
             * give the impression of losing momentum. */
            if(_controller.isGrounded && _verticalVelocity < -1f)
                _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, -1f, (-Physics.gravity.y * GRAVITY_MULTIPLIER * 2f) * deltaTime);
        }

        /// Received when the character controller hits an object during move.
        private void OnControllerColliderHit(ControllerColliderHit hit) {
            //Cancel jump velocity if hitting something above.
            if(_verticalVelocity > 0f && hit.moveDirection.y > 0f) {
                //If hit is above middle of character controller it's safe to assume it's above.
                if(hit.point.y > transform.position.y + (_controller.height / 2f))
                    _verticalVelocity = 0f;
            }
        }

        /// Sets if the controller is grounded.
        private void SetIsGrounded(bool replaying, out bool changed) {
            //State before checking for ground.
            bool previousGrounded = _isGrounded;
            _isGrounded = CastForGround();
            changed = (previousGrounded != _isGrounded);
        }

        /// Checks for ground beneath the player.
        private bool CastForGround(float extraDistance = 0.05f) {
            float radius = _controller.radius + (_controller.skinWidth / 2f);
            //Start right in the center.
            Vector3 start = transform.position + new Vector3(0f, (_controller.height / 2f), 0f);
            float distance = (_controller.height / 2f) - (radius / 2f);

            //Check for ground.
            Ray ray = new Ray(start, Vector3.down);
            RaycastHit hit;
            SetCharacterLayer(true);
            //Disable colliders on self.
            bool isGrounded = Physics.SphereCast(ray, radius, out hit, distance + extraDistance, (GlobalManager.LayerManager.MovementBlockingLayers | GlobalManager.LayerManager.CharacterLayer));
            if(isGrounded && hit.collider.gameObject.name.Contains("Character"))
                Debug.Log(hit.collider.gameObject.name);
            SetCharacterLayer(false);
            return isGrounded;
        }

        /// Sets the layer for Hitboxes.
        private void SetCharacterLayer(bool toIgnore) {
            int layer = (toIgnore) ?
                Layers.LayerMaskToLayerNumber(GlobalManager.LayerManager.IgnoreCollisionLayer) :
                Layers.LayerMaskToLayerNumber(GlobalManager.LayerManager.CharacterLayer);

            gameObject.layer = layer;
        }

        /// Conditionally adjust steps height.
        private void SetStepOffset() {
            /* Don't allow stepping when in the air. This is so the client cannot step up on cliffs when falling in front of them.
             * This is an issue with the unity character controller that would maybe be good for ledge grabbing, but not for
             * a FPS game. */
            _controller.stepOffset = (_isGrounded && _verticalVelocity <= 0f) ? _defaultStepOffset : 0f;
        }

        /// Applies gravity to verticalVelocity.
        private void ApplyGravity(ref float verticalVelocity, float deltaTime) {
            //Multiply gravity by 2 for snappier jumps.
            verticalVelocity += (Physics.gravity.y * GRAVITY_MULTIPLIER) * deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, Physics.gravity.y * GRAVITY_MULTIPLIER);
        }

        /// Dampens current external forces.
        private void DampenExternalForces(float deltaTime) {
            _externalForces = Vector3.MoveTowards(_externalForces, Vector3.zero, _forceDampening * deltaTime);
        }

        /// Returns if can jump.
        private bool CanJump() {
            /* Check for ground using a tiny bit of extra distance. This is for when going down slopes
             * where the player may break ground slightly. */
            if(!_isGrounded)
                return false;
            //Jump already queued.
            if(_jump)
                return false;

            /* If owner require exact jump intervals, if server allow a little leanancy.
             * When server allow to jump 150ms sooner to compensate for slower packets. */
            float nextAllowedJumpTime = (base.IsServer && !base.IsOwner) ? _nextAllowedJumpTime - 0.15f : _nextAllowedJumpTime;
            if(Time.time < nextAllowedJumpTime)
                return false;

            return true;
        }

        /// Applies jump.
        private void Jump(bool replaying) {
            _verticalVelocity = _jumpHeight;

            //Only increase time and unset jump if not replay.
            if(!replaying) {
                _jump = false;
                _nextAllowedJumpTime = Time.time + _jumpInterval;
                _animatorController.Jump();
            }
        }


        /// Returns true if a blocked direction. Can be true when moving against a path the controller shouldn't allow.
        private bool BlockedDirection(ref ReplicateData input, float moveRate, float deltaTime) {
            /* ref is abused here because the structure is so large it's faster
             * to pass by reference rather than have the structure values copied. */

            /* Check for too steep of a slope. Character control is broken and
             * allows you to climb slopes you shouldn't normally be able to.
             * Only check if slope is too steep when jumping or not grounded. */
            if(!_isGrounded || _verticalVelocity > 0f) {
                //Start in the center of the character controller.
                Vector3 start = transform.position + new Vector3(0f, _controller.height / 2f, 0f);
                Vector3 estimatedImpact = transform.position + (input.WorldDirection.normalized * (_controller.radius + _controller.skinWidth + moveRate) * deltaTime);
                float distance = (start - estimatedImpact).magnitude;
                Vector3 direction = (estimatedImpact - start).normalized;
                Ray ray = new Ray(start, direction);
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit, distance, GlobalManager.LayerManager.MovementBlockingLayers)) {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if(angle > _controller.slopeLimit)
                        return true;
                }
            }

            return false;
        }

        /// Dampens a vector if it differs from facing when character jumped.
        /// This prevents characters from swaying in the air, which would be very obnoxious in a shooting game.
        private void DampenAirMovement(ref Vector3 v, bool server) {
            //Not in the air.
            if(_isGrounded)
                return;

            //If directions are the same then there is no reason to dampen.
            if(v == _lastJumpDirection)
                return;

            //If magnitude hasn't been reduced yet from client.
            if(server) {
                /* if magnitude is low then it's likely
                 * the movement has already been dampened,
                 * which means client is not cheating. */
                if(v.magnitude < 0.98f)
                    return;
                else {
                    v = v.normalized;
                }
            }

            float angle;
            if(_lastJumpDirection == Vector3.zero)
                angle = 90f;
            else
                angle = Vector3.Angle(_lastJumpDirection, v);

            float dampener = Mathf.Max(1f - _maximumAirDampening, Mathf.InverseLerp(180f, 0f, angle));
            v = new Vector3(
                    v.x * dampener,
                    v.y,
                    v.z * dampener);
        }

        /// Returns a move rate after being modified by action codes.
        private float GetMoveRate(ActionCodes actionCodes, Weapon weaponOverride = null) {
            float moveRate = _baseMoveRate;
            //Action code alterations.
            if(actionCodes.Contains(ActionCodes.Crouch) || actionCodes.Contains(ActionCodes.Walking)) {
                moveRate *= WALK_CROUCH_PERCENT;
            }
            //Alterations from weapons.
            else {
                if(weaponOverride != null)
                    moveRate *= weaponOverride.MoveRateModifier;
                else if(_weaponHandler.WeaponIndexValid())
                    moveRate *= _weaponHandler.Weapon.MoveRateModifier;
            }

            return moveRate;
        }

        /// Moves using data.
        [Replicate]
        private void Replicate(ReplicateData input, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false) {
            //If not enabled. Can occur when player is out of health, possible a command go through after.
            if(!this.enabled)
                return;
            //Could be disabled on despawn or death.
            if(!_controller.enabled)
                return;

            //Update grounded, and set if grounded changed.
            SetIsGrounded(replaying, out _groundedChanged);
            /* True if data is default. Some things shouldn't be 
             * applied if data is default. */
            bool defaultData = Comparers.IsDefault(input);
            float deltaTime = (float)base.TimeManager.TickDelta;
            //Simulate velocities.
            ApplyGravity(ref _verticalVelocity, deltaTime);
            DampenExternalForces(deltaTime);
            //When grounded use a different velocity. This gives a better feel to the motor.
            SetGroundedVelocity(deltaTime, asServer, replaying);
            //Change step offset based on if grounded or not.
            SetStepOffset();

            /* Only update rotation if asServer or not replaying.
             * Replaying would be false if asServer but having
             * the separate checks shows intent more clearly. We
             * do this because we do not want the client to update rotation
             * while replaying inputs, that would cause the camera
             * to jitter if they desyncd. Instead the character moves using
             * world positions and the client gets to retain their rotation. 
             * If you wanted to limit how fast the client could rotate you
             * would need to change this behavior. 
             *
             * Also do not apply if the data is default because this will
             * just keep resetting the client to v3.Zero. */
            if(!defaultData && (asServer || !replaying))
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, input.Rotation, transform.eulerAngles.z);

            bool jumping = input.ActionCodes.Contains(ActionCodes.Jump);
            if(jumping) {
                //Check if can jump as server to maintain server authority.
                if(asServer) {
                    if(CanJump()) {
                        _lastJumpDirection = input.WorldDirection;
                        Jump(replaying);
                    }
                }
                //Processing as client. Already went through client validation.
                else {
                    _lastJumpDirection = input.WorldDirection;
                    Jump(replaying);
                }
            }

            //Move rate can vary based on modifiers such as weapon or walking.
            float moveRate = GetMoveRate(input.ActionCodes, input.Weapon);
            /* If direction isn't 0f make sure character can move that way.
             * Don't process if a replay as original input would have already cleared
             * direction if blocked. */
            if(!replaying && input.WorldDirection != Vector3.zero) {
                if(BlockedDirection(ref input, moveRate, deltaTime))
                    input.WorldDirection = Vector3.zero;
            }

            Vector3 v = input.WorldDirection;
            DampenAirMovement(ref v, true);

            //Multiply inputs by move rate.
            v.x *= moveRate;
            v.z *= moveRate;
            //Add on vertical velocity and external forces.
            v.y += _verticalVelocity;
            v += _externalForces;

            //Apply movement to the character controller.
            if(_controller.gameObject.activeInHierarchy)
                _controller.Move(v * deltaTime);

            //Update running which will send in another method.
            bool running = (_isGrounded && input.LocalDirection != Vector3.zero && !input.ActionCodes.Contains(ActionCodes.Walking));
            SetRunning(running, replaying);
            if(asServer)
                ServerUpdateAnimator(input);
        }


        /// Checks if running audio should play.
        [Client(Logging = LoggingType.Off)]
        private void CheckPlayRunningAudio() {
            //Not running or cannot play running audio yet due to time restrictions.
            if(!_running || Time.time < _nextRunningAudioTime)
                return;

            _nextRunningAudioTime = Time.time + RUNNING_AUDIO_INTERVAL;

            if(base.IsOwner)
                OfflineGameplayDependencies.AudioManager.PlayFirstPerson(_runningAudioPrefab);
            else
                OfflineGameplayDependencies.AudioManager.PlayAtPoint(_runningAudioPrefab, transform.position);
        }

        /// Reconciles using data.
        [Reconcile]
        private void Reconcile(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable) {
            //Server doesn't actually reconcile, it just sends the data to the client.
            //Nothing to process.
            if(Comparers.IsDefault(rd))
                return;

            _verticalVelocity = rd.VerticalVelocity;
            _externalForces = rd.ExternalForces;
            transform.position = rd.Position;
        }


        /// Tries to jump using input.
        [Client(Logging = LoggingType.Off)]
        private void CheckJump() {
            if(!Input.GetKeyDown(KeyCode.Space) || SessionManager.Instance.GetPause())
                return;
            if(!CanJump())
                return;

            _jump = true;
        }

        /// Sets the running value and resets last run audio time when appropriate.
        private void SetRunning(bool value, bool replaying) {
            if(value == _running)
                return;

            _running = value;
            //If now running reset audio time to play slightly ahead of start time.
            if(!replaying && _running)
                _nextRunningAudioTime = Time.time + (RUNNING_AUDIO_INTERVAL / 2f);
        }


        /// Updates queued input on owning client.
        [Client(Logging = LoggingType.Off)]
        private void CheckInput(out ReplicateData rd) {
            float hor = 0;
            float ver = 0;
            if(!SessionManager.Instance.GetPause()) {
                hor = Input.GetAxisRaw("Horizontal");
                ver = Input.GetAxisRaw("Vertical");
            }

            Vector3 localDirection = new Vector3(hor, 0f, ver);
            Vector3 worldDirection = transform.TransformDirection(localDirection.normalized);

            //Update animator with movement direction.
            _animatorController.SetMovementDirection(localDirection);
            //Default to move action code.
            ActionCodes actionCodes = ActionCodes.None;

            bool crouched = false; //Doesn't actually do anything yet.
            bool walking = Input.GetKey(KeyCode.LeftShift);
            crouched = Input.GetKey(KeyCode.LeftControl);

            //Jump action code.
            if(_jump)
                actionCodes |= ActionCodes.Jump;
            if(walking)
                actionCodes |= ActionCodes.Walking;
            if(crouched)
                actionCodes |= ActionCodes.Crouch;
            float rotY = transform.eulerAngles.y;
            if(localDirection != Vector3.zero || rotY != _lastOwnerRotation)
                actionCodes |= ActionCodes.Move;
            _lastOwnerRotation = rotY;

            rd = new ReplicateData(
                worldDirection,
                localDirection,
                rotY,
                _weaponHandler.Weapon,
                actionCodes
                );
        }

        /// Update AnimatorController using input.
        [Server(Logging = LoggingType.Off)]
        private void ServerUpdateAnimator(ReplicateData input) {
            _animatorController.SetMovementDirection(input.LocalDirection);
        }


        /// Sent to all clients containing extra effects data after server has processed user input.
        [ObserversRpc]
        private void ObserversSetRunning(bool running, Channel c = Channel.Unreliable) {
            //Do not update for owner.
            if(base.IsOwner)
                return;

            SetRunning(running, false);
        }


    }


}