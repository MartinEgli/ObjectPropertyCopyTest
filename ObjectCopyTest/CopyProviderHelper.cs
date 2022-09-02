using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ObjectCopyTest
{

    public static class PropertyInfoExtensions
    {
        //public static IEnumerable<PropertyInfo> GetPropertyWithAttribute<TAttribute>(this object target)
        //    where TAttribute : Attribute
        //{
        //    return target.GetType().GetPropertyWithAttribute<TAttribute>();
        //}

        public static IEnumerable<PropertyInfo> GetPropertyWithAttribute<TAttribute>(this Type type)
            where TAttribute : Attribute
        {
            if (!type.IsInterface)
            {
                var interfaces = type.GetInterfaces();
                var properties = interfaces.SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public));
                var attributedProperties = properties.Where(pi => pi.GetCustomAttributes(typeof(TAttribute), true).Any());
                return attributedProperties;
            }
            else
            {
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                var attributedProperties = properties.Where(pi => pi.GetCustomAttributes(typeof(TAttribute), true).Any());
                return attributedProperties;
            }
            

        }

        public static IEnumerable<PropertyInfo> GetPropertyWithAttribute(this Type type, Attribute attribute)
        {
            return type
                .GetInterfaces()
                .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(pi => pi.GetCustomAttributes(attribute.GetType(), true).Any());
        }
    }

    public static class CopyProviderHelper
    {
        public static string GetClassName(Type sourceType,
            Type targetType)

        {
            if (sourceType == null) throw new ArgumentNullException(nameof(sourceType));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            var className = "Copy_";

            className += sourceType.FullName.Replace(".", "_");

            className += "_";

            className += targetType.FullName.Replace(".", "_");

            return className;
        }

        

        public static IList<CopyPropertyMapProvider.PropertyMap> GetMatchingProperties(Type sourceType, Type targetType)

        {
            var sourceProperties = sourceType.GetProperties();
            var targetProperties = targetType.GetProperties();

            var properties = (from s in sourceProperties
                from t in targetProperties
                where s.Name == t.Name &&
                      s.CanRead &&
                      t.CanWrite &&
                      s.PropertyType.IsPublic &&
                      t.PropertyType.IsPublic &&
                      s.PropertyType == t.PropertyType &&
                      (
                          s.PropertyType.IsValueType &&
                          t.PropertyType.IsValueType ||
                          s.PropertyType == typeof(string) &&
                          t.PropertyType == typeof(string)
                      )
                select new CopyPropertyMapProvider.PropertyMap

                {
                    SourceProperty = s,

                    TargetProperty = t
                }).ToList();

            return properties;
        }
    }
}