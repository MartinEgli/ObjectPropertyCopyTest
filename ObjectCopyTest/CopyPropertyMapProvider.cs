using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ObjectCopyTest
{
    public class CopyPropertyMapProvider : ICopyProvider
    {

        public static CopyPropertyMapProvider Instance { get; } = new CopyPropertyMapProvider();

        private readonly Dictionary<string, PropertyMap[]> _maps =
            new Dictionary<string, PropertyMap[]>();
        

        public void AddPropertyMap<T, TU>()

        {
            var props = CopyProviderHelper.GetMatchingProperties(typeof(T), typeof(TU));

            var className = CopyProviderHelper.GetClassName(typeof(T), typeof(TU));

            _maps.Add(className, props.ToArray());
        }

        public PropertyMap[] AddPropertyMap(object type) => AddPropertyMap(type, type);

        public PropertyMap[] AddPropertyMap(object source, object target)

        {
            var props = CopyProviderHelper.GetMatchingProperties(source.GetType(), target.GetType());
            var className = CopyProviderHelper.GetClassName(source.GetType(), target.GetType());
            var array = props.ToArray();
            _maps.Add(className, array);
            return array;
        }

        public void Copy<T, TU>(T source, TU target)
        {
            var className = CopyProviderHelper.GetClassName(source.GetType(), target.GetType());
            if (!(_maps.TryGetValue(className, out var propMap)))
            {
                propMap = AddPropertyMap(source,target);
            }
            
            foreach (var prop in propMap)
            {
                var sourceValue = prop.SourceProperty.GetValue(source, null);
                prop.TargetProperty.SetValue(target, sourceValue, null);
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

        public class PropertyMap

        {
            public PropertyInfo SourceProperty { get; set; }

            public PropertyInfo TargetProperty { get; set; }
        }
    }
}