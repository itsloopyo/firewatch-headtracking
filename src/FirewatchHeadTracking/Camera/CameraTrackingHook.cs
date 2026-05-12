using System;
using CameraUnlock.Core.Protocol;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// MonoBehaviour on the main camera. Owns the gameplay-vs-menu predicate
    /// that gates whether tracking is applied this frame.
    ///
    /// Tracking itself is applied/removed by Harmony prefix/postfix on
    /// vgCameraController.LateUpdate (see CameraControllerPatch). This hook
    /// only answers "should we track right now?" via ShouldTrack().
    /// </summary>
    public sealed class CameraTrackingHook : MonoBehaviour
    {
        // Rate-limit gameplay detection: the full check runs on the first
        // ShouldTrack() call after a SetEnabled change, then once every
        // GameplayCheckInterval frames. ~6 frames ≈ 100ms at 60fps, which is
        // imperceptible on pause/unpause and within the CLAUDE.md guidance.
        private const int GameplayCheckInterval = 6;

        private OpenTrackReceiver _receiver;
        private Camera _camera;
        private bool _isEnabled;

        private int _gameplayCheckCounter = int.MaxValue;
        private bool _lastGameplayResult;

        // Firewatch ships a pre-5.4 Unity that doesn't have
        // SceneManager.sceneLoaded / activeSceneChanged. Poll the scene name
        // inside the rate-limited gameplay check instead of subscribing.
        private static bool _isPausedGetterDisabled;

        public void Initialize(OpenTrackReceiver receiver)
        {
            _receiver = receiver;
            _camera = GetComponent<Camera>();
            _isEnabled = true;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _gameplayCheckCounter = int.MaxValue;
        }

        private static bool IsMenuScene(string name) =>
            string.IsNullOrEmpty(name) ||
            string.Equals(name, "boot", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "menu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "main_menu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "MainMenu", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true when the player has control: cursor locked, game
        /// not paused, current scene is not a menu/boot.
        /// </summary>
        private static bool CheckInGameplay()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return false;

            var isPausedGetter = GameTypeResolver.IsPausedGetter;
            if (isPausedGetter != null && !_isPausedGetterDisabled)
            {
                try
                {
                    if (isPausedGetter())
                        return false;
                }
                catch (Exception ex)
                {
                    _isPausedGetterDisabled = true;
                    MelonLogger.Warning("vgPauseManager.isPaused threw; disabling pause detection: " + ex.Message);
                }
            }

            return !IsMenuScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Returns true if tracking should be active this frame. Called by the
        /// Harmony postfix to decide whether to apply tracking. The expensive
        /// gameplay-state check is rate-limited; cheap gates (_isEnabled,
        /// receiver, camera) re-evaluate every frame so instant-disable paths
        /// still fire immediately.
        /// </summary>
        public bool ShouldTrack()
        {
            if (_gameplayCheckCounter >= GameplayCheckInterval)
            {
                _gameplayCheckCounter = 0;
                _lastGameplayResult = CheckInGameplay();
            }
            else
            {
                _gameplayCheckCounter++;
            }

            return _lastGameplayResult && _isEnabled &&
                   NullHelper.NotNull(_receiver) && _receiver.IsReceiving &&
                   _camera != null;
        }
    }
}
