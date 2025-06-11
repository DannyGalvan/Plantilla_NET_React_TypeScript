namespace Project.Server.Utils
{
    /// <summary>
    /// Defines the <see cref="OrderAttribute" />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class OrderAttribute(int priority) : Attribute
    {
        /// <summary>
        /// Gets the Priority
        /// </summary>
        public int Priority { get; } = priority;
    }
}