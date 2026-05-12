using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Briefly swaps camera.transform.rotation to the head-tracked rotation between
    /// OnPreCull and OnPostRender so screen-space effects (lens flares, light
    /// halos) sample the correct view direction. The transform is restored
    /// immediately after rendering, so game logic never observes the tracked
    /// rotation.
    ///
    /// View-matrix-only modification is not enough for these effects in Unity 2017
    /// because they read camera.transform directly rather than worldToCameraMatrix.
    /// </summary>
    internal sealed class ScreenSpaceRenderRotation
    {
        // Camera.onPreCull fires for every camera in the scene each frame
        // (UI, reflections, shadows). We compare GetInstanceID() (int) instead
        // of using Unity's overloaded `==` operator, which is a native interop
        // call. State is pushed in via SetState() from the postfix that already
        // runs each LateUpdate, so the per-camera path holds zero delegate
        // dispatches and one int compare for non-target cameras.
        private int _targetInstanceId;
        private bool _active;
        private Quaternion _trackedRotation;

        private Quaternion _savedRotation;
        private bool _modified;

        public void SetActive(Camera target, Quaternion trackedRotation)
        {
            if (target == null)
            {
                _active = false;
                return;
            }
            _targetInstanceId = target.GetInstanceID();
            _trackedRotation = trackedRotation;
            _active = true;
        }

        /// <summary>
        /// Same as SetActive(Camera, Quaternion) but accepts a pre-cached instance
        /// ID so the LateUpdate postfix doesn't have to cross into native every
        /// frame to read it. The caller (CameraHookManager) already knows the ID
        /// because it fetched the camera via Camera.main.
        /// </summary>
        public void SetActive(int targetInstanceId, Quaternion trackedRotation)
        {
            _targetInstanceId = targetInstanceId;
            _trackedRotation = trackedRotation;
            _active = true;
        }

        public void SetInactive()
        {
            _active = false;
        }

        public void Subscribe()
        {
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
        }

        public void Unsubscribe()
        {
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPostRender -= OnCameraPostRender;
        }

        private void OnCameraPreCull(Camera cam)
        {
            if (!_active) return;
            if (cam.GetInstanceID() != _targetInstanceId) return;

            _savedRotation = cam.transform.rotation;
            cam.transform.rotation = _trackedRotation;
            _modified = true;
        }

        private void OnCameraPostRender(Camera cam)
        {
            if (!_modified) return;
            if (cam.GetInstanceID() != _targetInstanceId) return;

            cam.transform.rotation = _savedRotation;
            _modified = false;
        }
    }
}
