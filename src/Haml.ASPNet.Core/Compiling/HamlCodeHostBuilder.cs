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

namespace Haml.Compiling
{
    public class HamlCodeHostBuilder : ITemplateRenderer
    {
        private Compilation _compilation;
        private ClassDeclarationSyntax _compilationTargetClass;
        private CompilationUnitSyntax _compilationUnit;
        private Type _modelType;
        private MethodDeclarationSyntax _renderMethod;
        private StringBuilder textRun;
        private Stack<IList<StatementSyntax>> expressions;

        public HamlCodeHostBuilder(Type modelType)
        {
            this._modelType = modelType;
            var modelTypeToken = SyntaxFactory.ParseTypeName(modelType.FullName);
            var rootDirectory = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            var className = "__haml_UserCode_CompilationTarget";
            _compilation = CSharpCompilation.Create("Compilation")
                .WithReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(rootDirectory, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(rootDirectory, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(modelType.GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(HtmlHelper).GetTypeInfo().Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));
            _compilationTargetClass = SyntaxFactory.ClassDeclaration(className);
            _compilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")) }));

            textRun = new StringBuilder();
            _compilationTargetClass = _compilationTargetClass
                /*.AddMembers(
                    SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(typeof(HtmlHelper).FullName), "Html"))*/
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword))
                .AddMembers(
                    SyntaxFactory.ConstructorDeclaration(className)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("_modelType")).WithType(modelTypeToken))
                        .AddBodyStatements(SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("model"), SyntaxFactory.IdentifierName("_modelType")))))
               .AddMembers(SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(modelTypeToken, SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator("model")))));

            _renderMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "render")
                                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("textWriter")).WithType(SyntaxFactory.ParseTypeName(typeof(TextWriter).FullName)))
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            expressions = new Stack<IList<StatementSyntax>>();
            expressions.Push(new List<StatementSyntax>());
        }

        public void PrintExpressionResult(string code)
        {
            FlushStringRun();
            expressions.Peek().Add(TextRunWriteSyntax(SyntaxFactory.ParseExpression(code)));
        }

        public void InlineCodeExpression(string content)
        {
            FlushStringRun();
            expressions.Peek().Add(SyntaxFactory.ParseStatement(content));
        }

        public string NewCodeBlock(string content, TypeSyntax returnType)
        {
            var methodName = string.Format("_{0:x}", content.GetHashCode());

            var body = SyntaxFactory.ParseExpression(content);

            var method = SyntaxFactory.MethodDeclaration(returnType, methodName)
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(body)))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
            _compilationTargetClass = _compilationTargetClass.AddMembers(method);

            return methodName;
        }

        public Type Compile()
        {
            if (expressions.Count != 1)
            {
                throw new Exception("HAML node stack misaligned. Expected only 1 root node.");
            }
            FlushStringRun();
            _renderMethod = _renderMethod.AddBodyStatements(SyntaxFactory.Block(expressions.Peek()));

            _compilationTargetClass = _compilationTargetClass.AddMembers(_renderMethod);
            _compilationUnit = _compilationUnit
                .AddMembers(_compilationTargetClass);
            _compilationUnit = _compilationUnit.NormalizeWhitespace("    ", true);
            _compilation = _compilation.AddSyntaxTrees(CSharpSyntaxTree.Create(_compilationUnit));
            MemoryStream stream = new MemoryStream();
            EmitResult result = _compilation.Emit(stream);
            _compilation.Emit("Output.dll");
            if (!result.Success)
            {
                throw new HamlCompilationFailedException(result.Diagnostics);
            }
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            return assembly.GetType(_compilationTargetClass.Identifier.Text);
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

        public void CallMethod(string name)
        {
            FlushStringRun();
            expressions.Peek().Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression(string.Format("textWriter.Write({0}())", name))));
        }

        public void ConditionalBegin()
        {
            FlushStringRun();
            expressions.Push(new List<StatementSyntax>());
        }

        public void ConditionalElseBegin()
        {
            FlushStringRun();
            expressions.Push(new List<StatementSyntax>());
        }

        public void ConditionalElseEnd()
        {
            FlushStringRun();
            var expr = expressions.Pop();
            var last = expressions.Peek().Count - 1;
            var ifStatement = expressions.Peek()[last] as IfStatementSyntax;
            if (ifStatement == null)
            {
                throw new Exception();
            }
            expressions.Peek()[last] = ifStatement.WithElse(SyntaxFactory.ElseClause(SyntaxFactory.Block(expr)));
        }

        public void ConditionalEnd(string expressionContent)
        {
            FlushStringRun();
            var expression = expressions.Pop();
            var conditional = SyntaxFactory.ParseExpression(expressionContent);
            expressions.Peek().Add(SyntaxFactory.IfStatement(conditional, SyntaxFactory.Block(expression)));
        }

        private static ExpressionStatementSyntax TextRunWriteSyntax(ExpressionSyntax value)
        {
            return SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("textWriter"), SyntaxFactory.IdentifierName("Write")),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(value)))));
        }

        private void FlushStringRun()
        {
            if (textRun.Length == 0)
            {
                return;
            }
            expressions.Peek().Add(TextRunWriteSyntax(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(textRun.ToString()))));
            textRun.Clear();
        }
    }
}
