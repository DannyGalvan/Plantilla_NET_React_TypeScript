namespace Project.Server.Configs.Extensions
{
    using FluentValidation;
    using Project.Server.Entities.Request;
    using Project.Server.Validations.Auth;
    using Project.Server.Validations.Operation;
    using Project.Server.Validations.Rol;
    using Project.Server.Validations.RolOperation;
    using Project.Server.Validations.User;

    /// <summary>
    /// Defines the <see cref="ValidationsGroup" />
    /// </summary>
    public static class ValidationsGroup
    {
        /// <summary>
        /// The AddValidationsGroup
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddValidationsGroup(this IServiceCollection services)
        {
            //auth validations
            services.AddScoped<IValidator<LoginRequest>, LoginValidations>();
            services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordValidations>();
            services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordValidations>();
            services.AddScoped<IValidator<RecoveryPasswordRequest>, RecoveryPasswordValidations>();
            services.AddScoped<IValidator<RegisterRequest>, RegisterValidations>();

            //user validations
            services.AddKeyedScoped<IValidator<UserRequest>, CreateUserValidation>("Create");
            services.AddKeyedScoped<IValidator<UserRequest>, UpdateUserValidation>("Update");
            services.AddKeyedScoped<IValidator<UserRequest>, PartialUserValidation>("Partial");

            //rol validations
            services.AddKeyedScoped<IValidator<RolRequest>, CreateRolValidation>("Create");
            services.AddKeyedScoped<IValidator<RolRequest>, UpdateRolValidation>("Update");
            services.AddKeyedScoped<IValidator<RolRequest>, PartialRolValidation>("Partial");

            //rolOperation validations
            services.AddKeyedScoped<IValidator<RolOperationRequest>, CreateRolOperationValidation>("Create");
            services.AddKeyedScoped<IValidator<RolOperationRequest>, UpdateRolOperationValidation>("Update");
            services.AddKeyedScoped<IValidator<RolOperationRequest>, PartialRolOperationValidation>("Partial");

            //operation validations
            services.AddKeyedScoped<IValidator<OperationRequest>, CreateOperationValidation>("Create");
            services.AddKeyedScoped<IValidator<OperationRequest>, UpdateOperationValidation>("Update");
            services.AddKeyedScoped<IValidator<OperationRequest>, PartialOperationValidation>("Partial");

            return services;
        }

    }

}
