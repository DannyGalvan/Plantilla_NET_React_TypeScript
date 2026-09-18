using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Project.Server.Utils
{
    public static class Util
    {
        /// <summary>
        /// Properties that <see cref="UpdateProperties{TDestination,TSource}"/> must never copy
        /// from the request DTO onto the tracked entity. Closes the mass-assignment
        /// privilege escalation (B2): a client sending <c>RolId</c>, <c>State</c>,
        /// <c>Password</c> or <c>OwnerId</c> in the body must not have those fields
        /// applied to the entity.
        /// </summary>
        public static readonly IReadOnlySet<string> MassAssignmentDenylist = new HashSet<string>(StringComparer.Ordinal)
        {
            "Id",
            "CreatedBy",
            "CreatedAt",
            "UpdatedAt",
            "UpdatedBy",
            "State",
            "Password",
            "PasswordHash",
            "RecoveryToken",
            "DateToken",
            "FailedLoginAttempts",
            "LockoutEnd",
            "MustChangePassword",
            "OwnerId",
            "RolId",
            "UserId",
        };

        /// <summary>
        /// Copies scalar, non-deny-listed, non-default-zero properties from
        /// <paramref name="updatedEntity"/> onto <paramref name="existingEntity"/>.
        /// Replaces the original mass-assignment helper that copied
        /// <c>RolId</c>/<c>State</c>/<c>UpdatedBy</c> from the request body.
        /// </summary>
        public static void UpdateProperties<TDestination, TSource>(TDestination existingEntity, TSource updatedEntity)
        {
            if (existingEntity is null) throw new ArgumentNullException(nameof(existingEntity));
            if (updatedEntity is null) throw new ArgumentNullException(nameof(updatedEntity));

            foreach (PropertyInfo property in typeof(TDestination).GetProperties())
            {
                if (MassAssignmentDenylist.Contains(property.Name)) continue;
                if (!property.CanWrite) continue;

                object? updatedValue;
                try { updatedValue = property.GetValue(updatedEntity); }
                catch { continue; }

                if (updatedValue is null) continue;

                // Skip default-zero scalars so an absent field in a PATCH/PUT
                // body does not overwrite the existing value.
                switch (updatedValue)
                {
                    case long l when l == 0L: continue;
                    case int i when i == 0: continue;
                    case decimal m when m == 0M: continue;
                    case short s when s == 0: continue;
                    case byte b when b == 0: continue;
                    case Guid g when g == Guid.Empty: continue;
                    case DateTime dt when dt == default: continue;
                }

                property.SetValue(existingEntity, updatedValue);
            }
        }

        /// <summary>
        /// The HasValidId
        /// </summary>
        public static bool HasValidId<TId>(TId? id)
        {
            if (id == null) return false;
            return id switch
            {
                long longId => longId > 0,
                int intId => intId > 0,
                decimal decimalId => decimalId > 0,
                string stringId => stringId.Length > 0,
                _ => false
            };
        }

        /// <summary>
        /// The SetPropertyValue
        /// </summary>
        public static dynamic? SetPropertyValue(Type objType, string propertyPath, string valueToConvert)
        {
            // Navegar por la ruta anidada (ej: "Empleado.Nombre")
            string[] parts = propertyPath.Split('.');
            Type? currentType = objType;
            PropertyInfo? property = null;

            foreach (var part in parts)
            {
                property = currentType?.GetProperty(part);
                if (property == null)
                {
                    Console.WriteLine($"La propiedad '{part}' no se encontró en el tipo '{currentType?.Name}'");
                    return null;
                }
                currentType = property.PropertyType;
            }

            if (property == null) return null;

            Type targetType = property.PropertyType;
            object? parsedValue = ConvertToType(valueToConvert, targetType);
            return parsedValue;
        }

        /// <summary>
        /// The ConvertToType
        /// </summary>
        public static dynamic? ConvertToType(string value, Type targetType)
        {
            try
            {
                Type? underlyingType = Nullable.GetUnderlyingType(targetType);

                if (underlyingType != null)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return null;
                    }
                }
                else
                {
                    underlyingType = targetType;
                }

                if (underlyingType.IsEnum)
                {
                    return Enum.Parse(underlyingType, value);
                }

                if (underlyingType == typeof(DateTime))
                {
                    string[] formatos = { "yyyy-MM-ddTHH", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-dd" };
                    if (DateTime.TryParseExact(value, formatos, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime fecha))
                    {
                        return fecha;
                    }
                    else
                    {
                        Console.WriteLine($"Error al convertir el valor '{value}' a {targetType.Name}: Formato de fecha no válido.");
                        return null;
                    }
                }

                return Convert.ChangeType(value, underlyingType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al convertir el valor '{value}' a {targetType.Name}: {ex.Message}");
                return null;
            }
        }

        public static bool IsCuiValid(string? cui)
        {
            if (string.IsNullOrWhiteSpace(cui))
                return false;

            cui = cui.Replace(" ", "");

            if (!Regex.IsMatch(cui, @"^\d{13}$"))
                return false;

            var numero = cui.Substring(0, 8);
            if (!int.TryParse(cui.Substring(8, 1), out int verificador))
                return false;

            if (!int.TryParse(cui.Substring(9, 2), out int depto) ||
                !int.TryParse(cui.Substring(11, 2), out int muni))
                return false;

            int[] munisPorDepto = {
                17,  8, 16, 16, 13, 14, 19,  8, 24, 21,  9,
                30, 32, 21,  8, 17, 14,  5, 11, 11,  7, 17
            };

            if (depto == 0 || muni == 0)
                return false;

            if (depto > munisPorDepto.Length)
                return false;

            if (muni > munisPorDepto[depto - 1])
                return false;

            int total = 0;
            for (int i = 0; i < numero.Length; i++)
            {
                int digit = int.Parse(numero[i].ToString());
                total += digit * (i + 2);
            }

            int modulo = total % 11;
            return modulo == verificador;
        }
    }
}