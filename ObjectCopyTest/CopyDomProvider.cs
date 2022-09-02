using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace ObjectCopyTest
{
    public class CopyDomProvider
    {
        /// <summary>
        /// The copy action method name
        /// </summary>
        private const string COPY_ACTION_METHOD_NAME = "CopyAction";

        /// <summary>
        /// The copy method name
        /// </summary>
        private const string COPY_METHOD_NAME = "Copy";

        /// <summary>
        /// The namespace name
        /// </summary>
        private const string NAMESPACE_NAME = "DynamicCode.CopyableHelper";

        /// <summary>
        /// The actions
        /// </summary>
        private readonly Dictionary<Type, Action<object, object>> _actions = new Dictionary<Type, Action<object, object>>();

        /// <summary>
        /// The comp
        /// </summary>
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
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public void Copy<TSource, TTarget>(TSource source, TTarget target)
        {
            CopyAction(target.GetType())(source, target);
        }

        /// <summary>
        /// Copies the action.
        /// </summary>
        /// <returns>
        /// The copy action
        /// </returns>
        public Action<object, object> CopyAction(Type targetType)
        {
            var key = targetType;
            if (_actions.TryGetValue(key, out var action))
            {
                return action;
            }

            return CreateCopyAction(key);
        }

        /// <summary>
        /// Copies the action.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>
        /// The copy action
        /// </returns>
        public Action<object, object> CopyAction<T>()
        {
            var key = typeof(T);
            if (_actions.TryGetValue(key, out var action))
            {
                return action;
            }

            return CreateCopyAction(key);
        }

        /// <summary>
        /// Generates the copy class.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <returns>The type.</returns>
        public Type GenerateCopyClass<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            return GenerateCopyClass(sourceType, targetType);
        }

        /// <summary>
        /// Generates the copy class.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns>The type.</returns>
        public Type GenerateCopyClass(Type sourceType, Type targetType)
        {
            var className = CopyProviderHelper.GetClassName(sourceType, targetType);

            if (_comp.TryGetValue(className, out var type))
            {
                return type;
            }

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

            foreach (var line in results.Output)
            {
                Debug.WriteLine(line);
            }

            var copierType = results.CompiledAssembly.GetType(NAMESPACE_NAME + "." + className);

            _comp.Add(className, copierType);

            return copierType;
        }

        /// <summary>
        /// Builds the assembly.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="referencedAssembly">The referenced assembly.</param>
        /// <returns>The CompilerResults.</returns>
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
        /// <returns>The CodeTypeDeclaration.</returns>
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
        /// Creates the copy action method.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="copyMethod">The copy method.</param>
        /// <returns>The CodeMemberMethod.</returns>
        private static CodeMemberMethod CreateCopyActionMethod(Type sourceType, Type targetType, CodeTypeMember copyMethod)
        {
            var copyActionMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = COPY_ACTION_METHOD_NAME
            };

            var action = new CodeTypeReference(typeof(Action<,>));
            action.TypeArguments.Add(typeof(object));
            action.TypeArguments.Add(typeof(object));

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
        /// <returns>The CodeMemberMethod.</returns>
        private static CodeMemberMethod CreateCopyMethod(Type sourceType, Type targetType)
        {
            var copyMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = COPY_METHOD_NAME
            };

            var sourceParameter = new CodeParameterDeclarationExpression(typeof(object), "source");
            var targetParameter = new CodeParameterDeclarationExpression(typeof(object), "target");
            copyMethod.Parameters.Add(sourceParameter);
            copyMethod.Parameters.Add(targetParameter);

            var source = new CodeParameterDeclarationExpression(targetType, "s");
            var target = new CodeParameterDeclarationExpression(targetType, "t");

            copyMethod.Statements.Add(new CodeAssignStatement(target, new CodeCastExpression(targetType, new CodeVariableReferenceExpression(targetParameter.Name))));
            copyMethod.Statements.Add(new CodeAssignStatement(source, new CodeCastExpression(sourceType, new CodeVariableReferenceExpression(sourceParameter.Name))));

            var propertyInfos = targetType.GetPropertyWithAttribute<CopyableAttribute>();

            foreach (var propertyInfo in propertyInfos)
            {
                var targetReference =
                    new CodeFieldReferenceExpression(
                        new CodeVariableReferenceExpression(target.Name),
                        propertyInfo.Name);

                var sourceReference =
                    new CodeFieldReferenceExpression(
                        new CodeVariableReferenceExpression(source.Name),
                        propertyInfo.Name);
                copyMethod.Statements.Add(new CodeAssignStatement(targetReference, sourceReference));
            }

            return copyMethod;
        }

        /// <summary>
        /// Creates the namespace.
        /// </summary>
        /// <returns>The CodeNamespace.</returns>
        private static CodeNamespace CreateNamespace()
        {
            var copyHelper = new CodeNamespace(NAMESPACE_NAME);
            copyHelper.Imports.Add(new CodeNamespaceImport("System"));
            return copyHelper;
        }

        /// <summary>
        /// Creates the copy action.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>
        /// The copy action
        /// </returns>
        private Action<object, object> CreateCopyAction(Type key)
        {
            var className = CopyProviderHelper.GetClassName(key, key);

            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod;

            if (!_comp.TryGetValue(className, out var comp))
            {
                comp = GenerateCopyClass(key, key);
            }

            var result = comp.InvokeMember(
                COPY_ACTION_METHOD_NAME,
                FLAGS,
                null,
                null,
                null) as Action<object, object>;

            _actions.Add(key, result);
            return result;
        }
    }
}