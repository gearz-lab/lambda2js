using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Masb.ExpressionTreeToJavascript
{
    public class ExpressionTreeToJavascriptVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression contextParameter;
        private readonly StringBuilder result = new StringBuilder();
        private readonly List<JavascriptOperationTypes> operandTypes = new List<JavascriptOperationTypes>();

        public ExpressionTreeToJavascriptVisitor(ParameterExpression contextParameter)
        {
            this.contextParameter = contextParameter;
        }

        public string Result
        {
            get { return this.result.ToString(); }
        }

        private PrecedenceController Operation(JavascriptOperationTypes op)
        {
            return new PrecedenceController(this.result, this.operandTypes, op);
        }

        private IDisposable Operation(Expression node)
        {
            var op = JsOperationHelper.GetJsOperator(node.NodeType);
            if (op == JavascriptOperationTypes.NoOp)
                return null;

            return new PrecedenceController(this.result, this.operandTypes, op);
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                using (this.Operation(JavascriptOperationTypes.IndexerProperty))
                {
                    this.Visit(node.Left);
                    this.result.Append('[');
                    using (this.Operation(0))
                        this.Visit(node.Right);
                    this.result.Append(']');
                    return node;
                }
            }

            using (this.Operation(node))
            {
                this.Visit(node.Left);
                JsOperationHelper.WriteOperator(this.result, node.NodeType);
                this.Visit(node.Right);
            }

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
                this.result.Append(Convert.ToString(node.Value, CultureInfo.InvariantCulture));
            }
            else if (node.Type == typeof(string))
            {
                this.WriteStringLiteral((string)node.Value);
            }
            else if (node.Type == typeof(Regex))
            {
                this.result.Append('/');
                this.result.Append(node.Value);
                this.result.Append("/g");
            }
            else if (node.Value == null)
            {
                this.result.Append("null");
            }

            return node;
        }

        private void WriteStringLiteral(string str)
        {
            this.result.Append('"');
            this.result.Append(
                str
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t")
                    .Replace("\0", "\\0")
                    .Replace("\"", "\\\""));
            this.result.Append('"');
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
            using (this.Operation(node))
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

                this.result.Append("){");
                if (node.ReturnType != typeof(void))
                    using (this.Operation(0))
                    {
                        this.result.Append("return ");
                        this.Visit(node.Body);
                    }

                this.result.Append(";}");
                return node;
            }
        }

        private static bool IsDictionaryType([NotNull] Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (typeof(IDictionary).IsAssignableFrom(type))
                return true;

            if (type.IsGenericType)
            {
                var generic = type.GetGenericTypeDefinition();
                if (typeof(IDictionary<,>).IsAssignableFrom(generic))
                    return true;
            }

            return false;
        }

        private static bool IsListType([NotNull] Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (typeof(ICollection).IsAssignableFrom(type))
                return true;

            if (type.IsGenericType)
            {
                var generic = type.GetGenericTypeDefinition();
                if (typeof(ICollection<>).IsAssignableFrom(generic))
                    return true;
            }

            return false;
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            // Detecting a new dictionary
            if (IsDictionaryType(node.Type))
            {
                using (this.Operation(0))
                {
                    this.result.Append('{');

                    var posStart = this.result.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.result.Length > posStart)
                            this.result.Append(',');

                        if (init.Arguments.Count != 2)
                            throw new Exception(
                                "Objects can only be initialized with methods that receive pairs: key -> name");

                        var nameArg = init.Arguments[0];
                        if (nameArg.NodeType != ExpressionType.Constant || nameArg.Type != typeof(string))
                            throw new Exception("The key of an object must be a constant string value.");

                        var name = (string)((ConstantExpression)nameArg).Value;
                        if (Regex.IsMatch(name, @"^\w[\d\w]*$"))
                            this.result.Append(name);
                        else
                            this.WriteStringLiteral(name);

                        this.result.Append(':');
                        this.Visit(init.Arguments[1]);
                    }

                    this.result.Append('}');
                }

                return node;
            }

            // Detecting a new dictionary
            if (IsListType(node.Type))
            {
                using (this.Operation(0))
                {
                    this.result.Append('[');

                    var posStart = this.result.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.result.Length > posStart)
                            this.result.Append(',');

                        if (init.Arguments.Count != 1)
                            throw new Exception(
                                "Arrays can only be initialized with methods that receive a single parameter for the value");

                        this.Visit(init.Arguments[0]);
                    }

                    this.result.Append(']');
                }

                return node;
            }

            return node;
        }

        protected override Expression VisitLoop(LoopExpression node)
        {
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            using (this.Operation(node))
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
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            using (this.Operation(node))
            {
                var isPostOp = JsOperationHelper.IsPostfixOperator(node.NodeType);

                if (!isPostOp)
                    JsOperationHelper.WriteOperator(this.result, node.NodeType);
                this.Visit(node.Operand);
                if (isPostOp)
                    JsOperationHelper.WriteOperator(this.result, node.NodeType);

                return node;
            }
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
            using (this.Operation(0))
            {
                this.result.Append('[');

                var posStart = this.result.Length;
                foreach (var item in node.Expressions)
                {
                    if (this.result.Length > posStart)
                        this.result.Append(',');

                    this.Visit(item);
                }

                this.result.Append(']');
            }

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            // Detecting inline objects
            if (node.Members.Count > 0)
            {
                using (this.Operation(0))
                {
                    this.result.Append('{');

                    var posStart = this.result.Length;
                    for (int itMember = 0; itMember < node.Members.Count; itMember++)
                    {
                        var member = node.Members[itMember];
                        if (this.result.Length > posStart)
                            this.result.Append(',');

                        if (Regex.IsMatch(member.Name, @"^\w[\d\w]*$"))
                            this.result.Append(member.Name);
                        else
                            this.WriteStringLiteral(member.Name);

                        this.result.Append(':');
                        this.Visit(node.Arguments[itMember]);
                    }

                    this.result.Append('}');
                }

                return node;
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsSpecialName)
            {
                if (node.Method.Name == "get_Item")
                {
                    using (this.Operation(JavascriptOperationTypes.IndexerProperty))
                    {
                        this.Visit(node.Object);
                        this.result.Append('[');

                        using (this.Operation(0))
                        {
                            var posStart0 = this.result.Length;
                            foreach (var arg in node.Arguments)
                            {
                                if (this.result.Length != posStart0)
                                    this.result.Append(',');

                                this.Visit(arg);
                            }
                        }

                        this.result.Append(']');
                        return node;
                    }
                }

                if (node.Method.Name == "set_Item")
                {
                    using (this.Operation(0))
                    {
                        using (this.Operation(JavascriptOperationTypes.AssignRhs))
                        {
                            using (this.Operation(JavascriptOperationTypes.IndexerProperty))
                            {
                                this.Visit(node.Object);
                                this.result.Append('[');

                                using (this.Operation(0))
                                {
                                    var posStart0 = this.result.Length;
                                    foreach (var arg in node.Arguments)
                                    {
                                        if (this.result.Length != posStart0)
                                            this.result.Append(',');

                                        this.Visit(arg);
                                    }
                                }

                                this.result.Append(']');
                            }
                        }

                        this.result.Append('=');
                        this.Visit(node.Arguments.Single());
                    }

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
                    using (this.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.result.Append(".hasOwnProperty(");
                        using (this.Operation(0))
                            this.Visit(node.Arguments.Single());
                        this.result.Append(')');
                        return node;
                    }
                }
            }

            if (!node.Method.IsStatic)
                throw new Exception("Can only convert static methods.");

            using (this.Operation(JavascriptOperationTypes.Call))
            {
                this.result.Append(node.Method.DeclaringType.FullName);
                this.result.Append('.');
                this.result.Append(node.Method.Name);
                this.result.Append('(');

                var posStart = this.result.Length;
                using (this.Operation(0))
                    foreach (var arg in node.Arguments)
                    {
                        if (this.result.Length != posStart)
                            this.result.Append(',');

                        this.Visit(arg);
                    }

                this.result.Append(')');

                return node;
            }
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
    }
}
