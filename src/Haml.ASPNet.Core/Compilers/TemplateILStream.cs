using Haml.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using NHaml.Walkers.Exceptions;
using NHaml.Walkers.IntermediateNodes;
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

namespace Haml.Compiling
{
    public class TemplateILStream
    {
        private ParameterExpression _textWriterParameter;
        private ParameterExpression _modelParameter;
        private MethodInfo writeMethodInfo;
        private StringBuilder textRun;
        private Type modelType;
        private Stack<IList<IIntermediateNode>> nodes;
        private Type compilationTargetType;
        private ParameterExpression baseClassVariable;

        [DebuggerDisplay("Late-bound method: {MethodName}()")]
        private class LateBoundMethodCall : IIntermediateNode
        {
            private TemplateILStream walker;

            public LateBoundMethodCall(TemplateILStream walker, string methodName)
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
            private TemplateILStream walker;
            private IEnumerable<IIntermediateNode> ifBlock;

            public ConditionalExpression(TemplateILStream walker, string methodName, IEnumerable<IIntermediateNode> ifBlock)
            {
                this.walker = walker;
                this.methodName = methodName;
                this.ifBlock = ifBlock;
            }

            public IEnumerable<IIntermediateNode> ElseBlock
            {
                get;
                set;
            }

            private Expression ReduceNodeSet(IEnumerable<IIntermediateNode> nodes)
            {
                var block = nodes.Select(n => n.Build());
                return block.Count() > 1 ? Expression.Block(block) : block.First();
            }

            public Expression Build()
            {
                MethodInfo evalMethod = walker.compilationTargetType.GetMethod(methodName);
                var conditional = Expression.Call(evalMethod, walker._modelParameter);
                if (ElseBlock != null)
                {
                    var elseInnerBlock = ElseBlock.Select(n => n.Build());

                    return Expression.IfThenElse(conditional, ReduceNodeSet(ifBlock), ReduceNodeSet(ElseBlock));
                }
                else
                {
                    return Expression.IfThen(conditional, ReduceNodeSet(ifBlock));
                }
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

        public TemplateILStream(Type modelType)
        {
            _textWriterParameter = Expression.Parameter(typeof(TextWriter));
            _modelParameter = Expression.Parameter(modelType);
            writeMethodInfo = typeof(TextWriter).GetMethod("Write", new Type[] { typeof(string) });
            textRun = new StringBuilder();
            nodes = new Stack<IList<IIntermediateNode>>();
            nodes.Push(new List<IIntermediateNode>());
            baseClassVariable = Expression.Variable(typeof(BaseViewClass));
            Nodes.Add(new StaticExpression(baseClassVariable));
        }

        private IList<IIntermediateNode> Nodes
        {
            get
            {
                return nodes.Peek();
            }
        }

        public void WriteStaticString(string content)
        {
            textRun.Append(content);
        }

        public void WriteStaticString(char content)
        {
            textRun.Append(content);
        }

        public void WriteStaticString(string format, params object[] formats)
        {
            textRun.AppendFormat(format, formats);
        }

        public void ConditionalBegin()
        {
            FlushStringRun();
            nodes.Push(new List<IIntermediateNode>());
        }

        public void ConditionalElseBegin()
        {
            ConditionalBegin();
        }
        
        public void ConditionalElseEnd()
        {
            FlushStringRun();
            var elseNodes = nodes.Pop();
            var lastNode = Nodes.Last() as ConditionalExpression;
            if (lastNode == null)
            {
                throw new Exception("An else block must directly follow an if block");
            }
            lastNode.ElseBlock = elseNodes;
        }

        public void CallThunkMethod(string methodName)
        {
            FlushStringRun();
            Nodes.Add(new LateBoundMethodCall(this, methodName));
        }

        public void ConditionalEnd(string methodName)
        {
            FlushStringRun();
            var ifNodes = nodes.Pop();

            Nodes.Add(new ConditionalExpression(this, methodName, ifNodes));
        }

        public LambdaExpression Build(Type compilationTargetType)
        {
            if (nodes.Count != 1)
            {
                throw new Exception("HAML node stack misaligned. Expected only 1 root node.");
            }
            FlushStringRun();
            this.compilationTargetType = compilationTargetType;
            var block = Expression.Block(Nodes.Select(n => n.Build()));
            return Expression.Lambda(block, _textWriterParameter, _modelParameter);
        }

        private void FlushStringRun()
        {
            if (textRun.Length == 0)
            {
                return;
            }
            Nodes.Add(new StaticExpression(Expression.Call(_textWriterParameter, writeMethodInfo, Expression.Constant(textRun.ToString()))));
            textRun.Clear();
        }
    }
}
