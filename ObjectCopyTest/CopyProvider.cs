using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ObjectCopyTest
{
    public class CopyProvider : ICopyProvider
    {
        public static ICopyProvider Instance { get; } = new CopyProvider();

        public static void Copy(object source, object target)
        {
            foreach (var propertyInfo in GetPropertyWithAttribute<CopyableAttribute>(target))
            {
                var value = propertyInfo.GetValue(source);
                propertyInfo.SetValue(target, value);
            }
        }

        public Action<TSource, TTarget> CopyAction<TSource, TTarget>()
        {
            return (source, target) => Copy(source, target);
        }

        public Action<T, T> CopyAction<T>()
        {
            return (source, target) => Copy(source, target);
        }

        private static IEnumerable<PropertyInfo> GetPropertyWithAttribute<TAttribute>(object equatableEntity)
            where TAttribute : Attribute
        {
            return equatableEntity
                .GetType()
                .GetInterfaces()
                .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(pi => pi.GetCustomAttributes(typeof(TAttribute), true).Any());
        }

        void ICopyProvider.Copy<T, TU>(T source, TU target)
        {
            Copy(source, target);
        }
    }
}


