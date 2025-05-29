namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Defines the <see cref="ISendMail" />
    /// </summary>
    public interface ISendMail
    {
        /// <summary>
        /// The Send
        /// </summary>
        /// <param name="correo">The correo<see cref="string"/></param>
        /// <param name="asunto">The asunto<see cref="string"/></param>
        /// <param name="mensaje">The mensaje<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public bool Send(string correo, string asunto, string mensaje);
    }
}
