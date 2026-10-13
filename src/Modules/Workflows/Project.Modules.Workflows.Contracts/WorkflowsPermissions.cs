namespace Project.Modules.Workflows.Contracts;

/// <summary>Permission keys of the Workflows module. Approvers act through the approvers of each step.</summary>
public static class WorkflowsPermissions
{
    public const string Module = "workflows";

    public static class Definitions
    {
        public const string Manage = "workflows.definitions.manage";
    }

    /// <summary>See every request of the tenant (auditors, administrators).</summary>
    public static class Requests
    {
        public const string ViewAll = "workflows.requests.view-all";
    }
}
