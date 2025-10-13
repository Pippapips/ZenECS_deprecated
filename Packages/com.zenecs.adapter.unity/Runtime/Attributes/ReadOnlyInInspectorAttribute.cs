namespace ZenECS.Adapter.Unity.Attributes
{
    [System.AttributeUsage(
        System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Class,
        Inherited = true, AllowMultiple = false)]
    public sealed class ReadOnlyInInspectorAttribute : System.Attribute
    {
        public ReadOnlyInInspectorAttribute()
        {
        }
    }
}