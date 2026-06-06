using CameraUnlock.Core.Unity.Extensions;
using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Polls per-frame hotkey state and dispatches to the mod's command methods.
    /// Each binding has both a configurable primary key and a Ctrl+Shift+letter
    /// chord alternative (see ChordHotkeys) for keyboards without a nav cluster.
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
            // vs. 10+ GetKey/GetKeyDown calls otherwise.
            if (!Input.anyKeyDown) return;

            if (ChordHotkeys.IsActionPressed(HeadTrackingConfig.RecenterKey, ChordHotkeys.RecenterLetter))
                _mod.Recenter();

            if (ChordHotkeys.IsActionPressed(HeadTrackingConfig.ToggleKey, ChordHotkeys.ToggleLetter))
                _mod.ToggleTracking();

            if (ChordHotkeys.IsActionPressed(HeadTrackingConfig.TrackingModeKey, ChordHotkeys.PositionLetter))
                _mod.CycleTrackingMode();

            if (ChordHotkeys.IsActionPressed(HeadTrackingConfig.YawModeKey, ChordHotkeys.FourthToggleLetter))
                _mod.ToggleYawMode();

            if (ChordHotkeys.IsActionPressed(HeadTrackingConfig.ReticleToggleKey, ChordHotkeys.FifthToggleLetter))
                _mod.ToggleReticle();
        }
    }
}
