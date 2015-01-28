using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Lambda2Js
{
    public class JavascriptCompilerExpressionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression contextParameter;
        private readonly IEnumerable<JavascriptConversionExtension> extensions;
        private readonly JavascriptWriter result = new JavascriptWriter();

        public JavascriptCompilerExpressionVisitor(
            ParameterExpression contextParameter,
            IEnumerable<JavascriptConversionExtension> extensions)
        {
            this.contextParameter = contextParameter;
            this.extensions = extensions;
        }

        public string Result
        {
            get { return this.result.ToString(); }
        }

        public override Expression Visit(Expression node)
        {
            var context = new JavascriptConversionContext(node, this, this.result);
            foreach (var each in this.extensions)
            {
                each.ConvertToJavascript(context);

                if (context.gotWriter && context.Node != node)
                    throw new Exception(
                        "Cannot both write and return a new node. Either write javascript code, or return a new node.");

                if (context.preventDefault || context.gotWriter)
                {
                    // canceling any further action with the current node
                    return node;
                }

                if (context.Node != node)
                {
                    // a new node must be completelly revisited
                    return this.Visit(context.Node);
                }
            }

            // nothing happened, continue to the default conversion behavior
            return base.Visit(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                {
                    this.Visit(node.Left);
                    this.result.Append('[');
                    using (this.result.Operation(0))
                        this.Visit(node.Right);
                    this.result.Append(']');
                    return node;
                }
            }

            using (this.result.Operation(node))
            {
                this.Visit(node.Left);
                this.result.WriteOperator(node.NodeType);
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
            if (TypeHelpers.IsNumericType(node.Type))
            {
                using (this.result.Operation(JavascriptOperationTypes.Literal))
                    this.result.Append(Convert.ToString(node.Value, CultureInfo.InvariantCulture));
            }
            else if (node.Type == typeof(string))
            {
                using (this.result.Operation(JavascriptOperationTypes.Literal))
                    this.WriteStringLiteral((string)node.Value);
            }
            else if (node.Type == typeof(Regex))
            {
                using (this.result.Operation(JavascriptOperationTypes.Literal))
                {
                    this.result.Append('/');
                    this.result.Append(node.Value);
                    this.result.Append("/g");
                }
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
            using (this.result.Operation(node))
            {
                this.result.Append("function(");

                var posStart = this.result.Length;
                foreach (var param in node.Parameters)
                {
                    if (param.IsByRef)
                        throw new NotSupportedException("Cannot pass by ref in javascript.");

                    if (this.result.Length > posStart)
                        this.result.Append(',');

                    this.result.Append(param.Name);
                }

                this.result.Append("){");
                if (node.ReturnType != typeof(void))
                    using (this.result.Operation(0))
                    {
                        this.result.Append("return ");
                        this.Visit(node.Body);
                    }

                this.result.Append(";}");
                return node;
            }
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            // Detecting a new dictionary
            if (TypeHelpers.IsDictionaryType(node.Type))
            {
                using (this.result.Operation(0))
                {
                    this.result.Append('{');

                    var posStart = this.result.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.result.Length > posStart)
                            this.result.Append(',');

                        if (init.Arguments.Count != 2)
                            throw new NotSupportedException(
                                "Objects can only be initialized with methods that receive pairs: key -> name");

                        var nameArg = init.Arguments[0];
                        if (nameArg.NodeType != ExpressionType.Constant || nameArg.Type != typeof(string))
                            throw new NotSupportedException("The key of an object must be a constant string value");

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
            if (TypeHelpers.IsListType(node.Type))
            {
                using (this.result.Operation(0))
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
            if (node.Expression == null)
            {
                var decl = node.Member.DeclaringType;
                if (decl == typeof(string))
                {
                    if (node.Member.Name == "Empty")
                    {
                        using (this.result.Operation(JavascriptOperationTypes.Literal))
                            this.result.Append("\"\"");
                        return node;
                    }
                }
            }

            using (this.result.Operation(node))
            {
                var pos = this.result.Length;
                if (node.Expression == null)
                {
                    var decl = node.Member.DeclaringType;
                    if (decl != null)
                    {
                        this.result.Append(decl.FullName);
                        this.result.Append('.');
                        this.result.Append(decl.Name);
                    }
                }
                else if (node.Expression != this.contextParameter)
                    this.Visit(node.Expression);

                if (this.result.Length > pos)
                    this.result.Append('.');

                var propInfo = node.Member as PropertyInfo;
                if (propInfo != null
                    && propInfo.DeclaringType != null
                    && node.Type == typeof(int)
                    && node.Member.Name == "Count"
                    && TypeHelpers.IsListType(propInfo.DeclaringType))
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
            using (this.result.Operation(node))
            {
                var isPostOp = JsOperationHelper.IsPostfixOperator(node.NodeType);

                if (!isPostOp)
                    this.result.WriteOperator(node.NodeType);
                this.Visit(node.Operand);
                if (isPostOp)
                    this.result.WriteOperator(node.NodeType);

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
            using (this.result.Operation(0))
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
            // Detecting inlineable objects
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (node.Members != null && node.Members.Count > 0)
            {
                using (this.result.Operation(0))
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

            if (node.Type == typeof(Regex))
            {
                var lambda = Expression.Lambda<Func<Regex>>(node);

                // if all parameters are constant
                if (node.Arguments.All(a => a.NodeType == ExpressionType.Constant))
                {
                    this.result.Append('/');

                    var pattern = (string)((ConstantExpression)node.Arguments[0]).Value;
                    this.result.Append(pattern);
                    var args = node.Arguments.Count;

                    this.result.Append('/');
                    this.result.Append('g');
                    RegexOptions options = 0;
                    if (args == 2)
                    {
                        options = (RegexOptions)((ConstantExpression)node.Arguments[1]).Value;

                        if ((options & RegexOptions.IgnoreCase) != 0)
                            this.result.Append('i');
                        if ((options & RegexOptions.Multiline) != 0)
                            this.result.Append('m');
                    }

                    var ecmaRegex = new Regex(pattern, options | RegexOptions.ECMAScript);
                }
                else
                {
                    using (this.result.Operation(JavascriptOperationTypes.New))
                    {
                        this.result.Append("new RegExp(");

                        using (this.result.Operation(JavascriptOperationTypes.ParamIsolatedLhs))
                            this.Visit(node.Arguments[0]);

                        var args = node.Arguments.Count;

                        if (args == 2)
                        {
                            this.result.Append(',');

                            var optsConst = node.Arguments[1] as ConstantExpression;
                            if (optsConst == null)
                                throw new NotSupportedException("The options parameter of a Regex must be constant");

                            var options = (RegexOptions)optsConst.Value;

                            this.result.Append('\'');
                            this.result.Append('g');
                            if ((options & RegexOptions.IgnoreCase) != 0)
                                this.result.Append('i');
                            if ((options & RegexOptions.Multiline) != 0)
                                this.result.Append('m');
                            this.result.Append('\'');
                        }

                        this.result.Append(')');
                    }
                }
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsSpecialName)
            {
                if (node.Method.Name == "get_Item")
                {
                    using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                    {
                        this.Visit(node.Object);
                        this.result.Append('[');

                        using (this.result.Operation(0))
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
                    using (this.result.Operation(0))
                    {
                        using (this.result.Operation(JavascriptOperationTypes.AssignRhs))
                        {
                            using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                            {
                                this.Visit(node.Object);
                                this.result.Append('[');

                                using (this.result.Operation(0))
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
                if (node.Method.DeclaringType != null
                    && (node.Method.Name == "ContainsKey"
                        && TypeHelpers.IsDictionaryType(node.Method.DeclaringType)))
                {
                    using (this.result.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.result.Append(".hasOwnProperty(");
                        using (this.result.Operation(0))
                            this.Visit(node.Arguments.Single());
                        this.result.Append(')');
                        return node;
                    }
                }
            }

            if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name == "Contains")
                {
                    using (this.result.Operation(JavascriptOperationTypes.Comparison))
                    {
                        using (this.result.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.result.Append(".indexOf(");
                            using (this.result.Operation(0))
                            {
                                var posStart = this.result.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.result.Length > posStart)
                                        this.result.Append(',');
                                    this.Visit(arg);
                                }
                            }

                            this.result.Append(')');
                        }

                        this.result.Append(">=0");
                        return node;
                    }
                }
            }

            if (node.Method.Name == "ToString" && node.Type == typeof(string) && node.Object != null)
            {
                string methodName = null;
                if (node.Arguments.Count == 0 || typeof(IFormatProvider).IsAssignableFrom(node.Arguments[0].Type))
                {
                    methodName = "toString()";
                }
                else if (TypeHelpers.IsNumericType(node.Object.Type)
                         && node.Arguments.Count >= 1
                         && node.Arguments[0].Type == typeof(string)
                         && node.Arguments[0].NodeType == ExpressionType.Constant)
                {
                    var str = (string)((ConstantExpression)node.Arguments[0]).Value;
                    var match = Regex.Match(str, @"^([DEFGNX])(\d*)$", RegexOptions.IgnoreCase);
                    var f = match.Groups[1].Value.ToUpper();
                    var n = match.Groups[2].Value;
                    if (f == "D")
                        methodName = "toString()";
                    else if (f == "E")
                        methodName = "toExponential(" + n + ")";
                    else if (f == "F" || f == "G")
                        methodName = "toFixed(" + n + ")";
                    else if (f == "N")
                        methodName = "toLocaleString()";
                    else if (f == "X")
                        methodName = "toString(16)";
                }

                if (methodName != null)
                    using (this.result.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.result.AppendFormat(".{0}", methodName);
                        return node;
                    }
            }

            if (!node.Method.IsStatic)
                throw new NotSupportedException("Can only convert static methods.");

            using (this.result.Operation(JavascriptOperationTypes.Call))
                if (node.Method.DeclaringType != null)
                {
                    this.result.Append(node.Method.DeclaringType.FullName);
                    this.result.Append('.');
                    this.result.Append(node.Method.Name);
                    this.result.Append('(');

                    var posStart = this.result.Length;
                    using (this.result.Operation(0))
                        foreach (var arg in node.Arguments)
                        {
                            if (this.result.Length != posStart)
                                this.result.Append(',');

                            this.Visit(arg);
                        }

                    this.result.Append(')');

                    return node;
                }

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
    }

    internal static class TypeHelpers
    {
        private static readonly Type[] numTypes = new[]
            {
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(short),
                typeof(byte),
                typeof(sbyte),
                typeof(float),
                typeof(double),
                typeof(decimal)
            };

        public static bool IsNumericType(Type type)
        {
            return Array.IndexOf(numTypes, type) >= 0;
        }

        public static bool IsDictionaryType([NotNull] Type type)
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

        public static bool IsListType([NotNull] Type type)
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

        public static bool IsEnumerableType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return true;

            if (type.IsGenericType)
            {
                var generic = type.GetGenericTypeDefinition();
                if (typeof(IEnumerable<>).IsAssignableFrom(generic))
                    return true;
            }

            return false;
        }
    }
}