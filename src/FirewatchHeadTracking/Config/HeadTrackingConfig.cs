using MelonLoader;
using UnityEngine;

namespace FirewatchHeadTracking
{
    internal static class HeadTrackingConfig
    {
        private static MelonPreferences_Category _category;

        private static MelonPreferences_Entry<int> _udpPort;
        private static MelonPreferences_Entry<float> _yawSensitivity;
        private static MelonPreferences_Entry<float> _pitchSensitivity;
        private static MelonPreferences_Entry<float> _rollSensitivity;
        private static MelonPreferences_Entry<float> _smoothing;
        private static MelonPreferences_Entry<string> _recenterKey;
        private static MelonPreferences_Entry<string> _toggleKey;
        private static MelonPreferences_Entry<string> _trackingModeKey;
        private static MelonPreferences_Entry<string> _reticleToggleKey;
        private static MelonPreferences_Entry<string> _yawModeKey;
        private static MelonPreferences_Entry<bool> _worldSpaceYaw;
        private static MelonPreferences_Entry<bool> _showReticle;
        private static MelonPreferences_Entry<bool> _positionEnabled;
        private static MelonPreferences_Entry<float> _positionSensitivityX;
        private static MelonPreferences_Entry<float> _positionSensitivityY;
        private static MelonPreferences_Entry<float> _positionSensitivityZ;
        private static MelonPreferences_Entry<bool> _invertPitch;
        private static MelonPreferences_Entry<bool> _invertPositionX;
        private static MelonPreferences_Entry<bool> _invertPositionY;
        private static MelonPreferences_Entry<bool> _invertPositionZ;
        private static MelonPreferences_Entry<float> _positionLimitX;
        private static MelonPreferences_Entry<float> _positionLimitY;
        private static MelonPreferences_Entry<float> _positionLimitZ;
        private static MelonPreferences_Entry<float> _positionLimitZBack;
        private static MelonPreferences_Entry<float> _positionSmoothing;

        // Cached parsed KeyCodes. ParseKeyCode calls Enum.Parse which is reflection-backed,
        // case-insensitive, and wrapped in try/catch — far too expensive to run on every
        // Update frame. We cache the parsed value and re-parse only when the underlying
        // string actually changes (preserves live-reload semantics).
        private static string _recenterKeyString;
        private static KeyCode _recenterKeyCached;
        private static string _toggleKeyString;
        private static KeyCode _toggleKeyCached;
        private static string _reticleToggleKeyString;
        private static KeyCode _reticleToggleKeyCached;
        private static string _trackingModeKeyString;
        private static KeyCode _trackingModeKeyCached;
        private static string _yawModeKeyString;
        private static KeyCode _yawModeKeyCached;

        internal static int UdpPort => _udpPort.Value;
        internal static float YawSensitivity => _yawSensitivity.Value;
        internal static float PitchSensitivity => _pitchSensitivity.Value;
        internal static float RollSensitivity => _rollSensitivity.Value;
        internal static float Smoothing => _smoothing.Value;
        internal static KeyCode RecenterKey => GetCachedKeyCode(_recenterKey.Value, ref _recenterKeyString, ref _recenterKeyCached, KeyCode.Home);
        internal static KeyCode ToggleKey => GetCachedKeyCode(_toggleKey.Value, ref _toggleKeyString, ref _toggleKeyCached, KeyCode.End);
        internal static KeyCode ReticleToggleKey => GetCachedKeyCode(_reticleToggleKey.Value, ref _reticleToggleKeyString, ref _reticleToggleKeyCached, KeyCode.Insert);
        internal static KeyCode TrackingModeKey => GetCachedKeyCode(_trackingModeKey.Value, ref _trackingModeKeyString, ref _trackingModeKeyCached, KeyCode.PageUp);
        internal static KeyCode YawModeKey => GetCachedKeyCode(_yawModeKey.Value, ref _yawModeKeyString, ref _yawModeKeyCached, KeyCode.PageDown);
        internal static bool WorldSpaceYaw => _worldSpaceYaw.Value;
        internal static bool ShowReticle => _showReticle.Value;
        internal static bool PositionEnabled => _positionEnabled.Value;
        internal static float PositionSensitivityX => _positionSensitivityX.Value;
        internal static float PositionSensitivityY => _positionSensitivityY.Value;
        internal static float PositionSensitivityZ => _positionSensitivityZ.Value;
        internal static bool InvertPitch => _invertPitch.Value;
        internal static bool InvertPositionX => _invertPositionX.Value;
        internal static bool InvertPositionY => _invertPositionY.Value;
        internal static bool InvertPositionZ => _invertPositionZ.Value;
        internal static float PositionLimitX => _positionLimitX.Value;
        internal static float PositionLimitY => _positionLimitY.Value;
        internal static float PositionLimitZ => _positionLimitZ.Value;
        internal static float PositionLimitZBack => _positionLimitZBack.Value;
        internal static float PositionSmoothing => _positionSmoothing.Value;

        internal static void Initialize()
        {
            _category = MelonPreferences.CreateCategory("HeadTracking", "Head Tracking");

            _udpPort = _category.CreateEntry("UdpPort", 4242, "UDP Port");
            _yawSensitivity = _category.CreateEntry("YawSensitivity", 1.0f, "Yaw Sensitivity");
            _pitchSensitivity = _category.CreateEntry("PitchSensitivity", 1.0f, "Pitch Sensitivity");
            _rollSensitivity = _category.CreateEntry("RollSensitivity", 1.0f, "Roll Sensitivity");
            _smoothing = _category.CreateEntry("Smoothing", 0.0f, "Smoothing Factor");
            _recenterKey = _category.CreateEntry("RecenterKey", "Home", "Recenter Key");
            _toggleKey = _category.CreateEntry("ToggleKey", "End", "Toggle Key");
            _reticleToggleKey = _category.CreateEntry("ReticleToggleKey", "Insert", "Reticle Toggle Key");
            _trackingModeKey = _category.CreateEntry("TrackingModeKey", "PageUp", "Tracking Mode Cycle Key");
            _yawModeKey = _category.CreateEntry("YawModeKey", "PageDown", "Yaw Mode Toggle Key");
            _worldSpaceYaw = _category.CreateEntry("WorldSpaceYaw", true, "World-Space Yaw (true = horizon-locked, false = camera-local)");
            _showReticle = _category.CreateEntry("ShowReticle", true, "Show Reticle");
            _positionEnabled = _category.CreateEntry("PositionEnabled", true, "Position Tracking Enabled");
            _positionSensitivityX = _category.CreateEntry("PositionSensitivityX", 1.0f, "Position Sensitivity X");
            _positionSensitivityY = _category.CreateEntry("PositionSensitivityY", 1.0f, "Position Sensitivity Y");
            _positionSensitivityZ = _category.CreateEntry("PositionSensitivityZ", 1.0f, "Position Sensitivity Z");
            _invertPitch = _category.CreateEntry("InvertPitch", false, "Invert Pitch");
            _invertPositionX = _category.CreateEntry("InvertPositionX", false, "Invert Position X");
            _invertPositionY = _category.CreateEntry("InvertPositionY", false, "Invert Position Y");
            _invertPositionZ = _category.CreateEntry("InvertPositionZ", true, "Invert Position Z");
            _positionLimitX = _category.CreateEntry("PositionLimitX", 0.30f, "Position Limit X (side-to-side, meters)");
            _positionLimitY = _category.CreateEntry("PositionLimitY", 0.20f, "Position Limit Y (up/down, meters)");
            _positionLimitZ = _category.CreateEntry("PositionLimitZ", 0.40f, "Position Limit Z forward (meters)");
            _positionLimitZBack = _category.CreateEntry("PositionLimitZBack", 0.10f, "Position Limit Z back (meters)");
            _positionSmoothing = _category.CreateEntry("PositionSmoothing", 0.15f, "Position Smoothing (0.0-1.0)");
        }

        private static KeyCode ParseKeyCode(string value, KeyCode fallback)
        {
            try
            {
                return (KeyCode)System.Enum.Parse(typeof(KeyCode), value, true);
            }
            catch
            {
                MelonLogger.Warning("Invalid key name '" + value + "', using fallback: " + fallback);
                return fallback;
            }
        }

        // Fast per-frame KeyCode getter. ReferenceEquals covers the hot case
        // (MelonPreferences returns the same interned string frame-to-frame).
        // The string-value compare catches legitimate live edits without
        // re-running Enum.Parse on every frame.
        private static KeyCode GetCachedKeyCode(string current, ref string lastString, ref KeyCode cached, KeyCode fallback)
        {
            if (ReferenceEquals(current, lastString)) return cached;
            if (current != null && lastString != null && current == lastString)
            {
                lastString = current;
                return cached;
            }
            lastString = current;
            cached = ParseKeyCode(current, fallback);
            return cached;
        }
    }
}
