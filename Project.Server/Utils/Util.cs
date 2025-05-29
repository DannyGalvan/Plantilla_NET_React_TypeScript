using System.Reflection;

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
        /// <param name="propertyName">The propertyName<see cref="string"/></param>
        /// <param name="valueToConvert">The valueToConvert<see cref="string"/></param>
        /// <returns>The <see cref="dynamic?"/></returns>
        public static dynamic? SetPropertyValue(Type objType, string propertyName, string valueToConvert)
        {
            // Obtener la propiedad a través de reflexión
            PropertyInfo? property = objType.GetProperty(propertyName);
            if (property == null)
            {
                Console.WriteLine($"La propiedad {propertyName} no se encontró en el tipo {objType.Name}");
                return null;
            }

            // Obtener el tipo de la propiedad
            Type propertyType = property.PropertyType;

            // Convertir el valor de string al tipo de la propiedad
            object? parsedValue = ConvertToType(valueToConvert, propertyType);

            // Asignar el valor convertido a la propiedad
            if (parsedValue != null)
            {
                return parsedValue;
            }

            return null;
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

                // Convertir el valor utilizando Convert.ChangeType
                return Convert.ChangeType(value, underlyingType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al convertir el valor '{value}' a {targetType.Name}: {ex.Message}");
                return null;
            }
        }
    }
}
