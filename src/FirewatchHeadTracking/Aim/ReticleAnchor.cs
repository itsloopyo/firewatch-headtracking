using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Repositions Firewatch's HUD reticle (and its sibling interaction text)
    /// so it follows the head-tracked aim point on screen.
    ///
    /// Targets vgHudManager.reticuleParent → walks up two levels to
    /// ReticuleGroup, the common parent of the reticle and TargetName /
    /// TargetDetailGroup labels. Anchored position is written every frame in
    /// canvas-local units (screen-space pixels divided by canvas scale).
    /// </summary>
    internal sealed class ReticleAnchor
    {
        private const int MaxFindRetries = 300;
        private const int RetryEveryNthFrame = 60;

        private readonly Action<string> _log;
        private RectTransform _reticuleGroupRT;
        private float _canvasScale = 1f;
        private int _findRetries;
        private bool _hadReticule;

        public ReticleAnchor(Action<string> log)
        {
            _log = log ?? (_ => { });
        }

        public void UpdatePosition(Vector2 screenOffsetPixels)
        {
            // Re-entry on Unity-null means our cached RectTransform was destroyed
            // (scene transition, HUD reload). Reset the retry budget so we can
            // rediscover instead of being stuck past MaxFindRetries forever.
            bool needsLookup = NullHelper.IsNull(_reticuleGroupRT) || _reticuleGroupRT == null;
            if (needsLookup && _hadReticule)
            {
                _hadReticule = false;
                _findRetries = 0;
                _reticuleGroupRT = null;
            }

            if (needsLookup)
            {
                _findRetries++;
                if (_findRetries > MaxFindRetries) return;
                if (_findRetries % RetryEveryNthFrame != 0) return;
                FindReticuleGroup();
                if (_reticuleGroupRT == null) return;
                _hadReticule = true;
            }

            _reticuleGroupRT.anchoredPosition = new Vector2(
                screenOffsetPixels.x / _canvasScale,
                screenOffsetPixels.y / _canvasScale);
        }

        public void ResetPosition()
        {
            if (NullHelper.NotNull(_reticuleGroupRT) && _reticuleGroupRT != null)
                _reticuleGroupRT.anchoredPosition = Vector2.zero;
        }

        // ReticuleGroup is the common parent of the reticle and all interaction text
        // (TargetName, TargetDetailGroup). Hierarchy: ReticuleGroup → ReticuleParent → ReticuleCanvasGroup.
        private void FindReticuleGroup()
        {
            var hudType = AccessTools.TypeByName("vgHudManager");
            if (NullHelper.IsNull(hudType)) return;

            var hudInstance = UnityEngine.Object.FindObjectOfType(hudType);
            if (NullHelper.IsNull(hudInstance) || hudInstance == null) return;

            FieldInfo field = hudType.GetField("reticuleParent",
                BindingFlags.Public | BindingFlags.Instance);
            if (NullHelper.IsNull(field)) return;

            var reticleComponent = field.GetValue(hudInstance) as Component;
            if (NullHelper.IsNull(reticleComponent) || reticleComponent == null) return;

            Transform reticuleGroup = reticleComponent.transform.parent != null
                ? reticleComponent.transform.parent.parent
                : null;
            if (reticuleGroup == null) return;

            _reticuleGroupRT = reticuleGroup as RectTransform;
            if (NullHelper.IsNull(_reticuleGroupRT) || _reticuleGroupRT == null)
            {
                _reticuleGroupRT = null;
                return;
            }

            var canvas = reticleComponent.GetComponentInParent<Canvas>();
            if (NullHelper.NotNull(canvas) && canvas != null)
                _canvasScale = canvas.scaleFactor;

            _log("[HUD] Found ReticuleGroup: " + reticuleGroup.name +
                " anchoredPos=" + _reticuleGroupRT.anchoredPosition +
                " canvasScale=" + _canvasScale);
        }
    }
}
