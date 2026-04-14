using System.Reflection;
using System.Text.RegularExpressions;

namespace Project.Server.Utils
{
    /// <summary>
    /// Defines the <see cref="Util" />
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// The UpdateProperties
        /// </summary>
        /// <typeparam name="TDestination"></typeparam>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="existingEntity">The existingEntity<see cref="TDestination"/></param>
        /// <param name="updatedEntity">The updatedEntity<see cref="TSource"/></param>
        public static void UpdateProperties<TDestination, TSource>(TDestination existingEntity, TSource updatedEntity)
        {
            foreach (PropertyInfo property in typeof(TDestination).GetProperties())
            {
                //object? existingValue = property.GetValue(existingEntity);
                object? updatedValue = property.GetValue(updatedEntity);

                switch (property.Name)
                {
                    case "CreatedAt" or "OrderDate" or "DeliveryDate":
                        continue;
                    case "Id":
                        continue;
                    case "CreatedBy":
                        continue;
                    case "Password":
                        continue;
                }

                if (updatedValue == null || !property.CanWrite) continue;
                switch (updatedValue)
                {
                    case long when long.Parse(updatedValue.ToString()!) == 0L:
                    case decimal when decimal.Parse(updatedValue.ToString()!) == 0M:
                    case Dictionary<string, decimal> { Count: 0 }:
                    case int when int.Parse(updatedValue.ToString()!) == 0 && property.Name == "OrdersQuantity":
                        continue;
                    default:
                        property.SetValue(existingEntity, updatedValue);
                        break;
                }
            }
        }

        /// <summary>
        /// The HasValidId
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="id">The id<see cref="TId?"/></param>
        /// <returns>The <see cref="bool"/></returns>
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
        /// <param name="objType">The objType<see cref="Type"/></param>
        /// <param name="propertyPath">The propertyName<see cref="string"/></param>
        /// <param name="valueToConvert">The valueToConvert<see cref="string"/></param>
        /// <returns>The <see>
        ///         <cref>dynamic?</cref>
        ///     </see>
        /// </returns>
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

            // Obtener el tipo final
            Type targetType = property.PropertyType;

            // Convertir el valor
            object? parsedValue = ConvertToType(valueToConvert, targetType);
            return parsedValue;
        }

        /// <summary>
        /// The ConvertToType
        /// </summary>
        /// <param name="value">The value<see cref="string"/></param>
        /// <param name="targetType">The targetType<see cref="Type"/></param>
        /// <returns>The <see cref="dynamic?"/></returns>
        public static dynamic? ConvertToType(string value, Type targetType)
        {
            try
            {
                // Si el tipo es Nullable, obtener el tipo subyacente
                Type? underlyingType = Nullable.GetUnderlyingType(targetType);

                // Si es Nullable y el valor es nulo o vacío, devolver null
                if (underlyingType != null)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return null; // Para tipos nullable, devolver null si el valor es vacío o nulo
                    }
                }
                else
                {
                    underlyingType = targetType; // Si no es nullable, usar el tipo original
                }

                // Para tipos Enum
                if (underlyingType.IsEnum)
                {
                    return Enum.Parse(underlyingType, value);
                }

                // Para DateTime con un formato específico
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

                // Convertir el valor utilizando Convert.ChangeType
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

            // Remover espacios
            cui = cui.Replace(" ", "");

            // Validar formato general
            if (!Regex.IsMatch(cui, @"^\d{13}$"))
                return false;

            // Extraer partes
            var numero = cui.Substring(0, 8);
            if (!int.TryParse(cui.Substring(8, 1), out int verificador))
                return false;

            if (!int.TryParse(cui.Substring(9, 2), out int depto) ||
                !int.TryParse(cui.Substring(11, 2), out int muni))
                return false;

            // Validar código de municipio y departamento
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

            // Validar dígito verificador (módulo 11)
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