using System;
using System.Collections.Generic;
using System.Linq;

namespace ObjectCopyTest
{
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