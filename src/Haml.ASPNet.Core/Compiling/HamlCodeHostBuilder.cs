using Haml.Framework;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using NHaml.Walkers.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Linq;

namespace Haml.Compiling
{
    public class HamlCodeHostBuilder : ITemplateRenderer
    {
        private Compilation compilation;
        private ClassDeclarationSyntax compilationTargetClass;
        private CompilationUnitSyntax compilationUnit;
        private Type modelType;
        private MethodDeclarationSyntax renderMethod;
        private StringBuilder textRun;
        private Stack<IList<StatementSyntax>> expressions;

        public HamlCodeHostBuilder(Type modelType)
        {
            this.modelType = modelType;
            var rootDirectory = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            compilation = CSharpCompilation.Create("Compilation")
                .WithReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(rootDirectory, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(rootDirectory, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(modelType.GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(HtmlHelper).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BaseViewClass).GetTypeInfo().Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));
            compilationTargetClass = SyntaxFactory.ClassDeclaration("__haml_UserCode_CompilationTarget");
            compilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")) }));

            compilationTargetClass = compilationTargetClass.AddMembers(
                SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(typeof(HtmlHelper).FullName), "Html"));
            textRun = new StringBuilder();

            renderMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "render")
                                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("textWriter")).WithType(SyntaxFactory.ParseTypeName(typeof(TextWriter).FullName)))
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            expressions = new Stack<IList<StatementSyntax>>();
            expressions.Push(new List<StatementSyntax>());
        }

        public string NewCodeBlock(string content, TypeSyntax returnType)
        {
            var methodName = string.Format("_{0:x}", content.GetHashCode());

            var body = SyntaxFactory.ParseExpression(content);

            var method = SyntaxFactory.MethodDeclaration(returnType, methodName)
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Parameter(SyntaxFactory.Identifier("model")).WithType(SyntaxFactory.ParseTypeName(modelType.FullName)) })))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(body)))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
            compilationTargetClass = compilationTargetClass.AddMembers(method);

            return methodName;
        }

        public Type Compile()
        {
            if (expressions.Count != 1)
            {
                throw new Exception("HAML node stack misaligned. Expected only 1 root node.");
            }
            FlushStringRun();
            renderMethod = renderMethod.AddBodyStatements(expressions.Peek().ToArray());

            compilationTargetClass = compilationTargetClass.AddMembers(renderMethod);
            compilationUnit = compilationUnit
                .AddMembers(compilationTargetClass);
            compilationUnit = compilationUnit.NormalizeWhitespace("    ", true);
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.Create(compilationUnit));
            MemoryStream stream = new MemoryStream();
            EmitResult result = compilation.Emit(stream);
            if (!result.Success)
            {
                throw new HamlCompilationFailedException(result.Diagnostics);
            }
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            return assembly.GetType(compilationTargetClass.Identifier.Text);
        }

        public void Write(string content)
        {
            textRun.Append(content);
        }

        public void Write(char content)
        {
            textRun.Append(content);
        }

        public void Write(string format, params object[] formats)
        {
            textRun.AppendFormat(format, formats);
        }

        public void ConditionalBegin()
        {
            FlushStringRun();
            expressions.Push(new List<StatementSyntax>());
        }

        public void ConditionalElseBegin()
        {
            FlushStringRun();
        }

        public void ConditionalElseEnd()
        {
            FlushStringRun();
        }

        public void ConditionalEnd(string methodName)
        {
            FlushStringRun();
            var expression = expressions.Pop();
            var t = CSharpSyntaxTree.ParseText("public class Foo { public void t() { if (true) { this.DoFirst(); this.DoSecond(); }}}");
            var conditional = SyntaxFactory.ParseExpression(string.Format("{0}()", methodName));
            expressions.Peek().Add(SyntaxFactory.IfStatement(conditional, SyntaxFactory.Block(expression)));
        }

        private void FlushStringRun()
        {
            if (textRun.Length == 0)
            {
                return;
            }
            var v = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("textWriter"), SyntaxFactory.IdentifierName("Write")), 
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(textRun.ToString()))))));
            expressions.Peek().Add(SyntaxFactory.ExpressionStatement(v));
            textRun.Clear();
        }
    }
}
