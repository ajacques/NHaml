using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web.NHaml.Parser;
using System.Web.NHaml.Parser.Rules;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Emit;
using System.Diagnostics;

namespace NHaml.Walkers
{
    public class LinqDocumentWalker
    {
        private IList<IIntermediateNode> nodes;
        private ParameterExpression _textWriterParameter;
        private ParameterExpression _modelParameter;
        private MethodInfo writeMethodInfo;
        private StringBuilder textRun;
        private Type modelType;
        private Compilation compilation;
        private ClassDeclarationSyntax compilationTargetClass;
        private CompilationUnitSyntax compilationUnit;
        private Type compilationTargetType;

        /// <summary>
        /// Represents either a fully compiled expression or a partial
        /// compilation 
        /// </summary>
        private interface IIntermediateNode
        {
            Expression Build();
        }

        [DebuggerDisplay("Late-bound method: {MethodName}()")]
        private class LateBoundMethodCall : IIntermediateNode
        {
            private LinqDocumentWalker walker;

            public LateBoundMethodCall(LinqDocumentWalker walker, string methodName)
            {
                this.walker = walker;
                MethodName = methodName;
            }

            public string MethodName
            {
                get;
                private set;
            }

            public Expression Build()
            {
                MethodInfo evalMethod = walker.compilationTargetType.GetMethod(MethodName);
                return Expression.Call(walker._textWriterParameter, walker.writeMethodInfo, Expression.Call(evalMethod, walker._modelParameter));
            }
        }
        private class ConditionalExpression : IIntermediateNode
        {
            private string methodName;
            private LinqDocumentWalker walker;
            private IEnumerable<IIntermediateNode> ifBlock;

            public ConditionalExpression(LinqDocumentWalker walker, string methodName, IEnumerable<IIntermediateNode> ifBlock)
            {
                this.walker = walker;
                this.methodName = methodName;
                this.ifBlock = ifBlock;
            }

            public Expression Build()
            {
                MethodInfo evalMethod = walker.compilationTargetType.GetMethod(methodName);
                return Expression.IfThen(Expression.Call(evalMethod, walker._modelParameter), Expression.Block(ifBlock.Select(n => n.Build())));
            }
        }

        [DebuggerDisplay("Static expression: {expression}")]
        private class StaticExpression : IIntermediateNode
        {
            private Expression expression;

            public StaticExpression(Expression expr)
            {
                expression = expr;
            }

            public Expression Build()
            {
                return expression;
            }
        }

        public LinqDocumentWalker(Type modelType)
        {
            _textWriterParameter = Expression.Parameter(typeof(TextWriter));
            _modelParameter = Expression.Parameter(modelType);
            writeMethodInfo = typeof(TextWriter).GetMethod("Write", new Type[] { typeof(string) });
            textRun = new StringBuilder();
            this.modelType = modelType;
            compilation = CSharpCompilation.Create("Compilation")
                .WithReferences(
                    MetadataReference.CreateFromFile(typeof(Object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(modelType.GetTypeInfo().Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilationTargetClass = SyntaxFactory.ClassDeclaration("__haml_UserCode_CompilationTarget")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            compilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")) }));
            nodes = new List<IIntermediateNode>();
        }

        public void Walk(HamlDocument document)
        {
            Walk(document.Children);
        }

        public void Walk(IEnumerable<HamlNode> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (HamlNode node in nodes)
            {
                Type nodeType = node.GetType();
                if (nodeType == typeof(HamlNodeTagId) || nodeType == typeof(HamlNodeTagClass) || nodeType == typeof(HamlNodeHtmlAttributeCollection))
                    Walk(node.Children);
                if (nodeType == typeof(HamlNodeTextContainer))
                    Walk(node.Children);
                if (nodeType == typeof(HamlNodeTag))
                    Walk((HamlNodeTag)node);
                if (nodeType == typeof(HamlNodeEval))
                    Walk((HamlNodeEval)node);
                if (nodeType == typeof(HamlNodeCode))
                    Walk((HamlNodeCode)node);
                if (nodeType == typeof(HamlNodeTextLiteral))
                    textRun.Append(((HamlNodeTextLiteral)node).Content);
                if (nodeType == typeof(HamlNodeTextVariable))
                    Walk((HamlNodeTextVariable)node);
                if (nodeType == typeof(HamlNodeDocType))
                    Walk((HamlNodeDocType)node);
                if (nodeType == typeof(HamlNodeHtmlAttribute))
                    Walk((HamlNodeHtmlAttribute)node);
                if (nodeType == typeof(HamlNodePartial))
                    continue;
            }
        }

        private void Walk(HamlNodeDocType docType)
        {
            textRun.Append("<!DOCTYPE html>");
        }

        private void Walk(HamlNodeEval node)
        {
            CompileAndInjectCodeThunk(node.Content);
        }

        private void Walk(HamlNodeCode node)
        {
            if (node.Content.Trim().StartsWith("if"))
            {
                FlushStringRun();
                int start = node.Content.IndexOf('(') + 1;
                string expression = node.Content.Substring(start, node.Content.Length - start - 1);
                string methodName = CompileCodeThunk(expression, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));
                FlushStringRun();
                var oldNodes = nodes;
                nodes = new List<IIntermediateNode>();

                Walk(node.Children);
                FlushStringRun();
                oldNodes.Add(new ConditionalExpression(this, methodName, nodes));
                nodes = oldNodes;
            }
        }

        private void Walk(HamlNodeTextVariable node)
        {
            CompileAndInjectCodeThunk(node.VariableName);
        }

        private void Walk(HamlNodeHtmlAttribute node)
        {
            textRun.AppendFormat(" {0}=\"", node.Name);
            this.Walk(node.Children);
            textRun.Append('"');
        }

        private void Walk(HamlNodeTag node)
        {
            textRun.Append('<');
            textRun.Append(node.NamespaceQualifiedTagName);

            this.Walk(node.Attributes);
            if (node.Children.Count > 0)
            {
                textRun.Append('>');
                this.Walk(node.Children);
                textRun.Append("</");
                textRun.Append(node.NamespaceQualifiedTagName);
            }
            else if (!node.IsSelfClosing)
            {
                textRun.Append('/');
            }

            textRun.Append('>');
        }

        public Delegate Compile()
        {
            FlushStringRun();
            CommitStageTwo();
            var method = Expression.Block(nodes.Select(n => n.Build()));
            var lambda = Expression.Lambda(method, _textWriterParameter, _modelParameter);
            Delegate output = lambda.Compile();

            return output;
        }

        private string CompileAndInjectCodeThunk(string content)
        {
            string methodName = CompileCodeThunk(content, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)));

            FlushStringRun();
            nodes.Add(new LateBoundMethodCall(this, methodName));

            return methodName;
        }

        private string CompileCodeThunk(string content, TypeSyntax returnType)
        {
            var methodName = content.GetHashCode().ToString("x");
            var method = SyntaxFactory.MethodDeclaration(returnType, methodName)
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Parameter(SyntaxFactory.Identifier("model")).WithType(SyntaxFactory.ParseTypeName(modelType.FullName)) })))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ParseExpression(content))))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            compilationTargetClass = compilationTargetClass.AddMembers(method);

            return methodName;
        }

        private void CommitStageTwo()
        {
            compilationUnit = compilationUnit.AddMembers(compilationTargetClass);
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.Create(compilationUnit));
            MemoryStream stream = new MemoryStream();
            EmitResult result = compilation.Emit(stream);
            if (!result.Success)
            {
                throw new Exception("Syntax error");
            }
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            compilationTargetType = assembly.GetType(compilationTargetClass.Identifier.Text);
        }
        private void AppendExpression(Expression expression)
        {
            nodes.Add(new StaticExpression(expression));
        }

        private void FlushStringRun()
        {
            if (textRun.Length == 0)
            {
                return;
            }
            AppendExpression(Expression.Call(_textWriterParameter, writeMethodInfo, Expression.Constant(textRun.ToString())));
            textRun.Clear();
        }
    }
}
