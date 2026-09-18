namespace Project.Server.Utils
{
    /// <summary>
    /// Marks an interceptor with a numeric priority so the entity service can
    /// execute registered interceptors in a deterministic order. Lower values
    /// run first.
    /// </summary>
    /// <remarks>
    /// Sealed by design: the original <c>abstract</c> declaration could never
    /// be applied, so priority was effectively ignored.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OrderAttribute : Attribute
    {
        public int Priority { get; }

        public OrderAttribute(int priority)
        {
            if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
            Priority = priority;
        }
    }
}