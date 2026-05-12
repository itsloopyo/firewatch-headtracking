using CameraUnlock.Core.Data;
using CameraUnlock.Core.Math;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Unity.Tracking;
using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Applies head tracking rotation to the game camera additively.
    /// Rotation is applied on top of existing mouse/controller look to preserve normal controls.
    /// Delegates to shared TrackingProcessor (sensitivity, recenter, smoothing, deadzone)
    /// and PoseInterpolator (inter-sample interpolation).
    /// </summary>
    public sealed class CameraController
    {
        private readonly OpenTrackReceiver _receiver;
        private readonly TrackingProcessor _processor;
        private readonly PoseInterpolator _interpolator;
        private readonly float _smoothingFactor;
        private readonly PositionProcessor _positionProcessor;
        private readonly PositionInterpolator _positionInterpolator;

        private Camera _targetCamera;
        private Vec3 _lastPositionOffset;
        private bool _hasCentered;
        private Matrix4x4 _lastTrackedViewMatrix;
        private Quaternion _lastModifiedRotation;

        /// <summary>The final view matrix from the last ApplyTracking call.</summary>
        public Matrix4x4 LastTrackedViewMatrix => _lastTrackedViewMatrix;

        /// <summary>The final world-space rotation from the last ApplyTracking call.</summary>
        public Quaternion LastModifiedRotation => _lastModifiedRotation;

        // Output SLERP smoothing state (second smoothing layer for remote connections)
        private readonly SmoothedEulerState _smoothedState = new SmoothedEulerState();

        /// <summary>Whether positional tracking is enabled.</summary>
        public bool PositionEnabled { get; set; } = true;

        /// <summary>Whether rotational tracking is enabled.</summary>
        public bool RotationEnabled { get; set; } = true;

        /// <summary>
        /// True = yaw rotates around world up (horizon-locked).
        /// False = yaw rotates around the camera's current up-axis (leans on extreme pitch).
        /// </summary>
        public bool WorldSpaceYaw { get; set; } = true;

        /// <summary>Last applied position offset for transition fadeout.</summary>
        public Vec3 LastPositionOffset => _lastPositionOffset;

        public CameraController(OpenTrackReceiver receiver, TrackingProcessor processor, PoseInterpolator interpolator, float smoothingFactor,
            PositionProcessor positionProcessor, PositionInterpolator positionInterpolator)
        {
            _receiver = receiver;
            _processor = processor;
            _interpolator = interpolator;
            _smoothingFactor = smoothingFactor;
            _positionProcessor = positionProcessor;
            _positionInterpolator = positionInterpolator;
        }

        /// <summary>
        /// Sets the current head position as the center reference.
        /// </summary>
        public void Recenter()
        {
            var rawPose = _receiver.GetLatestPose();
            _processor.RecenterTo(rawPose);
            _interpolator.Reset();
            _smoothedState.Reset();
            _positionProcessor?.SetCenter(_receiver.GetLatestPosition());
            _positionInterpolator?.Reset();
            _lastPositionOffset = Vec3.Zero;
        }

        /// <summary>
        /// Applies head tracking rotation to the specified camera.
        /// Called by CameraTrackingHook.OnPreCull() with the hook's camera.
        /// </summary>
        public void ApplyTracking(Camera camera)
        {
            if (camera == null) return;
            _targetCamera = camera;

            // Auto-recenter on first valid frame so the user's startup head position is neutral
            if (!_hasCentered)
            {
                _hasCentered = true;
                Recenter();
            }

            // Cache hot-path accessors: Time.deltaTime and camera.transform.* each
            // cross into native on every call; we use each multiple times.
            float dt = Time.deltaTime;
            Transform camTransform = camera.transform;
            Quaternion camRotation = camTransform.rotation;
            Vector3 camPos = camTransform.position;

            ComputeSmoothedRotation(dt, out float sYaw, out float sPitch, out float sRoll);

            Quaternion modifiedRot = ComposeRotation(camRotation, sYaw, sPitch, sRoll);
            _lastModifiedRotation = modifiedRot;

            Matrix4x4 viewMatrix = BuildViewMatrix(modifiedRot, camPos);

            if (PositionEnabled && _positionProcessor != null)
                ApplyPositionOffset(ref viewMatrix, camRotation, sYaw, sPitch, sRoll, dt);

            _targetCamera.worldToCameraMatrix = viewMatrix;

            // Save the tracked matrix from the local variable — never read back
            // from camera.worldToCameraMatrix, which on Unity 2017 may return the
            // auto-derived matrix instead of our manually-set one.
            _lastTrackedViewMatrix = viewMatrix;
        }

        private void ComputeSmoothedRotation(float dt, out float sYaw, out float sPitch, out float sRoll)
        {
            // Get raw tracking data, interpolate between samples, then process
            var rawPose = _receiver.GetLatestPose();

            // Always update interpolator to maintain velocity state
            var interpolated = _interpolator.Update(rawPose, dt);

            // Use interpolated pose only when smoothing absorbs prediction corrections;
            // at smoothing=0, interpolation creates visible correction stutters
            if (_smoothingFactor >= 0.001f)
                rawPose = interpolated;

            var processed = _processor.Process(rawPose, dt);

            // Output SLERP smoothing: second smoothing layer that eliminates
            // snap-to-raw artifacts from PoseInterpolator on remote connections.
            _smoothedState.Update(processed.Yaw, processed.Pitch, processed.Roll, _smoothingFactor, dt,
                out sYaw, out sPitch, out sRoll);

            // Tracking-mode cycle: when rotation is disabled, zero deltas so the
            // camera renders with the game's clean rotation while position offset
            // still applies. Smoothed state keeps evolving, so re-enabling blends
            // back without snapping.
            if (!RotationEnabled)
            {
                sYaw = 0f;
                sPitch = 0f;
                sRoll = 0f;
            }
        }

        private Quaternion ComposeRotation(Quaternion camRotation, float sYaw, float sPitch, float sRoll)
        {
            // World-space yaw (default): yaw rotates around world up, preventing leaning
            // artifacts at steep pitch. Camera-local yaw composes Y*X*Z entirely in the
            // camera's local frame; at extreme pitch this rolls the view, which a few
            // players prefer for aerial / zero-G content.
            return WorldSpaceYaw
                ? CameraRotationComposer.ComposeAdditive(camRotation, sYaw, sPitch, sRoll)
                : camRotation * Quaternion.Euler(-sPitch, sYaw, sRoll);
        }

        private static Matrix4x4 BuildViewMatrix(Quaternion modifiedRot, Vector3 camPos)
        {
            // Analytic inverse of TRS(pos, rot, 1) = Rotate(rot^-1) * Translate(-pos).
            // Avoids Unity's general 4x4 matrix inverse (determinant/cofactor path)
            // which is substantially slower than a quaternion conjugate + mul-add.
            Matrix4x4 viewMatrix = Matrix4x4.Rotate(Quaternion.Inverse(modifiedRot));
            viewMatrix.m03 = -(viewMatrix.m00 * camPos.x + viewMatrix.m01 * camPos.y + viewMatrix.m02 * camPos.z);
            viewMatrix.m13 = -(viewMatrix.m10 * camPos.x + viewMatrix.m11 * camPos.y + viewMatrix.m12 * camPos.z);
            viewMatrix.m23 = -(viewMatrix.m20 * camPos.x + viewMatrix.m21 * camPos.y + viewMatrix.m22 * camPos.z);
            // Z-row flip: convert to Unity's left-handed view space.
            viewMatrix.m20 = -viewMatrix.m20;
            viewMatrix.m21 = -viewMatrix.m21;
            viewMatrix.m22 = -viewMatrix.m22;
            viewMatrix.m23 = -viewMatrix.m23;
            return viewMatrix;
        }

        private void ApplyPositionOffset(ref Matrix4x4 viewMatrix, Quaternion camRotation,
            float sYaw, float sPitch, float sRoll, float dt)
        {
            // Position tracking: use tracker 6DOF data via PositionProcessor.
            // Fold the position offset into the local viewMatrix struct before
            // writing to camera.worldToCameraMatrix so we only pay one native
            // matrix setter per frame instead of two.
            var rawPos = _receiver.GetLatestPosition();
            var interpolatedPos = _positionInterpolator.Update(rawPos, dt);

            var headRotQ = QuaternionUtils.FromYawPitchRoll(sYaw, -sPitch, sRoll);
            _lastPositionOffset = _positionProcessor.Process(interpolatedPos, headRotQ, dt);

            // Y is negated in the view-space delta because the TRS view matrix
            // Z-row negation inverts vertical sign vs the headRot*gameView
            // convention the offset signs were originally tuned for.
            Vector3 worldOffset = PositionApplicator.ToCameraLocalWorld(_lastPositionOffset, camRotation);
            Vector3 viewOffset = viewMatrix.MultiplyVector(worldOffset);
            viewMatrix.m03 += viewOffset.x;
            viewMatrix.m13 -= viewOffset.y;
            viewMatrix.m23 += viewOffset.z;
        }

        public void ResetCamera()
        {
            _smoothedState.Reset();
            _processor.ResetSmoothing();
            _interpolator.Reset();
            _positionProcessor?.Reset();
            _positionInterpolator?.Reset();
            _lastPositionOffset = Vec3.Zero;
        }
    }
}
