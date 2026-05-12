using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Polls per-frame hotkey state and dispatches to the mod's command methods.
    /// Each binding has both a configurable primary key and a Ctrl+Shift+letter
    /// chord alternative for keyboards without a nav cluster (see AGENTS.md).
    /// </summary>
    internal sealed class HotkeyHandler
    {
        private readonly FirewatchHeadTrackingMod _mod;

        internal HotkeyHandler(FirewatchHeadTrackingMod mod)
        {
            _mod = mod;
        }

        internal void Update()
        {
            // Common-case short-circuit: every action below fires only on a
            // GetKeyDown edge, so when no key transitioned to down this frame,
            // the entire body is dead work. anyKeyDown is one native call
            // vs. 9+ GetKey/GetKeyDown calls otherwise.
            if (!Input.anyKeyDown) return;

            // Modifier state is read once per frame so the chord-letter
            // Input.GetKeyDown calls are skipped entirely on the common path
            // (no Ctrl+Shift held).
            bool chordsActive =
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            if (Pressed(HeadTrackingConfig.RecenterKey, chordsActive, KeyCode.T))
                _mod.Recenter();

            if (Pressed(HeadTrackingConfig.ToggleKey, chordsActive, KeyCode.Y))
                _mod.ToggleTracking();

            if (Pressed(HeadTrackingConfig.TrackingModeKey, chordsActive, KeyCode.G))
                _mod.CycleTrackingMode();

            if (Pressed(HeadTrackingConfig.YawModeKey, chordsActive, KeyCode.H))
                _mod.ToggleYawMode();

            if (Pressed(HeadTrackingConfig.ReticleToggleKey, chordsActive, KeyCode.U))
                _mod.ToggleReticle();
        }

        private static bool Pressed(KeyCode primary, bool chordsActive, KeyCode chordLetter)
            => Input.GetKeyDown(primary) || (chordsActive && Input.GetKeyDown(chordLetter));
    }
}
