﻿using Microsoft.CodeAnalysis;
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
        private ITemplateRenderer _templateILStream;
        private HamlCodeHostBuilder _codeClassBuilder;
        private TemplateRenderContext context;

        public LinqDocumentWalker(Type modelType)
        {
            _templateILStream = _codeClassBuilder = new HamlCodeHostBuilder(modelType);
        }

        private void Walk(HamlDocument document)
        {
            Walk(document.Children);
        }

        public void Render(TemplateRenderContext context)
        {
            this.context = context;
            Walk(context.LayoutRoot);
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
                    _templateILStream.Write(((HamlNodeTextLiteral)node).Content);
                if (nodeType == typeof(HamlNodeTextVariable))
                    Walk((HamlNodeTextVariable)node);
                if (nodeType == typeof(HamlNodeDocType))
                    Walk((HamlNodeDocType)node);
                if (nodeType == typeof(HamlNodePartial))
                    Walk((HamlNodePartial)node);
            }
        }

        private void Walk(HamlNodeDocType docType)
        {
            _templateILStream.Write("<!DOCTYPE html>");
        }

        private void Walk(HamlNodePartial partial)
        {
            var content = partial.Content.Trim();
            if (content == "yield")
            {
                Walk(context.MainTemplate);
            }
            else if (content.StartsWith("render "))
            {
                int start = content.IndexOf('\'') + 1;
                string template = content.Substring(start, content.LastIndexOf('\'') - start);
                if (!template.EndsWith(".haml"))
                {
                    template += ".haml";
                }
                template = "_" + template;
                Walk(context.GetTemplate(template));
            }
        }

        private void Walk(HamlNodeEval node)
        {
            _codeClassBuilder.PrintExpressionResult(node.Content);
        }

        private void Walk(HamlNodeCode node)
        {
            var content = node.Content.Trim();
            // Conditionals require special logic since the parser doesn't yet extract it for us
            if (content.StartsWith("if"))
            {
                int start = content.IndexOf('(') + 1;
                string expression = content.Substring(start, content.Length - start - 1);

                _templateILStream.ConditionalBegin();
                Walk(node.Children);
                _templateILStream.ConditionalEnd(expression);
            }
            else if (content == "else")
            {
                _templateILStream.ConditionalElseBegin();
                Walk(node.Children);
                _templateILStream.ConditionalElseEnd();
            }
            else
            {
                _templateILStream.InlineCodeExpression(content);
            }
        }

        private void Walk(HamlNodeTextVariable node)
        {
            CompileAndInjectCodeThunk(node.VariableName);
        }

        private void Walk(HamlNodeTag node)
        {
            _templateILStream.Write('<');
            _templateILStream.Write(node.NamespaceQualifiedTagName);

            var attributes = node.Attributes.OrderBy(a => a.Name).GroupBy(a => a.Name);
            foreach (var attrGroup in attributes)
            {
                _templateILStream.Write(" {0}=\"", attrGroup.Key);

                bool leadingSpace = false;
                var values = attrGroup.Select(n => n.Child).OfType<HamlNodeTextLiteral>().OrderBy(n => n.Content).Select(n => n.Content);
                if (values.Any())
                {
                    leadingSpace = true;
                    _templateILStream.Write(values.Aggregate((accum, val) => string.Format("{0} {1}", accum, val)));
                }

                var thunkValues = attrGroup.Select(n => n.Child).OfType<HamlNodeEval>();
                if (thunkValues.Any())
                {
                    if (leadingSpace)
                    {
                        _templateILStream.Write(' ');
                        leadingSpace = false;
                    }
                    foreach (var value in thunkValues)
                    {
                        if (leadingSpace)
                        {
                            _templateILStream.Write(' ');
                        }
                        _codeClassBuilder.PrintExpressionResult(value.Content);
                        leadingSpace = true;
                    }
                }

                _templateILStream.Write('"');
            }
            if (node.Children.Count > 0)
            {
                _templateILStream.Write('>');
                this.Walk(node.Children);
                _templateILStream.Write("</");
                _templateILStream.Write(node.NamespaceQualifiedTagName);
            }
            else if (!node.IsSelfClosing)
            {
                _templateILStream.Write('/');
            }

            _templateILStream.Write('>');
        }

        public Type Compile()
        {
            return _codeClassBuilder.Compile();
        }

        private string CompileAndInjectCodeThunk(string content)
        {
            string methodName = CompileCodeThunk(content, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)));

            _codeClassBuilder.CallMethod(methodName);

            return methodName;
        }

        private string CompileCodeThunk(string content, TypeSyntax returnType)
        {
            return _codeClassBuilder.NewCodeBlock(content, returnType);
        }
    }
}
