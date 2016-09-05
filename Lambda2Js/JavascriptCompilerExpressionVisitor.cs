using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

#pragma warning disable 1591
namespace Lambda2Js
{
    /// <summary>
    /// Expression visitor that converts each node to JavaScript code.
    /// </summary>
    public class JavascriptCompilerExpressionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression contextParameter;
        private readonly IEnumerable<JavascriptConversionExtension> extensions;
        private readonly JavascriptWriter result = new JavascriptWriter();
        private List<string> usedScopeMembers;

        public JavascriptCompilerExpressionVisitor(
            ParameterExpression contextParameter,
            IEnumerable<JavascriptConversionExtension> extensions,
            [NotNull] JavascriptCompilationOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.Options = options;
            this.contextParameter = contextParameter;
            this.extensions = extensions;
        }

        /// <summary>
        /// Gets the user options.
        /// </summary>
        [NotNull]
        public JavascriptCompilationOptions Options { get; private set; }

        /// <summary>
        /// Gets the resulting JavaScript code.
        /// </summary>
        public string Result => this.result.ToString();

        /// <summary>
        /// Gets the scope names that were used from the scope parameter.
        /// </summary>
        [CanBeNull]
        public string[] UsedScopeMembers => this.usedScopeMembers?.ToArray();

        public override Expression Visit(Expression node)
        {
            var context = new JavascriptConversionContext(node, this, this.result, this.Options);
            foreach (var each in this.extensions)
            {
                each.ConvertToJavascript(context);

                #region Supported will be removed in v2
#if V1
                if (context.gotWriter && context.Node != node)
                    throw new Exception(
                        "Cannot both write and return a new node. Either write javascript code, or return a new node.");
#endif
                #endregion
                if (context.preventDefault || context.gotWriter)
                {
                    // canceling any further action with the current node
                    return node;
                }

                #region Supported will be removed in v2
#if V1
                if (context.Node != node)
                {
                    // a new node must be completelly revisited
                    return this.Visit(context.Node);
                }
#endif
                #endregion
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
                    this.result.Write('[');
                    using (this.result.Operation(0))
                        this.Visit(node.Right);
                    this.result.Write(']');
                    return node;
                }
            }

            using (this.result.Operation(node))
            {
                this.Visit(node.Left);
                this.result.WriteOperator(node.NodeType, node.Type);
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
                    this.result.Write(Convert.ToString(node.Value, CultureInfo.InvariantCulture));
            }
            else if (node.Type == typeof(string))
            {
                using (this.result.Operation(JavascriptOperationTypes.Literal))
                    this.WriteStringLiteral((string)node.Value);
            }
            else if (node.Value == null)
            {
                this.result.Write("null");
            }
            else if (node.Type.IsEnum)
            {
                using (this.result.Operation(JavascriptOperationTypes.Literal))
                {
                    var underlyingType = Enum.GetUnderlyingType(node.Type);
                    this.result.Write(Convert.ChangeType(node.Value, underlyingType, CultureInfo.InvariantCulture));
                }
            }
            else if (node.Type == typeof(Regex))
            {
                using (this.result.Operation(JavascriptOperationTypes.Literal))
                {
                    this.result.Write('/');
                    this.result.Write(node.Value);
                    this.result.Write("/g");
                }
            }
            else if (node.Type.IsClosureRootType())
            {
                // do nothing, this is a reference to the closure root object
            }
            else
                throw new NotSupportedException("The used constant value is not supported: `" + node + "` (" + node.Type.Name + ")");

            return node;
        }

        private void WriteStringLiteral(string str)
        {
            this.result.Write('"');
            this.result.Write(
                str
                    .Replace("\\", "\\\\")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t")
                    .Replace("\0", "\\0")
                    .Replace("\"", "\\\""));

            this.result.Write('"');
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
            // Ecma script 6+: rendering arrow function syntax
            // Other: rendering inline annonimous function
            if (this.Options.ScriptVersion.Supports(JavascriptSyntax.ArrowFunction))
            {
                // Arrow function syntax and precedence works mostly like an assignment.
                using (this.result.Operation(JavascriptOperationTypes.AssignRhs))
                {
                    var pars = node.Parameters;
                    if (pars.Count != 1)
                        this.result.Write("(");

                    var posStart = this.result.Length;
                    foreach (var param in node.Parameters)
                    {
                        if (param.IsByRef)
                            throw new NotSupportedException("Cannot pass by ref in javascript.");

                        if (this.result.Length > posStart)
                            this.result.Write(',');

                        this.result.Write(param.Name);
                    }

                    if (pars.Count != 1)
                        this.result.Write(")");

                    this.result.Write("=>");

                    using (this.result.Operation(JavascriptOperationTypes.ParamIsolatedLhs))
                    {
                        this.Visit(node.Body);
                    }
                }
            }
            else
            {
                using (this.result.Operation(node))
                {
                    this.result.Write("function(");

                    var posStart = this.result.Length;
                    foreach (var param in node.Parameters)
                    {
                        if (param.IsByRef)
                            throw new NotSupportedException("Cannot pass by ref in javascript.");

                        if (this.result.Length > posStart)
                            this.result.Write(',');

                        this.result.Write(param.Name);
                    }

                    this.result.Write("){");
                    if (node.ReturnType != typeof(void))
                        using (this.result.Operation(0))
                        {
                            this.result.Write("return ");
                            this.Visit(node.Body);
                        }
                    else
                        using (this.result.Operation(0))
                        {
                            this.Visit(node.Body);
                        }

                    this.result.Write(";}");
                }
            }
            return node;
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            // Detecting a new dictionary
            if (TypeHelpers.IsDictionaryType(node.Type))
            {
                using (this.result.Operation(0))
                {
                    this.result.Write('{');

                    var posStart = this.result.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.result.Length > posStart)
                            this.result.Write(',');

                        if (init.Arguments.Count != 2)
                            throw new NotSupportedException(
                                "Objects can only be initialized with methods that receive pairs: key -> name");

                        var nameArg = init.Arguments[0];
                        if (nameArg.NodeType != ExpressionType.Constant || nameArg.Type != typeof(string))
                            throw new NotSupportedException("The key of an object must be a constant string value");

                        var name = (string)((ConstantExpression)nameArg).Value;
                        if (Regex.IsMatch(name, @"^\w[\d\w]*$"))
                            this.result.Write(name);
                        else
                            this.WriteStringLiteral(name);

                        this.result.Write(':');
                        this.Visit(init.Arguments[1]);
                    }

                    this.result.Write('}');
                }

                return node;
            }

            // Detecting a new dictionary
            if (TypeHelpers.IsListType(node.Type))
            {
                using (this.result.Operation(0))
                {
                    this.result.Write('[');

                    var posStart = this.result.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.result.Length > posStart)
                            this.result.Write(',');

                        if (init.Arguments.Count != 1)
                            throw new Exception(
                                "Arrays can only be initialized with methods that receive a single parameter for the value");

                        this.Visit(init.Arguments[0]);
                    }

                    this.result.Write(']');
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
                            this.result.Write("\"\"");
                        return node;
                    }
                }
            }

            using (this.result.Operation(node))
            {
                var metadataProvider = this.Options.GetMetadataProvider();
                var pos = this.result.Length;
                if (node.Expression == null)
                {
                    var decl = node.Member.DeclaringType;
                    if (decl != null)
                    {
                        // TODO: there should be a way to customize the name of types through metadata
                        this.result.Write(decl.FullName);
                        this.result.Write('.');
                        this.result.Write(decl.Name);
                    }
                }
                else if (node.Expression != this.contextParameter)
                    this.Visit(node.Expression);
                else
                {
                    this.usedScopeMembers = this.usedScopeMembers ?? new List<string>();
                    var meta = metadataProvider.GetMemberMetadata(node.Member);
                    Debug.Assert(!string.IsNullOrEmpty(meta?.MemberName), "!string.IsNullOrEmpty(meta?.MemberName)");
                    this.usedScopeMembers.Add(meta?.MemberName ?? node.Member.Name);
                }

                if (this.result.Length > pos)
                    this.result.Write('.');

                if (node.Expression?.Type.IsClosureRootType() == true)
                {
                    var cte = ((ConstantExpression)node.Expression).Value;
                    var value = ((FieldInfo)node.Member).GetValue(cte);
                    this.Visit(Expression.Constant(value, node.Type));
                }

                var propInfo = node.Member as PropertyInfo;
                if (propInfo?.DeclaringType != null
                    && node.Type == typeof(int)
                    && node.Member.Name == "Count"
                    && TypeHelpers.IsListType(propInfo.DeclaringType))
                {
                    this.result.Write("length");
                }
                else
                {
                    var meta = metadataProvider.GetMemberMetadata(node.Member);
                    Debug.Assert(!string.IsNullOrEmpty(meta?.MemberName), "!string.IsNullOrEmpty(meta?.MemberName)");
                    this.result.Write(meta?.MemberName);
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
                    this.result.WriteOperator(node.NodeType, node.Type);
                this.Visit(node.Operand);
                if (isPostOp)
                    this.result.WriteOperator(node.NodeType, node.Type);

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
            this.result.Write(node.Name);
            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            using (this.result.Operation(0))
            {
                this.result.Write('[');

                var posStart = this.result.Length;
                foreach (var item in node.Expressions)
                {
                    if (this.result.Length > posStart)
                        this.result.Write(',');

                    this.Visit(item);
                }

                this.result.Write(']');
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
                    this.result.Write('{');

                    var posStart = this.result.Length;
                    for (int itMember = 0; itMember < node.Members.Count; itMember++)
                    {
                        var member = node.Members[itMember];
                        if (this.result.Length > posStart)
                            this.result.Write(',');

                        if (Regex.IsMatch(member.Name, @"^\w[\d\w]*$"))
                            this.result.Write(member.Name);
                        else
                            this.WriteStringLiteral(member.Name);

                        this.result.Write(':');
                        this.Visit(node.Arguments[itMember]);
                    }

                    this.result.Write('}');
                }
            }
            else if (node.Type != typeof(Regex))
            {
                using (this.result.Operation(0))
                {
                    this.result.Write("new");
                    this.result.Write(' ');
                    using (this.result.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.result.Write(node.Type.FullName.Replace('+', '.'));

                        this.result.Write('(');

                        var posStart = this.result.Length;
                        foreach (var argExpr in node.Arguments)
                        {
                            if (this.result.Length > posStart)
                                this.result.Write(',');

                            this.Visit(argExpr);
                        }

                        this.result.Write(')');
                    }
                }
            }
            else
            {
                // To run the regex use this code:
                // var lambda = Expression.Lambda<Func<Regex>>(node);

                // if all parameters are constant
                if (node.Arguments.All(a => a.NodeType == ExpressionType.Constant))
                {
                    this.result.Write('/');

                    var pattern = (string)((ConstantExpression)node.Arguments[0]).Value;
                    this.result.Write(pattern);
                    var args = node.Arguments.Count;

                    this.result.Write('/');
                    this.result.Write('g');
                    RegexOptions options = 0;
                    if (args == 2)
                    {
                        options = (RegexOptions)((ConstantExpression)node.Arguments[1]).Value;

                        if ((options & RegexOptions.IgnoreCase) != 0)
                            this.result.Write('i');
                        if ((options & RegexOptions.Multiline) != 0)
                            this.result.Write('m');
                    }

                    // creating a Regex object with `ECMAScript` to make sure the pattern is valid in JavaScript.
                    // If it is not valid, then an exception is thrown.
                    // ReSharper disable once UnusedVariable
                    var ecmaRegex = new Regex(pattern, options | RegexOptions.ECMAScript);
                }
                else
                {
                    using (this.result.Operation(JavascriptOperationTypes.New))
                    {
                        this.result.Write("new RegExp(");

                        using (this.result.Operation(JavascriptOperationTypes.ParamIsolatedLhs))
                            this.Visit(node.Arguments[0]);

                        var args = node.Arguments.Count;

                        if (args == 2)
                        {
                            this.result.Write(',');

                            var optsConst = node.Arguments[1] as ConstantExpression;
                            if (optsConst == null)
                                throw new NotSupportedException("The options parameter of a Regex must be constant");

                            var options = (RegexOptions)optsConst.Value;

                            this.result.Write('\'');
                            this.result.Write('g');
                            if ((options & RegexOptions.IgnoreCase) != 0)
                                this.result.Write('i');
                            if ((options & RegexOptions.Multiline) != 0)
                                this.result.Write('m');
                            this.result.Write('\'');
                        }

                        this.result.Write(')');
                    }
                }
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsSpecialName)
            {
                var isIndexer = node.Method.Name == "get_Item" || node.Method.Name == "get_Chars";
                if (isIndexer)
                {
                    using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                    {
                        this.Visit(node.Object);
                        this.result.Write('[');

                        using (this.result.Operation(0))
                        {
                            var posStart0 = this.result.Length;
                            foreach (var arg in node.Arguments)
                            {
                                if (this.result.Length != posStart0)
                                    this.result.Write(',');

                                this.Visit(arg);
                            }
                        }

                        this.result.Write(']');
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
                                this.result.Write('[');

                                using (this.result.Operation(0))
                                {
                                    var posStart0 = this.result.Length;
                                    foreach (var arg in node.Arguments)
                                    {
                                        if (this.result.Length != posStart0)
                                            this.result.Write(',');

                                        this.Visit(arg);
                                    }
                                }

                                this.result.Write(']');
                            }
                        }

                        this.result.Write('=');
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
                        this.result.Write(".hasOwnProperty(");
                        using (this.result.Operation(0))
                            this.Visit(node.Arguments.Single());
                        this.result.Write(')');
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
                            this.result.Write(".indexOf(");
                            using (this.result.Operation(0))
                            {
                                var posStart = this.result.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.result.Length > posStart)
                                        this.result.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.result.Write(')');
                        }

                        this.result.Write(">=0");
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
                    {
                        var undefined = this.Options.UndefinedLiteral;
                        if (string.IsNullOrEmpty(n))
                            methodName = "toLocaleString()";
                        else
                            methodName = string.Format(
                                "toLocaleString({0},{{minimumFractionDigits:{1}}})",
                                undefined,
                                n);
                    }
                    else if (f == "X")
                        methodName = "toString(16)";
                }

                if (methodName != null)
                    using (this.result.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.result.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.result.WriteFormat(".{0}", methodName);
                        return node;
                    }
            }

            if (!node.Method.IsStatic)
                throw new NotSupportedException(string.Format("By default, Lambda2Js cannot convert custom instance methods, only static ones. `{0}` is not static.", node.Method.Name));

            using (this.result.Operation(JavascriptOperationTypes.Call))
                if (node.Method.DeclaringType != null)
                {
                    this.result.Write(node.Method.DeclaringType.FullName);
                    this.result.Write('.');
                    this.result.Write(node.Method.Name);
                    this.result.Write('(');

                    var posStart = this.result.Length;
                    using (this.result.Operation(0))
                        foreach (var arg in node.Arguments)
                        {
                            if (this.result.Length != posStart)
                                this.result.Write(',');

                            this.Visit(arg);
                        }

                    this.result.Write(')');

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
            throw new NotSupportedException("MemberInitExpression is not supported. Converting it requires a custom JavascriptConversionExtension like MemberInitAsJson.");
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            return node;
        }
    }
}