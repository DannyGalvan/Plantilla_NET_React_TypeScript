namespace Project.SharedKernel.Domain;

/// <summary>The change history (Audit module) skips this entity.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class NotAuditedAttribute : Attribute;

/// <summary>
/// The change history never records this property. Properties whose name contains Password, Secret, Token,
/// Credential or Hash are skipped even without the attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute;
