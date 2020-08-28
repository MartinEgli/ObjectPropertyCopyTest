using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ObjectCopyTest
{
    public class CopyCacheProvider : ICopyProvider
    {
        private readonly IDictionary<Type,IDictionary<Type, IEnumerable<PropertyInfo>>> _dictionary = new ConcurrentDictionary<Type, IDictionary<Type, IEnumerable<PropertyInfo>>>();

        public static CopyCacheProvider Instance { get; } = new CopyCacheProvider();

        public void Copy(object source, object target)
        {
            var properties = GetPropertyInfos(source, target);

            Copy(source, target, properties);
        }

        public Action<TSource, TTarget> CopyAction<TSource, TTarget>()
        {
            return (source, target) => Copy(source, target);
        }

        public Action<T, T> CopyAction<T>()
        {
            return (source, target) => Copy(source, target);
        }

        public static IEnumerable<PropertyInfo> GetPropertyInfos( object target) => GetPropertyWithAttribute<CopyableAttribute>(target);

        public Action<TSource, TTarget> GetCopyAction<TSource, TTarget>(TTarget obj) => GetCopyAction<TSource, TTarget>(GetPropertyInfos(obj).ToList());

        public Action<TSource, TTarget> GetCopyAction<TSource, TTarget>(IEnumerable<PropertyInfo> propertyInfos) => (s, t) => Copy(s, t, propertyInfos);

        private IEnumerable<PropertyInfo> GetPropertyInfos(object source, object target)
        {
            IEnumerable<PropertyInfo> properties;

            if (!(_dictionary.TryGetValue(target.GetType(), out var sources)))
            {
                var s = new Dictionary<Type, IEnumerable<PropertyInfo>>();
                properties = GetPropertyWithAttribute<CopyableAttribute>(target);
                s.Add(source.GetType(), properties);
                _dictionary.Add(target.GetType(), s);
            }

            else if (!(sources.TryGetValue(source.GetType(), out properties)))
            {
                properties = GetPropertyWithAttribute<CopyableAttribute>(target);
                sources.Add(source.GetType(), properties);
            }

            return properties;
        }


        public void Copy(object source, object target, IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(source);
                propertyInfo.SetValue(target, value);
            }
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