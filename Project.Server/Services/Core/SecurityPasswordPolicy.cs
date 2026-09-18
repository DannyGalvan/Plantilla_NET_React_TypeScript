using System.Text.RegularExpressions;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Single source of truth for password complexity. Used by
    /// <c>AuthService</c> and every FluentValidation validator that needs to
    /// express "the password is complex enough".
    /// </summary>
    public sealed class SecurityPasswordPolicy
    {
        public int MinimumLength { get; init; } = 8;
        public int MaximumLength { get; init; } = 128;
        public bool RequireUppercase { get; init; } = true;
        public bool RequireLowercase { get; init; } = true;
        public bool RequireDigit { get; init; } = true;
        public bool RequireSpecialCharacter { get; init; } = true;
        public int PasswordHistoryLimit { get; init; } = 5;

        private static readonly Regex UpperRx = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex LowerRx = new(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex DigitRx = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex SpecialRx = new(@"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\;/`~]", RegexOptions.Compiled);

        /// <summary>
        /// Validates a password against the policy. Returns <c>true</c> and an
        /// empty message when the password is acceptable; otherwise <c>false</c>
        /// with a human-readable reason.
        /// </summary>
        public bool Validate(string? password, out string message)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                message = "Password is required.";
                return false;
            }

            if (password.Length < MinimumLength)
            {
                message = $"Password must be at least {MinimumLength} characters.";
                return false;
            }

            if (password.Length > MaximumLength)
            {
                message = $"Password must be at most {MaximumLength} characters.";
                return false;
            }

            if (RequireUppercase && !UpperRx.IsMatch(password))
            {
                message = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (RequireLowercase && !LowerRx.IsMatch(password))
            {
                message = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (RequireDigit && !DigitRx.IsMatch(password))
            {
                message = "Password must contain at least one digit.";
                return false;
            }

            if (RequireSpecialCharacter && !SpecialRx.IsMatch(password))
            {
                message = "Password must contain at least one special character.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}