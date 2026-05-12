namespace FirewatchHeadTracking
{
    /// <summary>
    /// Null check helper for old Mono compatibility.
    /// Unity's old Mono runtime lacks certain null operators, so we use ReferenceEquals.
    ///
    /// <para><b>When to use each pattern:</b></para>
    /// <list type="bullet">
    ///   <item><c>NullHelper.IsNull(x)</c> — for plain .NET objects (Type, FieldInfo, etc.)
    ///   where Unity's overloaded == is not involved.</item>
    ///   <item><c>x == null</c> — for Unity objects (Component, GameObject, etc.)
    ///   where you need destroyed-object detection (Unity overloads == to return true
    ///   for destroyed objects that aren't yet GC'd).</item>
    ///   <item><c>NullHelper.IsNull(x) || x == null</c> — the dual-check pattern for
    ///   objects of unknown origin that might be either a plain .NET null or a destroyed
    ///   Unity object. The first check catches true nulls without invoking Unity's ==
    ///   operator; the second catches destroyed Unity objects.</item>
    /// </list>
    /// </summary>
    internal static class NullHelper
    {
        internal static bool IsNull(object obj) => ReferenceEquals(obj, null);

        internal static bool NotNull(object obj) => !ReferenceEquals(obj, null);
    }
}
