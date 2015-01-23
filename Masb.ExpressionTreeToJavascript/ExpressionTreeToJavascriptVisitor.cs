using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Masb.ExpressionTreeToJavascript
{
    public class ExpressionTreeToJavascriptVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression contextParameter;
        private readonly StringBuilder result = new StringBuilder();

        private static readonly ExpressionType[] post = new[]
            {
                ExpressionType.ArrayLength,
                ExpressionType.PostIncrementAssign,
                ExpressionType.PostDecrementAssign,
            };

        private static readonly ExpressionType[] assign = new[]
            {
                ExpressionType.Assign,
                ExpressionType.AddAssign,
                ExpressionType.AddAssignChecked,
                ExpressionType.AndAssign,
                ExpressionType.DivideAssign,
                ExpressionType.ExclusiveOrAssign,
                ExpressionType.LeftShiftAssign,
                ExpressionType.ModuloAssign,
                ExpressionType.MultiplyAssign,
                ExpressionType.MultiplyAssignChecked,
                ExpressionType.OrAssign,
                ExpressionType.PostDecrementAssign,
                ExpressionType.PostIncrementAssign,
                ExpressionType.PowerAssign,
                ExpressionType.PreDecrementAssign,
                ExpressionType.PreIncrementAssign,
                ExpressionType.RightShiftAssign,
                ExpressionType.SubtractAssign,
                ExpressionType.SubtractAssignChecked,
            };

        public ExpressionTreeToJavascriptVisitor(ParameterExpression contextParameter)
        {
            this.contextParameter = contextParameter;
        }

        public string Result
        {
            get { return this.result.ToString(); }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                this.result.Append('(');
                this.Visit(node.Left);
                this.result.Append(')');
                this.result.Append('[');
                this.Visit(node.Right);
                this.result.Append(']');
                return node;
            }

            var isAssignOp = assign.Contains(node.NodeType);

            if (!isAssignOp)
                this.result.Append('(');
            this.Visit(node.Left);
            if (!isAssignOp)
                this.result.Append(')');

            this.WriteOperator(node.NodeType);

            this.result.Append('(');
            this.Visit(node.Right);
            this.result.Append(')');

            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            return node;
        }

        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var numTypes = new[]
                {
                    typeof(int),
                    typeof(long),
                    typeof(uint),
                    typeof(ulong),
                    typeof(short),
                    typeof(byte),
                    typeof(double)
                };

            if (numTypes.Contains(node.Type))
            {
                this.result.Append(node.Value);
            }
            else if (node.Type == typeof(string))
            {
                this.result.Append('"');
                this.result.Append(
                    ((string)node.Value)
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t")
                        .Replace("\0", "\\0")
                        .Replace("\"", "\\\""));
                this.result.Append('"');
            }
            else if (node.Type == typeof(System.Text.RegularExpressions.Regex))
            {
                this.result.Append('/');
                this.result.Append(node.Value);
                this.result.Append("/g");
            }

            return node;
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            return node;
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            return node;
        }

        protected override Expression VisitDynamic(DynamicExpression node)
        {
            return node;
        }

        protected override ElementInit VisitElementInit(ElementInit node)
        {
            return node;
        }

        protected override Expression VisitExtension(Expression node)
        {
            return node;
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            return node;
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            return node;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            return node;
        }

        protected override Expression VisitLabel(LabelExpression node)
        {
            return node;
        }

        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            this.result.Append("function(");

            var posStart = this.result.Length;
            foreach (var param in node.Parameters)
            {
                if (param.IsByRef)
                    throw new Exception("Cannot pass by ref in javascript.");

                if (this.result.Length > posStart)
                    this.result.Append(',');

                this.result.Append(param.Name);
            }

            this.result.Append("){return(");

            this.Visit(node.Body);

            this.result.Append(");}");
            return node;
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            return node;
        }

        protected override Expression VisitLoop(LoopExpression node)
        {
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var pos = this.result.Length;
            if (node.Expression != this.contextParameter)
                this.Visit(node.Expression);

            if (this.result.Length > pos)
                this.result.Append('.');

            var propInfo = node.Member as PropertyInfo;
            if (propInfo != null && node.Type == typeof(int) && node.Member.Name == "Count" &&
                (typeof(ICollection).IsAssignableFrom(propInfo.DeclaringType) ||
                 propInfo.DeclaringType.IsGenericType &&
                 typeof(ICollection<>).IsAssignableFrom(propInfo.DeclaringType.GetGenericTypeDefinition())))
            {
                this.result.Append("length");
            }
            else
            {
                this.result.Append(node.Member.Name);
            }

            return node;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var isAssignOp = assign.Contains(node.NodeType);
            var isPostOp = post.Contains(node.NodeType);

            if (!isPostOp)
                this.WriteOperator(node.NodeType);
            if (!isAssignOp)
                this.result.Append("(");
            this.Visit(node.Operand);
            if (!isAssignOp)
                this.result.Append(")");
            if (isPostOp)
                this.WriteOperator(node.NodeType);

            return node;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            return node;
        }

        protected override Expression VisitTry(TryExpression node)
        {
            return node;
        }

        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            return node;
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            return node;
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            this.result.Append(node.Name);
            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsSpecialName)
            {
                if (node.Method.Name == "get_Item")
                {
                    this.result.Append('(');
                    this.Visit(node.Object);
                    this.result.Append(')');
                    this.result.Append('[');
                    var posStart0 = this.result.Length;
                    foreach (var arg in node.Arguments)
                    {
                        if (this.result.Length != posStart0)
                            this.result.Append(',');

                        this.Visit(arg);
                    }
                    this.result.Append(']');
                    return node;
                }
            }
            else
            {
                if (node.Method.Name == "ContainsKey" &&
                (typeof(IDictionary).IsAssignableFrom(node.Method.DeclaringType) ||
                 node.Method.DeclaringType.IsGenericType &&
                 typeof(IDictionary<,>).IsAssignableFrom(node.Method.DeclaringType.GetGenericTypeDefinition())))
                {
                    this.result.Append('(');
                    this.Visit(node.Object);
                    this.result.Append(')');
                    this.result.Append(".hasOwnProperty(");
                    this.Visit(node.Arguments.Single());
                    this.result.Append(')');
                    return node;
                }
            }

            if (!node.Method.IsStatic)
                throw new Exception("Can only convert static methods.");

            this.result.AppendFormat(
                "{0}.{1}(",
                node.Method.DeclaringType.FullName,
                node.Method.Name);

            var posStart = this.result.Length;
            foreach (var arg in node.Arguments)
            {
                if (this.result.Length != posStart)
                    this.result.Append(',');

                this.Visit(arg);
            }

            this.result.Append(')');

            return node;
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            return node;
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            return node;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return node;
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            return node;
        }

        private void WriteOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    this.result.Append('+');
                    break;
                case ExpressionType.AddAssign:
                    this.result.Append("+=");
                    break;
                case ExpressionType.AddAssignChecked:
                    break;
                case ExpressionType.AddChecked:
                    break;
                case ExpressionType.And:
                    this.result.Append("&");
                    break;
                case ExpressionType.AndAlso:
                    this.result.Append("&&");
                    break;
                case ExpressionType.AndAssign:
                    this.result.Append("&=");
                    break;
                case ExpressionType.ArrayIndex:
                    break;
                case ExpressionType.ArrayLength:
                    this.result.Append(".length");
                    break;
                case ExpressionType.Assign:
                    this.result.Append("=");
                    break;
                case ExpressionType.Block:
                    break;
                case ExpressionType.Call:
                    break;
                case ExpressionType.Coalesce:
                    this.result.Append("||");
                    break;
                case ExpressionType.Conditional:
                    break;
                case ExpressionType.Constant:
                    break;
                case ExpressionType.Convert:
                    break;
                case ExpressionType.ConvertChecked:
                    break;
                case ExpressionType.DebugInfo:
                    break;
                case ExpressionType.Decrement:
                    this.result.Append("--");
                    break;
                case ExpressionType.Default:
                    break;
                case ExpressionType.Divide:
                    this.result.Append("/");
                    break;
                case ExpressionType.DivideAssign:
                    this.result.Append("/=");
                    break;
                case ExpressionType.Dynamic:
                    break;
                case ExpressionType.Equal:
                    this.result.Append("==");
                    break;
                case ExpressionType.ExclusiveOr:
                    this.result.Append("^");
                    break;
                case ExpressionType.ExclusiveOrAssign:
                    this.result.Append("^=");
                    break;
                case ExpressionType.Extension:
                    break;
                case ExpressionType.Goto:
                    break;
                case ExpressionType.GreaterThan:
                    this.result.Append(">");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    this.result.Append(">=");
                    break;
                case ExpressionType.Increment:
                    this.result.Append("++");
                    break;
                case ExpressionType.Index:
                    break;
                case ExpressionType.Invoke:
                    break;
                case ExpressionType.IsFalse:
                    break;
                case ExpressionType.IsTrue:
                    break;
                case ExpressionType.Label:
                    break;
                case ExpressionType.Lambda:
                    break;
                case ExpressionType.LeftShift:
                    break;
                case ExpressionType.LeftShiftAssign:
                    break;
                case ExpressionType.LessThan:
                    this.result.Append("<");
                    break;
                case ExpressionType.LessThanOrEqual:
                    this.result.Append("<=");
                    break;
                case ExpressionType.ListInit:
                    break;
                case ExpressionType.Loop:
                    break;
                case ExpressionType.MemberAccess:
                    break;
                case ExpressionType.MemberInit:
                    break;
                case ExpressionType.Modulo:
                    this.result.Append("%");
                    break;
                case ExpressionType.ModuloAssign:
                    this.result.Append("%=");
                    break;
                case ExpressionType.Multiply:
                    this.result.Append("*");
                    break;
                case ExpressionType.MultiplyAssign:
                    this.result.Append("*=");
                    break;
                case ExpressionType.MultiplyAssignChecked:
                    break;
                case ExpressionType.MultiplyChecked:
                    break;
                case ExpressionType.Negate:
                    this.result.Append("-");
                    break;
                case ExpressionType.NegateChecked:
                    break;
                case ExpressionType.New:
                    break;
                case ExpressionType.NewArrayBounds:
                    break;
                case ExpressionType.NewArrayInit:
                    break;
                case ExpressionType.Not:
                    this.result.Append("!");
                    break;
                case ExpressionType.NotEqual:
                    this.result.Append("!=");
                    break;
                case ExpressionType.OnesComplement:
                    this.result.Append("~");
                    break;
                case ExpressionType.Or:
                    this.result.Append("|");
                    break;
                case ExpressionType.OrAssign:
                    this.result.Append("|=");
                    break;
                case ExpressionType.OrElse:
                    this.result.Append("||");
                    break;
                case ExpressionType.Parameter:
                    break;
                case ExpressionType.PostDecrementAssign:
                    break;
                case ExpressionType.PostIncrementAssign:
                    break;
                case ExpressionType.Power:
                    break;
                case ExpressionType.PowerAssign:
                    break;
                case ExpressionType.PreDecrementAssign:
                    this.result.Append("--");
                    break;
                case ExpressionType.PreIncrementAssign:
                    this.result.Append("++");
                    break;
                case ExpressionType.Quote:
                    break;
                case ExpressionType.RightShift:
                    break;
                case ExpressionType.RightShiftAssign:
                    break;
                case ExpressionType.RuntimeVariables:
                    break;
                case ExpressionType.Subtract:
                    this.result.Append("-");
                    break;
                case ExpressionType.SubtractAssign:
                    this.result.Append("-=");
                    break;
                case ExpressionType.SubtractAssignChecked:
                    this.result.Append("--");
                    break;
                case ExpressionType.SubtractChecked:
                    this.result.Append("-");
                    break;
                case ExpressionType.Switch:
                    break;
                case ExpressionType.Throw:
                    break;
                case ExpressionType.Try:
                    break;
                case ExpressionType.TypeAs:
                    break;
                case ExpressionType.TypeEqual:
                    break;
                case ExpressionType.TypeIs:
                    break;
                case ExpressionType.UnaryPlus:
                    break;
                case ExpressionType.Unbox:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
