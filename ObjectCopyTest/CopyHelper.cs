using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace ObjectCopyTest
{
    public static class CopyHelper
    {
        private static readonly Dictionary<string, Type> Comp = new Dictionary<string, Type>();

        private static readonly Dictionary<string, PropertyMap[]> Maps =
            new Dictionary<string, PropertyMap[]>();

        public static void CopyContent(this ITestObject source, ITestObject target)
        {
            target.Number1 = source.Number1;
            target.Number2 = source.Number2;
            target.Number3 = source.Number3;
            target.Number4 = source.Number4;
            target.Number5 = source.Number5;
            target.Text1 = source.Text1;
            target.Text2 = source.Text2;
            target.Text3 = source.Text3;
            target.Text4 = source.Text4;
            target.Text5 = source.Text5;
        }

        public static void CopyContentByAttribute(this object source, object target)
        {
            foreach (var propertyInfo in target.GetPropertyWithAttribute<CopyableAttribute>())
            {
                var value = propertyInfo.GetValue(source);
                propertyInfo.SetValue(target, value);
            }
        }

        public static void CopyContentByBufferdAttribute(this object source, object target,
            IEnumerable<PropertyInfo> propertyInfos)
        {
            foreach (var propertyInfo in propertyInfos)
            {
                var value = propertyInfo.GetValue(source);
                propertyInfo.SetValue(target, value);
            }
        }

        internal static IEnumerable<PropertyInfo> GetPropertyWithAttribute<TAttribute>(this object equatableEntity)
            where TAttribute : Attribute
        {
            return equatableEntity
                .GetType()
                .GetInterfaces()
                .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(pi => pi.GetCustomAttributes(typeof(TAttribute), true).Any());
        }

        public static void CopyWithDom<T, TU>(T source, TU target)

        {
            var className = GetClassName(typeof(T), typeof(TU));

            var flags = BindingFlags.Public | BindingFlags.Static |
                        BindingFlags.InvokeMethod;

            var args = new object[] {source, target};
            var comp = Comp[className];
            comp.InvokeMember("CopyProps", flags, null,
                null, args);
        }

        public static Action<T, TU> ActionCopyWithDom<T, TU>()

        {
            var className = GetClassName(typeof(T), typeof(TU));

            var flags = BindingFlags.Public | BindingFlags.Static |
                        BindingFlags.InvokeMethod;

           // var args = new object[] { source, target };
            var comp = Comp[className];
            Action<T, TU> result;
            result = comp.InvokeMember("CopyPropsAction", flags, null,
                null, null) as Action<T, TU>;
             return result;
        }

        public static void AddPropertyMap<T, TU>()

        {
            var props = GetMatchingProperties(typeof(T), typeof(TU));

            var className = GetClassName(typeof(T), typeof(TU));

            Maps.Add(className, props.ToArray());
        }

        public static void CopyMatchingCachedProperties(object source,
            object target)

        {
            var className = GetClassName(source.GetType(),
                target.GetType());

            var propMap = Maps[className];

            for (var i = 0; i < propMap.Length; i++)

            {
                var prop = propMap[i];

                var sourceValue = prop.SourceProperty.GetValue(source,
                    null);

                prop.TargetProperty.SetValue(target, sourceValue, null);
            }
        }

        public static string GetClassName(Type sourceType,
            Type targetType)

        {
            var className = "Copy_";

            className += sourceType.FullName.Replace(".", "_");

            className += "_";

            className += targetType.FullName.Replace(".", "_");

            return className;
        }

        public static IList<PropertyMap> GetMatchingProperties(Type sourceType, Type targetType)

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
                select new PropertyMap

                {
                    SourceProperty = s,

                    TargetProperty = t
                }).ToList();

            return properties;
        }

        public static void GenerateCopyClass<T, TU>()

        {
            var sourceType = typeof(T);

            var targetType = typeof(TU);

            var className = GetClassName(typeof(T), typeof(TU));

            if (Comp.ContainsKey(className))

                return;

            var builder = new StringBuilder();

            builder.Append("namespace Copy {\r\n");

            builder.Append("    using System;\r\n");
            
            builder.Append("    public class ");

            builder.Append(className);

            builder.Append(" {\r\n");

            builder.Append("        public static Action<" + sourceType.FullName + ", " + targetType.FullName + "> CopyPropsAction()");

            builder.Append("        {\r\n");

            builder.Append("        return CopyProps;");


            builder.Append("        }\r\n");

            builder.Append("        public static void CopyProps(");

            builder.Append(sourceType.FullName);

            builder.Append(" source, ");

            builder.Append(targetType.FullName);

            builder.Append(" target) {\r\n");

            var map = GetMatchingProperties(sourceType, targetType);

            foreach (var item in map)

            {
                builder.Append("            target.");

                builder.Append(item.TargetProperty.Name);

                builder.Append(" = ");

                builder.Append("source.");

                builder.Append(item.SourceProperty.Name);

                builder.Append(";\r\n");
            }

            builder.Append("        }\r\n   }\r\n}");

            // Write out method body

            Debug.WriteLine(builder.ToString());

            var myCodeProvider = new CSharpCodeProvider();

            var myCodeCompiler = myCodeProvider.CreateCompiler();

            var myCompilerParameters = new CompilerParameters();

            myCompilerParameters.ReferencedAssemblies.Add(
                typeof(TestObject).Assembly.Location
            );


            myCompilerParameters.GenerateInMemory = true;

            var results = myCodeCompiler.CompileAssemblyFromSource
                (myCompilerParameters, builder.ToString());

            // Compiler output

            foreach (var line in results.Output)

                Debug.WriteLine(line);

            var copierType = results.CompiledAssembly.GetType(
                "Copy." + className);

            Comp.Add(className, copierType);
        }

        public class PropertyMap

        {
            public PropertyInfo SourceProperty { get; set; }

            public PropertyInfo TargetProperty { get; set; }
        }
    }
}