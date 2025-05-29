namespace Project.Server.Entities.Response
{
    public class UserResponse
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the RolId
        /// </summary>
        public long RolId { get; set; }


        /// <summary>
        /// Gets or sets the MunicipalityCode
        /// </summary>
        public string MunicipalityCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MunicipalityCode
        /// </summary>
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IdentificationDocument
        /// </summary>
        public string IdentificationDocument { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Number
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the State
        /// </summary>
        public int State { get; set; } = 1;

        /// <summary>
        /// Gets or sets the CreatedAt
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UpdatedAt
        /// </summary>
        public string? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the UpdatedBy
        /// </summary>
        public long? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the Rol
        /// </summary>
        public virtual RolResponse? Rol { get; set; }

        /// <summary>
        /// Gets or sets the Municipality
        /// </summary>
        public virtual MunicipalityResponse? Municipality { get; set; }
    }
}
