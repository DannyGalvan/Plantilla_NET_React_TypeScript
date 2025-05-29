namespace Project.Server.Entities.Response
{
    public class Authorizations
    {
        public ModuleResponse?                Module     { get; set; }
        public ICollection<OperationResponse> Operations { get; set; } = new List<OperationResponse>();
    }
}
