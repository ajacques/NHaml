using Haml.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using NHaml.Walkers.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Web.NHaml.Parser;
using System.Web.NHaml.Parser.Rules;

namespace NHaml.Walkers
{
    public class HamlCodeHostBuilder
    {
        private Compilation compilation;
        private ClassDeclarationSyntax compilationTargetClass;
        private CompilationUnitSyntax compilationUnit;
        private Type compilationTargetType;

        public HamlCodeHostBuilder(Type modelType)
        {
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
    }
}
