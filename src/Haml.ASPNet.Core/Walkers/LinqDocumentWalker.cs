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

namespace NHaml.Walkers
{
    public class LinqDocumentWalker
    {
        private IList<Expression> _expressions;
        private IList<Tuple<int, string>> _lateBoundExpressions;
        private ParameterExpression _textWriterParameter;
        private ParameterExpression _modelParameter;
        private MethodInfo writeMethodInfo;
        private StringBuilder textRun;
        private Type modelType;
        private Compilation compilation;
        private ClassDeclarationSyntax compilationTargetClass;
        private CompilationUnitSyntax compilationUnit;

        public LinqDocumentWalker(Type modelType)
        {
            _expressions = new List<Expression>();
            _textWriterParameter = Expression.Parameter(typeof(TextWriter));
            _modelParameter = Expression.Parameter(modelType);
            writeMethodInfo = typeof(TextWriter).GetMethod("Write", new Type[] { typeof(string) });
            textRun = new StringBuilder();
            this.modelType = modelType;
            compilation = CSharpCompilation.Create("Compilation", references: new[] {
                MetadataReference.CreateFromFile(typeof(Object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(modelType.GetTypeInfo().Assembly.Location)
            }).WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilationTargetClass = SyntaxFactory.ClassDeclaration("__haml_UserCode_CompilationTarget");
            compilationUnit = SyntaxFactory.CompilationUnit();
            _lateBoundExpressions = new List<Tuple<int, String>>();
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
                    continue;
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
            var methodName = node.Content.GetHashCode().ToString("x");
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)), methodName)
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Parameter(SyntaxFactory.Identifier("model")).WithType(SyntaxFactory.IdentifierName(modelType.FullName)) })))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ParseExpression(node.Content))))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            compilationTargetClass = compilationTargetClass.AddMembers(method);
            FlushStringRun();
            _lateBoundExpressions.Add(new Tuple<int, string>(_expressions.Count, methodName));
        }

        private void Walk(HamlNodeTextVariable node)
        {
            FlushStringRun();
            // Begin Crappy Rubyish compiler
            string[] blocks = node.VariableName.Split('.');
            Expression expr = null;
            Type objectType = modelType;
            foreach (var block in blocks)
            {
                if (block == "model")
                {
                    expr = _modelParameter;
                }
                else
                {
                    MethodInfo methInfo = objectType.GetMethod(block);
                    objectType = methInfo.ReturnType;
                    expr = Expression.Call(expr, methInfo);
                }
            }
            FlushStringRun();
            _expressions.Add(Expression.Call(_textWriterParameter, writeMethodInfo, expr));
        }

        private void Walk(HamlNodeHtmlAttribute node)
        {
            textRun.AppendFormat(" {0}=\"", node.Name);
            this.Walk(node.Children);
            textRun.Append('"');
        }

        private void Walk(HamlNodeTag node)
        {
            textRun.Append("<");
            textRun.Append(node.NamespaceQualifiedTagName);

            this.Walk(node.Attributes);
            if (node.Children.Count > 0)
            {
                textRun.Append(">");
                this.Walk(node.Children);
                textRun.Append("</");
                textRun.Append(node.NamespaceQualifiedTagName);
            }
            else if (!node.IsSelfClosing)
            {
                textRun.Append("/");
            }

            textRun.Append(">");
        }

        public Delegate Compile()
        {
            FlushStringRun();
            CommitStageTwo();
            var method = Expression.Block(_expressions);
            var lambda = Expression.Lambda(method, _textWriterParameter, _modelParameter);
            Delegate output = lambda.Compile();

            return output;
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
            Type type = assembly.GetType(compilationTargetClass.Identifier.Text);
            int offset = 0;
            foreach (var expr in _lateBoundExpressions)
            {
                MethodInfo evalMethod = type.GetMethod(expr.Item2);
                _expressions.Insert(expr.Item1 + offset, Expression.Call(_textWriterParameter, writeMethodInfo, Expression.Call(evalMethod, _modelParameter)));
                offset++;
            }
        }

        private void FlushStringRun()
        {
            if (textRun.Length == 0)
            {
                return;
            }
            _expressions.Add(Expression.Call(_textWriterParameter, writeMethodInfo, Expression.Constant(textRun.ToString())));
            textRun.Clear();
        }
    }
}
