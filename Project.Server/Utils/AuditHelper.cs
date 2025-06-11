using Project.Server.Entities.Interfaces;

namespace Project.Server.Utils
{
    using System.Reflection;

    public static class AuditHelper
    {
        public static void SetCreatedByRecursive(object obj, long userId)
        {
            if (obj == null) return;

            var type = obj.GetType();

            if (ImplementsIRequest(type))
            {
                var createdByProp = type.GetProperty("CreatedBy");
                if (createdByProp?.CanWrite == true)
                {
                    createdByProp.SetValue(obj, userId);
                }
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 🚫 Saltar propiedades indexadas
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var value = prop.GetValue(obj);
                if (value == null) continue;

                if (value is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                    {
                        if (item != null)
                            SetCreatedByRecursive(item, userId);
                    }
                }
                else
                {
                    SetCreatedByRecursive(value, userId);
                }
            }
        }


        public static void SetUpdatedByRecursive(object? obj, long userId)
        {
            if (obj == null) return;

            var type = obj.GetType();

            if (ImplementsIRequest(type))
            {
                var createdByProp = type.GetProperty("UpdatedBy");
                if (createdByProp?.CanWrite == true)
                {
                    createdByProp.SetValue(obj, userId);
                }
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 🚫 Saltar propiedades indexadas
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var value = prop.GetValue(obj);
                if (value == null) continue;

                if (value is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                    {
                        if (item != null)
                            SetUpdatedByRecursive(item, userId);
                    }
                }
                else
                {
                    SetUpdatedByRecursive(value, userId);
                }
            }
        }

        private static bool ImplementsIRequest(Type type)
        {
            return type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
        }
    }

}