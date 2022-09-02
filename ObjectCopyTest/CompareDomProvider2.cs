using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace ObjectCopyTest
{
    public class CompareDomProvider2 
    {
        private const string CompareActionMethodName = "CompareAction";
        private const string CompareMethodName = "Compare";
        private const string NamespaceName = "DynamicCode.ComparableHelper";

        private readonly Dictionary<string, object> _functions = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> _comp = new Dictionary<string, Type>();

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static CompareDomProvider2 Instance { get; } = new CompareDomProvider2();

        /// <summary>
        /// Copies the specified source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public void Compare<T>(T source, T target)
        {
            CompareAction<T>()(source, target);
        }

        /// <summary>
        /// Copies the action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Func<T, T, bool> CompareAction<T>()

        {
            var key = typeof(T).FullName;
            if (_functions.TryGetValue(key, out var func)) return func as Func<T, T, bool>;
            return CreateCompareAction<T>(key);
        }

        /// <summary>
        /// Creates the copy action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private Func<T, T, bool> CreateCompareAction<T>(string key)
        {
            var className = GetClassName(typeof(T));

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                       BindingFlags.InvokeMethod;

            if (!_comp.TryGetValue(className, out var comp)) comp = GenerateComparableClass(typeof(T), typeof(T));

            var result = comp.InvokeMember(CompareActionMethodName, flags, null,
                null, null) as Func<T, T, bool>;

            _functions.Add(key, result);
            return result;
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">targetType</exception>
        public static string GetClassName(Type targetType)

        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            var className = "Compare_";
            className += targetType.FullName.Replace(".", "_");
            return className;
        }

        /// <summary>
        /// Generates the copy class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU">The type of the u.</typeparam>
        /// <returns></returns>
        public Type GenerateComparableClass<T, TU>()
        {
            var sourceType = typeof(T);
            var targetType = typeof(TU);
            return GenerateComparableClass(sourceType, targetType);
        }

        /// <summary>
        /// Generates the copy class.
        /// </summary>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns></returns>
        public Type GenerateComparableClass(Type sourceType, Type targetType)
        {
            var className = GetClassName(sourceType);

            if (_comp.TryGetValue(className, out var type)) return type;

            var unit = new CodeCompileUnit();
            
            var ns = CreateNamespace();
            unit.Namespaces.Add(ns);

            var targetClass = CreateClass(className);
            ns.Types.Add(targetClass);
            
            var compareMethod = CreateCompareMethod(sourceType, targetType);
            targetClass.Members.Add(compareMethod);
            
            var copyActionMethod = CreateCopyActionMethod(sourceType, targetType, compareMethod);
            targetClass.Members.Add(copyActionMethod);

            Debug.WriteLine(unit.ToString());

            var results = BuildAssembly(unit, targetType.Assembly);

            // Compiler output

            foreach (var line in results.Output) Debug.WriteLine(line);

            CSharpCodeProvider provider = new CSharpCodeProvider();

            using (StreamWriter sw = new StreamWriter(@"c:\temp\CompareHelper.cs", false))
            {
                IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");

                // Generate source code using the code provider.
                provider.GenerateCodeFromCompileUnit(unit, tw,
                    new CodeGeneratorOptions());

                // Close the output file.
                tw.Close();
            }

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
                Name = CompareActionMethodName
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
        private static CodeMemberMethod CreateCompareMethod(Type sourceType, Type targetType)
        {
            var compareMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = CompareMethodName
            };

            var source = new CodeParameterDeclarationExpression(sourceType, "source");
            var target = new CodeParameterDeclarationExpression(targetType, "target");
            compareMethod.Parameters.Add(source);
            compareMethod.Parameters.Add(target);

            var propertyInfos = targetType.GetPropertyWithAttribute<CopyableAttribute>();

            foreach (var propertyInfo in propertyInfos.Where(p => p.PropertyType.IsValueType))
            {
                var targetReference =
                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(target.Name),
                        propertyInfo.Name);

                var sourceReference =
                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(source.Name),
                        propertyInfo.Name);

                var valueEquality = new CodeBinaryOperatorExpression(
                    targetReference,
                    CodeBinaryOperatorType.IdentityInequality,
                    sourceReference);

                var conditionalStatement = new CodeConditionStatement(
                    valueEquality,
                    new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));
                    
               
                compareMethod.Statements.Add(conditionalStatement);
            }

            foreach (var propertyInfo in propertyInfos.Where(p => !p.PropertyType.IsValueType))
            {
                var targetReference =
                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(target.Name),
                        propertyInfo.Name);

                var sourceReference =
                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression(source.Name),
                        propertyInfo.Name);

                var valueEquality = new CodeBinaryOperatorExpression(
                    targetReference,
                    CodeBinaryOperatorType.IdentityInequality,
                    sourceReference);

                var conditionalStatement = new CodeConditionStatement(
                    valueEquality,
                    new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));


                compareMethod.Statements.Add(conditionalStatement);
            }

            var returnTrueStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(true));
  
            compareMethod.Statements.Add(returnTrueStatement);

            return compareMethod;
        }


      }
}