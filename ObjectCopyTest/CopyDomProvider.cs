using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace ObjectCopyTest
{
    public class CopyDomProvider : ICopyProvider
    {
        private const string ClassName = "Copy";
        private const string CopyActionMethodName = "CopyAction";
        private const string CopyMethodName = "Copy";
        private const string NamespaceName = "CopyHelper";

        private readonly Dictionary<string, object> _actions = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> _comp = new Dictionary<string, Type>();

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static CopyDomProvider Instance { get; } = new CopyDomProvider();

        /// <summary>
        /// Copies the specified source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU">The type of the u.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public void Copy<T, TU>(T source, TU target)
        {
            CopyAction<T, TU>()(source, target);
        }

        /// <summary>
        /// Copies the action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU">The type of the u.</typeparam>
        /// <returns></returns>
        public Action<T, TU> CopyAction<T, TU>()

        {
            var key = typeof(T).FullName + typeof(TU).FullName;
            if (_actions.TryGetValue(key, out var action)) return action as Action<T, TU>;
            return CreateCopyAction<T, TU>(key);

        }

        /// <summary>
        /// Copies the action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Action<T, T> CopyAction<T>()

        {
            var key = typeof(T).FullName;
            if (_actions.TryGetValue(key, out var action)) return action as Action<T, T>;
            return CreateCopyAction<T,T>(key);
        }

        /// <summary>
        /// Creates the copy action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU">The type of the u.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private Action<T, TU> CreateCopyAction<T, TU>(string key)
        {
            var className = CopyProviderHelper.GetClassName(typeof(T), typeof(TU));

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                       BindingFlags.InvokeMethod;

            if (!_comp.TryGetValue(className, out var comp)) comp = GenerateCopyClass(typeof(T), typeof(TU));

            var result = comp.InvokeMember(CopyActionMethodName, flags, null,
                null, null) as Action<T, TU>;

            _actions.Add(key, result);
            return result;
        }

        /// <summary>
        /// Generates the copy class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU">The type of the u.</typeparam>
        /// <returns></returns>
        public Type GenerateCopyClass<T, TU>()
        {
            var sourceType = typeof(T);
            var targetType = typeof(TU);
            return GenerateCopyClass(sourceType, targetType);
        }

        /// <summary>
        /// Generates the copy class.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns></returns>
        public Type GenerateCopyClass(Type sourceType, Type targetType)
        {
            var className = CopyProviderHelper.GetClassName(sourceType, targetType);

            if (_comp.TryGetValue(className, out var type)) return type;

            var unit = new CodeCompileUnit();
            
            var ns = CreateNamespace();
            unit.Namespaces.Add(ns);

            var targetClass = CreateClass(className);
            ns.Types.Add(targetClass);
            
            var copyMethod = CreateCopyMethod(sourceType, targetType);
            targetClass.Members.Add(copyMethod);
            
            var copyActionMethod = CreateCopyActionMethod(sourceType, targetType, copyMethod);
            targetClass.Members.Add(copyActionMethod);

            Debug.WriteLine(unit.ToString());

            var results = BuildAssembly(unit, targetType.Assembly);

            // Compiler output

            foreach (var line in results.Output) Debug.WriteLine(line);

            var copierType = results.CompiledAssembly.GetType(NamespaceName + "." + className);

            _comp.Add(className, copierType);

            return copierType;
        }

        /// <summary>
        /// Builds the assembly.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="referencedAssembly">The referenced assembly.</param>
        /// <returns></returns>
        private static CompilerResults BuildAssembly(CodeCompileUnit unit, Assembly referencedAssembly)
        {
            var codeCompiler = CodeDomProvider.CreateProvider("CSharp");

            var compilerParameters = new CompilerParameters();

            compilerParameters.ReferencedAssemblies.Add(referencedAssembly.Location);

            compilerParameters.GenerateInMemory = true;

            var results = codeCompiler.CompileAssemblyFromDom(compilerParameters, unit);
            return results;
        }

        /// <summary>
        /// Creates the class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns></returns>
        private static CodeTypeDeclaration CreateClass(string className)
        {
            var targetClass = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
            };
            return targetClass;
        }

        /// <summary>
        /// Creates the namespace.
        /// </summary>
        /// <returns></returns>
        private static CodeNamespace CreateNamespace()
        {
            var copyHelper = new CodeNamespace(NamespaceName);
            copyHelper.Imports.Add(new CodeNamespaceImport("System"));
            return copyHelper;
        }

        /// <summary>
        /// Creates the copy action method.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="copyMethod">The copy method.</param>
        /// <returns></returns>
        private static CodeMemberMethod CreateCopyActionMethod(Type sourceType, Type targetType, CodeTypeMember copyMethod)
        {
            var copyActionMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = CopyActionMethodName
            };

            var action = new CodeTypeReference(typeof(Action<,>));
            action.TypeArguments.Add(sourceType);
            action.TypeArguments.Add(targetType);

            copyActionMethod.ReturnType = action;
            var returnStatement =
                new CodeMethodReturnStatement(new CodeVariableReferenceExpression(copyMethod.Name));

            copyActionMethod.Statements.Add(returnStatement);
            return copyActionMethod;
        }

        /// <summary>
        /// Creates the copy method.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns></returns>
        private static CodeMemberMethod CreateCopyMethod(Type sourceType, Type targetType)
        {
            var copyMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = CopyMethodName
            };

            var source = new CodeParameterDeclarationExpression(sourceType, "source");
            var target = new CodeParameterDeclarationExpression(targetType, "target");
            copyMethod.Parameters.Add(source);
            copyMethod.Parameters.Add(target);

            var map = CopyProviderHelper.GetMatchingProperties(sourceType, targetType);

            foreach (var item in map)

            {
                var targetReference =
                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(target.Name),
                        item.TargetProperty.Name);
                var sourceReference =
                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(source.Name),
                        item.SourceProperty.Name);
                copyMethod.Statements.Add(new CodeAssignStatement(targetReference, sourceReference));
            }

            return copyMethod;
        }
    }
}