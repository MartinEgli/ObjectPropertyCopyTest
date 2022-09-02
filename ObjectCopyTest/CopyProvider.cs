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
            foreach (var propertyInfo in PropertyInfoExtensions.GetPropertyWithAttribute<CopyableAttribute>(target.GetType()))
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

      

        void ICopyProvider.Copy<T, TU>(T source, TU target)
        {
            Copy(source, target);
        }
    }
}


