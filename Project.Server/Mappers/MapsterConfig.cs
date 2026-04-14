using Mapster;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;

namespace Project.Server.Mappers
{
    public abstract class MapsterConfig
    {
        public static void RegisterMappings()
        {
            //Mapper User
            TypeAdapterConfig<RegisterRequest, User>.NewConfig()
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.Number, src => src.Number)
                .Map(dest => dest.Password, src => src.Password)
                .Map(dest => dest.IdentificationDocument, src => src.IdentificationDocument)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.UserName, src => src.UserName)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UpdatedAt!);

            // Mapper UserRequest to User
            TypeAdapterConfig<UserRequest, User>.NewConfig()
                .Map(dest => dest.RolId, src => src.RolId)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.UserName, src => src.UserName)
                .Map(dest => dest.Password, src => src.Password)
                .Map(dest => dest.IdentificationDocument, src => src.IdentificationDocument)
                .Map(dest => dest.Number, src => src.Number)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UpdatedAt!);

            TypeAdapterConfig<User, UserResponse>.NewConfig()
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.Number, src => src.Number)
                .Map(dest => dest.IdentificationDocument, src => src.IdentificationDocument)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.UserName, src => src.UserName)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.RolId, src => src.RolId)
                .Map(dest => dest.Rol, src => src.Rol)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.HasValue ? src.UpdatedAt.Value.ToString("dd/MM/yyyy " +
                "HH:mm:ss") : null);

            TypeAdapterConfig<User, AuthResponse>.NewConfig()
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.UserName, src => src.UserName)
                .Map(dest => dest.UserId, src => src.Id)
                .Map(dest => dest.Rol, src => src.RolId)
                .Map(dest => dest.Redirect, src => src.Reset ?? false)
                .Ignore(dest => dest.Token)
                .Ignore(dest => dest.Operations);

            TypeAdapterConfig<User, User>.NewConfig();


            //Mapper Rol
            TypeAdapterConfig<RolRequest, Rol>.NewConfig()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UpdatedAt!);

            TypeAdapterConfig<Rol, RolResponse>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.HasValue ? src.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm:ss") : null)
                .Ignore(dest => dest.Users) // Ignorar navegación circular
                .Ignore(dest => dest.RolOperations); // Ignorar navegación circular

            TypeAdapterConfig<Rol, Rol>.NewConfig();

            //Mapper RolOperation
            TypeAdapterConfig<RolOperationRequest, RolOperation>.NewConfig()
                .Map(dest => dest.OperationId, src => src.OperationId)
                .Map(dest => dest.RolId, src => src.RolId)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UpdatedAt!);

            TypeAdapterConfig<RolOperation, RolOperationResponse>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.OperationId, src => src.OperationId)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.RolId, src => src.RolId)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.HasValue ? src.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm:ss") : null)
                .Ignore(dest => dest.Operation) // Ignorar navegación circular
                .Ignore(dest => dest.Rol); // Ignorar navegación circular

            TypeAdapterConfig<RolOperation, Operation>.NewConfig()
                .Map(dest => dest.Id, src => src.OperationId)
                .Map(dest => dest.Name, src => src.Operation!.Name)
                .Map(dest => dest.Guid, src => src.Operation!.Guid)
                .Map(dest => dest.Description, src => src.Operation!.Description)
                .Map(dest => dest.Policy, src => src.Operation!.Policy)
                .Map(dest => dest.Icon, src => src.Operation!.Icon)
                .Map(dest => dest.Path, src => src.Operation!.Path)
                .Map(dest => dest.ModuleId, src => src.Operation!.ModuleId)
                .Map(dest => dest.IsVisible, src => src.Operation!.IsVisible)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.Operation!.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.Operation!.UpdatedBy)
                .Map(dest => dest.CreatedAt, src => src.Operation!.CreatedAt)
                .Map(dest => dest.UpdatedAt, src => src.Operation!.UpdatedAt)
                .Ignore(dest => dest.RolOperations);

            TypeAdapterConfig<RolOperation, RolOperation>.NewConfig();

            //Mapper Operation
            TypeAdapterConfig<OperationRequest, Operation>.NewConfig()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Guid, src => src.Guid)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Policy, src => src.Policy)
                .Map(dest => dest.Icon, src => src.Icon)
                .Map(dest => dest.Path, src => src.Path)
                .Map(dest => dest.ModuleId, src => src.ModuleId)
                .Map(dest => dest.IsVisible, src => src.IsVisible)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.UpdatedAt!);

            TypeAdapterConfig<Operation, OperationResponse>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Guid, src => src.Guid)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Policy, src => src.Policy)
                .Map(dest => dest.Icon, src => src.Icon)
                .Map(dest => dest.Path, src => src.Path)
                .Map(dest => dest.ModuleId, src => src.ModuleId)
                .Map(dest => dest.IsVisible, src => src.IsVisible)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.HasValue ? src.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm:ss") : null)
                .Ignore(dest => dest.RolOperations) // Ignorar navegación circular
                .Map(dest => dest.Module, src => src.Module);

            TypeAdapterConfig<Operation, Operation>.NewConfig();    

            //Mapper Module
            TypeAdapterConfig<Module, ModuleResponse>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Image, src => src.Image)
                .Map(dest => dest.Path, src => src.Path)
                .Map(dest => dest.State, src => src.State)
                .Map(dest => dest.CreatedBy, src => src.CreatedBy)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.HasValue ? src.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm:ss") : null)
                .Ignore(dest => dest.Operations); // Ignorar navegación circular
        }
    }
}