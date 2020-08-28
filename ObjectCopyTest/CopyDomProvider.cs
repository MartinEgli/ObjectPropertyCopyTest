using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace ObjectCopyTest
{
    public class CopyDomProvider : ICopyProvider
    {
        private readonly Dictionary<string, Type> Comp = new Dictionary<string, Type>();
        private readonly Dictionary<string, object> actions = new Dictionary<string, object>();
        public static CopyDomProvider Instance { get; } = new CopyDomProvider();

        public Action<T, TU> CopyAction<T, TU>()

        {
            var key = typeof(T).FullName + typeof(TU).FullName;
            if (actions.TryGetValue(key, out var action))
            {
                return action as Action<T, TU>;
            }
            var className = CopyProviderHelper.GetClassName(typeof(T), typeof(TU));

            var flags = BindingFlags.Public | BindingFlags.Static |
                        BindingFlags.InvokeMethod;

            if (!Comp.TryGetValue(className, out var comp))
            {
                comp = GenerateCopyClass(typeof(T), typeof(TU));
            }
            // comp = GenerateCopyClass(typeof(T), typeof(TU));
            var result = comp.InvokeMember("CopyPropsAction", flags, null,
                null, null) as Action<T, TU>;

            actions.Add(key, result);
            return result;
        }

        
        public Action<T, T> CopyAction<T>()

        {
            var key = typeof(T).FullName;
            if (actions.TryGetValue(key, out var action))
            {
                return action as Action<T, T>;
            }
            var className = CopyProviderHelper.GetClassName(typeof(T), typeof(T));

            var flags = BindingFlags.Public | BindingFlags.Static |
                        BindingFlags.InvokeMethod;

            if (!Comp.TryGetValue(className, out var comp))
            {
                comp = GenerateCopyClass(typeof(T), typeof(T));
            }
            // comp = GenerateCopyClass(typeof(T), typeof(TU));
            var result = comp.InvokeMember("CopyPropsAction", flags, null,
                null, null) as Action<T, T>;

            actions.Add(key, result);
            return result;
        }

        public void Copy<T, TU>(T source, TU target)
        {
            CopyAction<T, TU>()(source, target);
        }

        public void CopyWithDom<T, TU>(T source, TU target)

        {
            var className = CopyProviderHelper.GetClassName(typeof(T), typeof(TU));

            var flags = BindingFlags.Public | BindingFlags.Static |
                        BindingFlags.InvokeMethod;

            var args = new object[] { source, target };
            if (!Comp.TryGetValue(className, out var comp))
                comp = GenerateCopyClass(source.GetType(), target.GetType());
            // comp = GenerateCopyClass(typeof(T), typeof(TU));

            comp.InvokeMember("CopyProps", flags, null, null, args);
        }

        public Type GenerateCopyClass<T, TU>()

        {
            var sourceType = typeof(T);

            var targetType = typeof(TU);
            return GenerateCopyClass(sourceType, targetType);
        }

        public Type GenerateCopyClass(Type sourceType, Type targetType)

        {
            var className = CopyProviderHelper.GetClassName(sourceType, targetType);

            if (Comp.TryGetValue(className, out var type)) return type;

            var builder = new StringBuilder();

            builder.Append("namespace Copy {\r\n");

            builder.Append("    using System;\r\n");

            builder.Append("    public class ");

            builder.Append(className);

            builder.Append("   {\r\n");

            builder.Append("        public static Action<" + sourceType.FullName + ", " + targetType.FullName +
                           "> CopyPropsAction()");

            builder.Append("        {\r\n");

            builder.Append("            return CopyProps;");

            builder.Append("        }\r\n");

            builder.Append("        public static void CopyProps(");

            builder.Append(sourceType.FullName);

            builder.Append(" source, ");

            builder.Append(targetType.FullName);

            builder.Append(" target) {\r\n");

            var map = CopyProviderHelper.GetMatchingProperties(sourceType, targetType);

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

            var codeCompiler = CodeDomProvider.CreateProvider("CSharp");

            var compilerParameters = new CompilerParameters();

            compilerParameters.ReferencedAssemblies.Add(typeof(TestObject).Assembly.Location);

            compilerParameters.GenerateInMemory = true;

            var results = codeCompiler.CompileAssemblyFromSource(compilerParameters, builder.ToString());

            // Compiler output

            foreach (var line in results.Output) Debug.WriteLine(line);

            var copierType = results.CompiledAssembly.GetType("Copy." + className);

            Comp.Add(className, copierType);

            return copierType;
        }
    }
}