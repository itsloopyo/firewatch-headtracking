using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace FirewatchHeadTracking
{
    /// <summary>
    /// Centralized, search-once-cache-forever resolver for Firewatch game types
    /// accessed via reflection. Lookup is lazy and runs at most once per
    /// resolved member.
    /// </summary>
    internal static class GameTypeResolver
    {
        private static bool _searched;
        private static Func<bool> _isPausedGetter;

        /// <summary>
        /// Direct-invoke delegate for vgPauseManager.isPaused. Avoids per-frame
        /// PropertyInfo.GetValue boxing + params-array allocation on the hot
        /// path. Null when the property could not be resolved.
        /// </summary>
        internal static Func<bool> IsPausedGetter
        {
            get
            {
                if (!_searched)
                {
                    _searched = true;
                    _isPausedGetter = ResolveIsPausedGetter();
                }
                return _isPausedGetter;
            }
        }

        private static Func<bool> ResolveIsPausedGetter()
        {
            var pauseManagerType = AccessTools.TypeByName("vgPauseManager");
            if (NullHelper.IsNull(pauseManagerType))
            {
                MelonLogger.Msg("[GameTypeResolver] vgPauseManager type NOT found");
                return null;
            }

            var prop =
                pauseManagerType.GetProperty("isPaused", BindingFlags.Public | BindingFlags.Static)
                ?? pauseManagerType.GetProperty("isPaused", BindingFlags.NonPublic | BindingFlags.Static);

            if (NullHelper.IsNull(prop))
            {
                MelonLogger.Msg("[GameTypeResolver] vgPauseManager.isPaused property NOT found");
                return null;
            }

            var getter = prop.GetGetMethod(true);
            if (NullHelper.IsNull(getter) || !getter.IsStatic || getter.ReturnType != typeof(bool))
                return null;

            try
            {
                return (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), getter);
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("[GameTypeResolver] Failed to bind isPaused delegate: " + ex.Message);
                return null;
            }
        }
    }
}
