using Project.Server.Entities.Response;

namespace Project.Server.Services.Interfaces
{
    public interface IResources
    {
        public Response<string> SaveImageToSignature(string? base64File, string previousValue);
    }
}
