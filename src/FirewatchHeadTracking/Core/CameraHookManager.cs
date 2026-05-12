using CameraUnlock.Core.Protocol;
using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Owns the lifecycle of the per-camera <see cref="CameraTrackingHook"/>
    /// MonoBehaviour. Re-checks <c>Camera.main</c> periodically (it walks the
    /// scene by tag) and re-attaches the hook on camera swaps.
    /// </summary>
    internal sealed class CameraHookManager
    {
        // Camera.main calls FindGameObjectWithTag internally; rate-limit lookups.
        private const int CameraCheckInterval = 30;

        private readonly OpenTrackReceiver _receiver;
        private CameraTrackingHook _hook;
        private Camera _cachedMainCamera;
        private int _cachedMainCameraId;
        private int _checkCounter;
        private bool _isEnabled = true;

        internal CameraHookManager(OpenTrackReceiver receiver)
        {
            _receiver = receiver;
        }

        internal Camera CachedMainCamera => _cachedMainCamera;
        internal int CachedMainCameraId => _cachedMainCameraId;
        internal CameraTrackingHook Hook => _hook;

        internal void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (_hook != null)
                _hook.SetEnabled(enabled);
        }

        internal void Refresh()
        {
            if (_hook != null && _cachedMainCamera != null)
            {
                _checkCounter++;
                if (_checkCounter < CameraCheckInterval)
                    return;
                _checkCounter = 0;
            }

            Camera currentMain = Camera.main;
            if (currentMain == null) return;

            if (_hook != null && _cachedMainCamera == currentMain) return;

            if (_hook != null)
            {
                Object.Destroy(_hook);
                _hook = null;
            }

            _cachedMainCamera = currentMain;
            _cachedMainCameraId = currentMain.GetInstanceID();
            _checkCounter = 0;

            _hook = _cachedMainCamera.gameObject.AddComponent<CameraTrackingHook>();
            _hook.Initialize(_receiver);
            _hook.SetEnabled(_isEnabled);
        }

        internal void Destroy()
        {
            if (_hook != null)
            {
                Object.Destroy(_hook);
                _hook = null;
            }
        }
    }
}
