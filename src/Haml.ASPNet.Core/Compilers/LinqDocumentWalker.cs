using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.NHaml.Parser;
using System.Web.NHaml.Parser.Rules;

namespace Haml.Compiling
{
    public class LinqDocumentWalker
    {
        private Type compilationTargetType;
        private TemplateILStream _templateILStream;
        private HamlCodeHostBuilder _codeClassBuilder;

        public LinqDocumentWalker(Type modelType)
        {
            _templateILStream = new TemplateILStream(modelType);
            _codeClassBuilder = new HamlCodeHostBuilder(modelType);
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
                    _templateILStream.WriteStaticString(((HamlNodeTextLiteral)node).Content);
                if (nodeType == typeof(HamlNodeTextVariable))
                    Walk((HamlNodeTextVariable)node);
                if (nodeType == typeof(HamlNodeDocType))
                    Walk((HamlNodeDocType)node);
                if (nodeType == typeof(HamlNodePartial))
                    continue;
            }
        }

        private void Walk(HamlNodeDocType docType)
        {
            _templateILStream.WriteStaticString("<!DOCTYPE html>");
        }

        private void Walk(HamlNodeEval node)
        {
            CompileAndInjectCodeThunk(node.Content);
        }
        
        private void Walk(HamlNodeCode node)
        {
            var content = node.Content.Trim();
            // Conditionals require special logic since the parser doesn't yet extract it for us
            if (content.StartsWith("if"))
            {
                int start = content.IndexOf('(') + 1;
                string expression = content.Substring(start, content.Length - start - 1);
                string methodName = CompileCodeThunk(expression, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));

                _templateILStream.ConditionalBegin();
                Walk(node.Children);
                _templateILStream.ConditionalEnd(methodName);
            }
            else if (content == "else")
            {
                _templateILStream.ConditionalElseBegin();
                Walk(node.Children);
                _templateILStream.ConditionalElseEnd();
            }
            else
            {
                throw new Exception("Side-effect free haml code block unsupported.");
            }
        }

        private void Walk(HamlNodeTextVariable node)
        {
            CompileAndInjectCodeThunk(node.VariableName);
        }

        private void Walk(HamlNodeHtmlAttribute node)
        {
            _templateILStream.WriteStaticString(" {0}=\"", node.Name);
            this.Walk(node.Children);
            _templateILStream.WriteStaticString('"');
        }

        private void Walk(HamlNodeTag node)
        {
            _templateILStream.WriteStaticString('<');
            _templateILStream.WriteStaticString(node.NamespaceQualifiedTagName);

            // There's two ways of defining a class, so we need to reconcile them and create the string writer
            var attributes = node.Attributes.GroupBy(a => a.Name);
            foreach (var attrGroup in attributes)
            {
                _templateILStream.WriteStaticString(" {0}=\"", attrGroup.Key);

                bool leadingSpace = false;
                var values = attrGroup.Select(n => n.Child).OfType<HamlNodeTextLiteral>().OrderBy(n => n.Content).Select(n => n.Content);
                if (values.Any())
                {
                    leadingSpace = true;
                    _templateILStream.WriteStaticString(values.Aggregate((accum, val) => string.Format("{0} {1}", accum, val)));
                }

                var thunkValues = attrGroup.Select(n => n.Child).OfType<HamlNodeEval>();
                if (thunkValues.Any())
                {
                    if (leadingSpace)
                    {
                        _templateILStream.WriteStaticString(' ');
                        leadingSpace = false;
                    }
                    foreach (var value in thunkValues)
                    {
                        if (leadingSpace)
                        {
                            _templateILStream.WriteStaticString(' ');
                        }
                        CompileAndInjectCodeThunk(value.Content);
                        leadingSpace = true;
                    }
                }

                _templateILStream.WriteStaticString('"');
            }
            if (node.Children.Count > 0)
            {
                _templateILStream.WriteStaticString('>');
                this.Walk(node.Children);
                _templateILStream.WriteStaticString("</");
                _templateILStream.WriteStaticString(node.NamespaceQualifiedTagName);
            }
            else if (!node.IsSelfClosing)
            {
                _templateILStream.WriteStaticString('/');
            }

            _templateILStream.WriteStaticString('>');
        }

        public Delegate Compile()
        {
            var compilationTargetType = _codeClassBuilder.CompileClass();
            return _templateILStream.Build(compilationTargetType).Compile();
        }

        private string CompileAndInjectCodeThunk(string content)
        {
            string methodName = CompileCodeThunk(content, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)));

            _templateILStream.CallThunkMethod(methodName);

            return methodName;
        }

        private string CompileCodeThunk(string content, TypeSyntax returnType)
        {
            return _codeClassBuilder.NewCodeBlock(content, returnType);
        }
    }
}
