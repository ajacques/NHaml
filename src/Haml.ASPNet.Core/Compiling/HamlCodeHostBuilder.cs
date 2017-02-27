using Haml.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using NHaml.Walkers.Exceptions;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Haml.Compiling
{
    public class HamlCodeHostBuilder
    {
        private Compilation compilation;
        private ClassDeclarationSyntax compilationTargetClass;
        private CompilationUnitSyntax compilationUnit;
        private Type modelType;

        public HamlCodeHostBuilder(Type modelType)
        {
            this.modelType = modelType;
            compilation = CSharpCompilation.Create("Compilation")
                .WithReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(modelType.GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BaseViewClass).GetTypeInfo().Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));
            compilationTargetClass = SyntaxFactory.ClassDeclaration("__haml_UserCode_CompilationTarget")
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeof(BaseViewClass).FullName)));
            compilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")) }));
        }

        public string NewCodeBlock(string content, TypeSyntax returnType)
        {
            var methodName = content.GetHashCode().ToString("x");

            var body = SyntaxFactory.ParseExpression(content);

            var method = SyntaxFactory.MethodDeclaration(returnType, methodName)
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Parameter(SyntaxFactory.Identifier("model")).WithType(SyntaxFactory.ParseTypeName(modelType.FullName)) })))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(body)))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            compilationTargetClass = compilationTargetClass.AddMembers(method);

            return methodName;
        }

        public Type CompileClass()
        {
            compilationUnit = compilationUnit.AddMembers(compilationTargetClass);
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
    }
}
