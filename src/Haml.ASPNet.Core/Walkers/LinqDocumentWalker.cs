using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web.NHaml.Parser;
using System.Web.NHaml.Parser.Rules;
using System.Reflection;
using System.Text;

namespace NHaml.Walkers
{
    public class LinqDocumentWalker
    {
        private IList<Expression> _expressions;
        private ParameterExpression _textWriterParameter;
        private ParameterExpression _modelParameter;
        private MethodInfo writeMethodInfo;
        private StringBuilder textRun;
        private Type modelType;

        public LinqDocumentWalker(Type modelType)
        {
            _expressions = new List<Expression>();
            _textWriterParameter = Expression.Parameter(typeof(TextWriter));
            _modelParameter = Expression.Parameter(modelType);
            writeMethodInfo = typeof(TextWriter).GetMethod("Write", new Type[] { typeof(string) });
            textRun = new StringBuilder();
            this.modelType = modelType;
        }

        public void Walk(HamlDocument document)
        {
            this.Walk(document.Children);
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
                    this.Walk(node.Children);
                if (nodeType == typeof(HamlNodeTextContainer))
                    this.Walk(node.Children);
                if (nodeType == typeof(HamlNodeTag))
                    this.Walk((HamlNodeTag)node);
                if (nodeType == typeof(HamlNodeHtmlComment))
                    continue;
                if (nodeType == typeof(HamlNodeHamlComment))
                    continue;
                if (nodeType == typeof(HamlNodeEval))
                    continue;
                if (nodeType == typeof(HamlNodeCode))
                    continue;
                if (nodeType == typeof(HamlNodeTextLiteral))
                    textRun.Append(((HamlNodeTextLiteral)node).Content);
                if (nodeType == typeof(HamlNodeTextVariable))
                    this.Walk((HamlNodeTextVariable)node);
                if (nodeType == typeof(HamlNodeDocType))
                    this.Walk((HamlNodeDocType)node);
                if (nodeType == typeof(HamlNodeHtmlAttribute))
                    this.Walk((HamlNodeHtmlAttribute)node);
                if (nodeType == typeof(HamlNodePartial))
                    continue;
            }
        }

        private void Walk(HamlNodeDocType docType)
        {
            textRun.Append("<!DOCTYPE html>");
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
            else
            {
                textRun.Append("/");
            }

            textRun.Append(">");
        }

        public Delegate Compile()
        {
            FlushStringRun();
            var method = Expression.Block(_expressions);
            var lambda = Expression.Lambda(method, _textWriterParameter, _modelParameter);
            Delegate output = lambda.Compile();

            return output;
        }

        private void FlushStringRun()
        {
            _expressions.Add(Expression.Call(_textWriterParameter, writeMethodInfo, Expression.Constant(textRun.ToString())));
            textRun.Clear();
        }
    }
}
