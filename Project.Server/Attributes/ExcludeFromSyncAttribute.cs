namespace Project.Server.Attributes
{
    /// <summary>
    /// Atributo para excluir un controlador o acción de la sincronización automática
    /// Útil para endpoints públicos como Login, Register, etc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ExcludeFromSyncAttribute : Attribute
    {
    }
}
