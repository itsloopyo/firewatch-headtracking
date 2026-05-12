namespace FirewatchHeadTracking.Patches
{
    /// <summary>
    /// Harmony prefix/postfix on vgCameraController.LateUpdate.
    ///
    /// This is the core look/aim decoupling mechanism (same pattern as Green-Hell):
    ///   Prefix:  ResetWorldToCameraMatrix — game's LateUpdate sees clean camera
    ///   Game:    vgCameraController.LateUpdate positions and rotates the camera normally
    ///   Postfix: ApplyTracking — head tracking applied to worldToCameraMatrix for rendering
    ///
    /// Between frames, the matrix stays in tracked mode. But all game code that matters
    /// (vgPlayerTargeting, vgCameraController) uses camera.transform — never the matrix directly.
    /// The prefix reset ensures the game's own LateUpdate camera positioning is clean.
    /// </summary>
    internal static class CameraControllerPatch
    {
        public static void LateUpdatePrefix()
        {
            FirewatchHeadTrackingMod.Instance?.RemoveTrackingOffset();
        }

        public static void LateUpdatePostfix()
        {
            FirewatchHeadTrackingMod.Instance?.ApplyHeadTracking();
        }
    }
}
