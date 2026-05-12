using System;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using FirewatchHeadTracking.Patches;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FirewatchHeadTracking.FirewatchHeadTrackingMod), "Firewatch Head Tracking", "0.0.0", "itsloopyo")]
[assembly: MelonGame("CampoSanto", "Firewatch")]

namespace FirewatchHeadTracking
{
    public sealed class FirewatchHeadTrackingMod : MelonMod
    {
        public const string ModName = "Firewatch Head Tracking";
        public const string ModVersion = "0.0.0";

        public static FirewatchHeadTrackingMod Instance { get; private set; }

        private OpenTrackReceiver _receiver;
        private CameraController _cameraController;
        private AimController _aimController;
        private ReticleAnchor _reticleAnchor;
        private ScreenSpaceRenderRotation _renderRotationSwap;
        private HotkeyHandler _hotkeys;
        private CameraHookManager _cameraHookManager;

        private bool _isEnabled;
        private bool _hasRenderData;
        private bool _wasConnected;
        private bool _reticleEnabled = true;

        // Tracking-mode cycle state: 0=both, 1=rotation only, 2=position only.
        private int _trackingMode;

        public override void OnInitializeMelon()
        {
            Instance = this;
            LoggerInstance.Msg("Initializing " + ModName + " v" + ModVersion + "...");

            HeadTrackingConfig.Initialize();
            _reticleEnabled = HeadTrackingConfig.ShowReticle;

            _receiver = new OpenTrackReceiver { Log = msg => LoggerInstance.Msg(msg) };
            _receiver.Start(HeadTrackingConfig.UdpPort);

            _cameraController = BuildCameraController(_receiver);
            _aimController = new AimController();
            _reticleAnchor = new ReticleAnchor(msg => LoggerInstance.Msg(msg));
            _renderRotationSwap = new ScreenSpaceRenderRotation();
            _hotkeys = new HotkeyHandler(this);
            _cameraHookManager = new CameraHookManager(_receiver);

            _isEnabled = true;
            _renderRotationSwap.Subscribe();
            PatchCameraController();

            LoggerInstance.Msg(ModName + " loaded! Port: " + HeadTrackingConfig.UdpPort +
                ", Toggle: " + HeadTrackingConfig.ToggleKey +
                ", Recenter: " + HeadTrackingConfig.RecenterKey);
        }

        private static CameraController BuildCameraController(OpenTrackReceiver receiver)
        {
            var processor = new TrackingProcessor
            {
                SmoothingFactor = HeadTrackingConfig.Smoothing,
                Sensitivity = new SensitivitySettings(
                    HeadTrackingConfig.YawSensitivity,
                    HeadTrackingConfig.PitchSensitivity,
                    HeadTrackingConfig.RollSensitivity,
                    invertYaw: false, invertPitch: HeadTrackingConfig.InvertPitch, invertRoll: false),
                Deadzone = DeadzoneSettings.None,
            };

            var positionProcessor = new PositionProcessor
            {
                TrackerPivotForward = 0.01f,
                Settings = new PositionSettings(
                    HeadTrackingConfig.PositionSensitivityX,
                    HeadTrackingConfig.PositionSensitivityY,
                    HeadTrackingConfig.PositionSensitivityZ,
                    HeadTrackingConfig.PositionLimitX,
                    HeadTrackingConfig.PositionLimitY,
                    HeadTrackingConfig.PositionLimitZ,
                    HeadTrackingConfig.PositionLimitZBack,
                    HeadTrackingConfig.PositionSmoothing,
                    invertX: HeadTrackingConfig.InvertPositionX,
                    invertY: HeadTrackingConfig.InvertPositionY,
                    invertZ: HeadTrackingConfig.InvertPositionZ),
            };

            return new CameraController(
                receiver,
                processor,
                new PoseInterpolator(),
                HeadTrackingConfig.Smoothing,
                positionProcessor,
                new PositionInterpolator())
            {
                PositionEnabled = HeadTrackingConfig.PositionEnabled,
                WorldSpaceYaw = HeadTrackingConfig.WorldSpaceYaw,
            };
        }

        private void PatchCameraController()
        {
            try
            {
                var cameraControllerType = AccessTools.TypeByName("vgCameraController");
                if (NullHelper.IsNull(cameraControllerType))
                {
                    LoggerInstance.Error("vgCameraController type not found - head tracking decoupling will NOT work!");
                    return;
                }

                var lateUpdate = AccessTools.Method(cameraControllerType, "LateUpdate");
                if (NullHelper.IsNull(lateUpdate))
                {
                    LoggerInstance.Error("vgCameraController.LateUpdate not found - head tracking decoupling will NOT work!");
                    return;
                }

                var prefix = new HarmonyMethod(typeof(CameraControllerPatch), "LateUpdatePrefix");
                var postfix = new HarmonyMethod(typeof(CameraControllerPatch), "LateUpdatePostfix");
                HarmonyInstance.Patch(lateUpdate, prefix: prefix, postfix: postfix);
                LoggerInstance.Msg("Patched vgCameraController.LateUpdate (prefix+postfix) - decoupling active");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to apply vgCameraController patches: " + ex);
            }
        }

        /// <summary>
        /// Harmony PREFIX on vgCameraController.LateUpdate.
        /// Resets the view matrix so the game's camera positioning sees clean state.
        /// </summary>
        internal void RemoveTrackingOffset()
        {
            Camera cam = _cameraHookManager?.CachedMainCamera;
            if (cam != null && _hasRenderData)
            {
                cam.ResetWorldToCameraMatrix();
                _hasRenderData = false;
                _renderRotationSwap?.SetInactive();
            }
        }

        /// <summary>
        /// Harmony POSTFIX on vgCameraController.LateUpdate.
        /// Applies head tracking to the view matrix so rendering sees the tracked
        /// view, then updates the aim offset and reticle position.
        /// </summary>
        internal void ApplyHeadTracking()
        {
            if (_cameraController == null || _cameraHookManager == null) return;
            Camera cam = _cameraHookManager.CachedMainCamera;
            CameraTrackingHook hook = _cameraHookManager.Hook;
            if (cam == null || hook == null || !hook.ShouldTrack()) return;

            _cameraController.ApplyTracking(cam);
            _hasRenderData = true;
            _renderRotationSwap.SetActive(_cameraHookManager.CachedMainCameraId, _cameraController.LastModifiedRotation);

            // Done in the postfix because MonoBehaviour.OnPreCull is unreliable on MelonLoader 0.5.7 + Unity 2017.
            _aimController.UpdateAim(cam, _cameraController.LastTrackedViewMatrix);
            if (_reticleEnabled)
                _reticleAnchor.UpdatePosition(_aimController.ScreenOffset);
        }

        public override void OnUpdate()
        {
            _hotkeys.Update();
            UpdateConnectionState();
        }

        private void UpdateConnectionState()
        {
            bool isConnected = _receiver != null && _receiver.IsReceiving;
            if (isConnected && !_wasConnected)
            {
                _wasConnected = true;
                Recenter();
                LoggerInstance.Msg("OpenTrack connected - recentered");
            }
            else if (_wasConnected && !isConnected)
            {
                _wasConnected = false;
                LoggerInstance.Msg("OpenTrack disconnected");
            }
        }

        public override void OnLateUpdate()
        {
            _cameraHookManager?.Refresh();
        }

        public void Recenter()
        {
            if (_cameraController == null)
                throw new InvalidOperationException("Cannot recenter: CameraController not initialized.");

            _cameraController.Recenter();
            LoggerInstance.Msg("Recentered");
        }

        public void ToggleReticle()
        {
            _reticleEnabled = !_reticleEnabled;
            LoggerInstance.Msg(_reticleEnabled ? "Reticle follow enabled" : "Reticle follow disabled");
            if (!_reticleEnabled)
                _reticleAnchor.ResetPosition();
        }

        public void ToggleYawMode()
        {
            if (_cameraController == null) return;
            _cameraController.WorldSpaceYaw = !_cameraController.WorldSpaceYaw;
            LoggerInstance.Msg(_cameraController.WorldSpaceYaw
                ? "Yaw mode: world-space (horizon-locked)"
                : "Yaw mode: camera-local");
        }

        public void CycleTrackingMode()
        {
            _trackingMode = (_trackingMode + 1) % 3;
            bool rotationOn, positionOn;
            string label;
            switch (_trackingMode)
            {
                case 1:
                    rotationOn = true; positionOn = false;
                    label = "rotation only (position disabled)";
                    break;
                case 2:
                    rotationOn = false; positionOn = true;
                    label = "position only (rotation disabled)";
                    break;
                default:
                    rotationOn = true; positionOn = true;
                    label = "rotation and position";
                    break;
            }
            _cameraController.RotationEnabled = rotationOn;
            _cameraController.PositionEnabled = positionOn;
            LoggerInstance.Msg("Tracking mode: " + label);
        }

        public void ToggleTracking()
        {
            _isEnabled = !_isEnabled;
            LoggerInstance.Msg(_isEnabled ? "Tracking enabled" : "Tracking disabled");

            _cameraHookManager?.SetEnabled(_isEnabled);

            if (!_isEnabled)
            {
                RemoveTrackingOffset();
                _cameraController?.ResetCamera();
                _reticleAnchor?.ResetPosition();
            }
        }

        public override void OnDeinitializeMelon()
        {
            _renderRotationSwap?.Unsubscribe();

            _cameraHookManager?.Destroy();
            _cameraHookManager = null;

            // Remove our Harmony patches so a re-init doesn't double-patch
            // vgCameraController.LateUpdate.
            try { HarmonyInstance?.UnpatchSelf(); }
            catch (Exception ex) { LoggerInstance.Warning("Failed to unpatch Harmony: " + ex.Message); }

            _reticleAnchor?.ResetPosition();
            _receiver?.Dispose();
            _cameraController?.ResetCamera();
            Instance = null;
        }
    }
}
