using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Computes aim offset from head tracking rotation.
    ///
    /// Since head tracking only modifies worldToCameraMatrix (never camera.transform),
    /// camera.transform.forward IS the aim direction at all times.
    ///
    /// Uses manual VP matrix projection instead of Camera.WorldToScreenPoint because
    /// Unity 2017's WorldToScreenPoint may not respect the manually-set worldToCameraMatrix
    /// during OnPreCull.
    /// </summary>
    public sealed class AimController
    {
        private const float MaxRaycastDistance = 1000f;
        private const float MinRaycastDistance = 0.5f;
        private const float DistanceSmoothingRate = 15f;
        private float _lastHitDistance = 100f;

        private Vector2 _screenOffset;

        public Vector2 ScreenOffset => _screenOffset;

        public void UpdateAim(Camera camera, Matrix4x4 trackedViewMatrix)
        {
            if (camera == null) return;

            // Cache native accessors; each .transform / Screen.* / Time.deltaTime
            // crosses into Unity native and we hit them multiple times.
            Transform camTransform = camera.transform;
            Vector3 aimDir = camTransform.forward;
            Vector3 camPos = camTransform.position;

            RaycastHit hit;
            if (Physics.Raycast(camPos, aimDir, out hit, MaxRaycastDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.distance >= MinRaycastDistance)
            {
                float t = 1f - Mathf.Exp(-DistanceSmoothingRate * Time.deltaTime);
                _lastHitDistance = Mathf.Lerp(_lastHitDistance, hit.distance, t);
            }

            // Project aim point through the tracked VP matrix manually.
            // Unity 2017's camera.worldToCameraMatrix returns the auto-derived matrix
            // during OnPreCull, not our manually-set tracked matrix. Use the explicit
            // tracked matrix passed from CameraController.ApplyTracking.
            Vector3 aimWorldPoint = camPos + aimDir * _lastHitDistance;
            Matrix4x4 vp = camera.projectionMatrix * trackedViewMatrix;
            Vector4 clip = vp * new Vector4(aimWorldPoint.x, aimWorldPoint.y, aimWorldPoint.z, 1f);

            float screenW = Screen.width;
            float screenH = Screen.height;

            if (clip.w > 0f)
            {
                float invW = 1f / clip.w;
                _screenOffset = new Vector2(
                    clip.x * invW * screenW * 0.5f,
                    clip.y * invW * screenH * 0.5f);
            }
            else
            {
                // Aim is behind camera — clamp to screen edge
                _screenOffset = new Vector2(
                    Mathf.Clamp(-clip.x, -1f, 1f) * screenW * 0.45f,
                    Mathf.Clamp(-clip.y, -1f, 1f) * screenH * 0.45f);
            }
        }
    }
}
