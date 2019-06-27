using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

#pragma warning disable 1591
namespace Lambda2Js
{
    /// <summary>
    /// Expression visitor that converts each node to JavaScript code.
    /// </summary>
    internal sealed class JavascriptCompilerExpressionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression contextParameter;
        private readonly IEnumerable<JavascriptConversionExtension> extensions;
        private readonly JavascriptWriter resultWriter = new JavascriptWriter();
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
        public string Result => this.resultWriter.ToString();

        /// <summary>
        /// Gets the scope names that were used from the scope parameter.
        /// </summary>
        [CanBeNull]
        public string[] UsedScopeMembers => this.usedScopeMembers?.ToArray();

        public override Expression Visit(Expression node)
        {
            var node2 = PreprocessNode(node);
            JavascriptConversionContext context = null;
            foreach (var each in this.extensions)
            {
                context = context ?? new JavascriptConversionContext(node2, this, this.resultWriter, this.Options);

                each.ConvertToJavascript(context);

                if (context.preventDefault)
                {
                    // canceling any further action with the current node
                    return node2;
                }
            }

            // nothing happened, continue to the default conversion behavior
            return base.Visit(node2);
        }

        private Expression PreprocessNode(Expression node)
        {
            if (node.NodeType == ExpressionType.Equal
                || node.NodeType == ExpressionType.Or
                || node.NodeType == ExpressionType.And
                || node.NodeType == ExpressionType.ExclusiveOr
                || node.NodeType == ExpressionType.OrAssign
                || node.NodeType == ExpressionType.AndAssign
                || node.NodeType == ExpressionType.ExclusiveOrAssign
                || node.NodeType == ExpressionType.NotEqual)
            {
                var binary = (BinaryExpression)node;
                var left = binary.Left as UnaryExpression;
                var leftVal = left != null && (left.NodeType == ExpressionType.Convert || left.NodeType == ExpressionType.ConvertChecked) ? left.Operand : binary.Left;
                var right = binary.Right as UnaryExpression;
                var rightVal = right != null && (right.NodeType == ExpressionType.Convert || right.NodeType == ExpressionType.ConvertChecked) ? right.Operand : binary.Right;
                if (rightVal.Type != leftVal.Type)
                {
                    if (leftVal.Type.GetTypeInfo().IsEnum && TypeHelpers.IsNumericType(rightVal.Type) && rightVal.NodeType == ExpressionType.Constant)
                    {
                        rightVal = Expression.Convert(
                            Expression.Constant(Enum.ToObject(leftVal.Type, ((ConstantExpression)rightVal).Value)),
                            rightVal.Type);
                        leftVal = binary.Left;
                    }
                    else if (rightVal.Type.GetTypeInfo().IsEnum && TypeHelpers.IsNumericType(leftVal.Type) && leftVal.NodeType == ExpressionType.Constant)
                    {
                        leftVal = Expression.Convert(
                            Expression.Constant(Enum.ToObject(rightVal.Type, ((ConstantExpression)leftVal).Value)),
                            leftVal.Type);
                        rightVal = binary.Right;
                    }
                    else
                    {
                        return node;
                    }

                    return Expression.MakeBinary(node.NodeType, leftVal, rightVal);
                }
            }

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                {
                    this.Visit(node.Left);
                    this.resultWriter.Write('[');
                    using (this.resultWriter.Operation(0))
                        this.Visit(node.Right);
                    this.resultWriter.Write(']');
                    return node;
                }
            }

            using (this.resultWriter.Operation(node))
            {
                this.Visit(node.Left);
                this.resultWriter.WriteOperator(node.NodeType, node.Type);
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
            using (this.resultWriter.Operation(JavascriptOperationTypes.TernaryOp))
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.TernaryTest))
                    this.Visit(node.Test);

                this.resultWriter.Write('?');

                using (this.resultWriter.Operation(JavascriptOperationTypes.TernaryTrueValue))
                    this.Visit(node.IfTrue);

                this.resultWriter.Write(':');

                using (this.resultWriter.Operation(JavascriptOperationTypes.TernaryFalseValue))
                    this.Visit(node.IfFalse);

                return node;
            }
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (TypeHelpers.IsNumericType(node.Type))
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                    this.resultWriter.Write(Convert.ToString(node.Value, CultureInfo.InvariantCulture));
            }
            else if (node.Type == typeof(bool))
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                    this.resultWriter.Write((bool)node.Value ? "true" : "false");
            }
            else if (node.Type == typeof(char))
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                    this.WriteStringLiteral(node.Value.ToString());
            }
            else if (node.Value == null)
            {
                this.resultWriter.Write("null");
            }
            else if (node.Type == typeof(string))
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                    this.WriteStringLiteral((string)node.Value);
            }
            else if (node.Type.GetTypeInfo().IsEnum)
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                {
                    var underlyingType = Enum.GetUnderlyingType(node.Type);
                    this.resultWriter.Write(Convert.ChangeType(node.Value, underlyingType, CultureInfo.InvariantCulture));
                }
            }
            else if (node.Type == typeof(Regex))
            {
                using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                {
                    this.resultWriter.Write('/');
                    this.resultWriter.Write(node.Value);
                    this.resultWriter.Write("/g");
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
            this.resultWriter.Write('"');
            this.resultWriter.Write(
                str
                    .Replace("\\", "\\\\")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t")
                    .Replace("\0", "\\0")
                    .Replace("\"", "\\\""));

            this.resultWriter.Write('"');
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            return node;
        }

        protected override Expression VisitDefault(DefaultExpression node)
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
            if (this.Options.ScriptVersion.Supports(JavascriptSyntaxFeature.ArrowFunction))
            {
                // Arrow function syntax and precedence works mostly like an assignment.
                using (this.resultWriter.Operation(JavascriptOperationTypes.AssignRhs))
                {
                    var pars = node.Parameters;
                    if (pars.Count != 1)
                        this.resultWriter.Write("(");

                    var posStart = this.resultWriter.Length;
                    foreach (var param in node.Parameters)
                    {
                        if (param.IsByRef)
                            throw new NotSupportedException("Cannot pass by ref in javascript.");

                        if (this.resultWriter.Length > posStart)
                            this.resultWriter.Write(',');

                        this.resultWriter.Write(param.Name);
                    }

                    if (pars.Count != 1)
                        this.resultWriter.Write(")");

                    this.resultWriter.Write("=>");

                    using (this.resultWriter.Operation(JavascriptOperationTypes.ParamIsolatedLhs))
                    {
                        this.Visit(node.Body);
                    }
                }
            }
            else
            {
                using (this.resultWriter.Operation(node))
                {
                    this.resultWriter.Write("function(");

                    var posStart = this.resultWriter.Length;
                    foreach (var param in node.Parameters)
                    {
                        if (param.IsByRef)
                            throw new NotSupportedException("Cannot pass by ref in javascript.");

                        if (this.resultWriter.Length > posStart)
                            this.resultWriter.Write(',');

                        this.resultWriter.Write(param.Name);
                    }

                    this.resultWriter.Write("){");
                    if (node.ReturnType != typeof(void))
                        using (this.resultWriter.Operation(0))
                        {
                            this.resultWriter.Write("return ");
                            this.Visit(node.Body);
                        }
                    else
                        using (this.resultWriter.Operation(0))
                        {
                            this.Visit(node.Body);
                        }

                    this.resultWriter.Write(";}");
                }
            }
            return node;
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            // Detecting a new dictionary
            if (TypeHelpers.IsDictionaryType(node.Type))
            {
                using (this.resultWriter.Operation(0))
                {
                    this.resultWriter.Write('{');

                    var posStart = this.resultWriter.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.resultWriter.Length > posStart)
                            this.resultWriter.Write(',');

                        if (init.Arguments.Count != 2)
                            throw new NotSupportedException(
                                "Objects can only be initialized with methods that receive pairs: key -> name");

                        var nameArg = init.Arguments[0];
                        if (nameArg.NodeType != ExpressionType.Constant || nameArg.Type != typeof(string))
                            throw new NotSupportedException("The key of an object must be a constant string value");

                        var name = (string)((ConstantExpression)nameArg).Value;
                        if (Regex.IsMatch(name, @"^\w[\d\w]*$"))
                            this.resultWriter.Write(name);
                        else
                            this.WriteStringLiteral(name);

                        this.resultWriter.Write(':');
                        this.Visit(init.Arguments[1]);
                    }

                    this.resultWriter.Write('}');
                }

                return node;
            }

            // Detecting a new dictionary
            if (TypeHelpers.IsListType(node.Type))
            {
                using (this.resultWriter.Operation(0))
                {
                    this.resultWriter.Write('[');

                    var posStart = this.resultWriter.Length;
                    foreach (var init in node.Initializers)
                    {
                        if (this.resultWriter.Length > posStart)
                            this.resultWriter.Write(',');

                        if (init.Arguments.Count != 1)
                            throw new Exception(
                                "Arrays can only be initialized with methods that receive a single parameter for the value");

                        this.Visit(init.Arguments[0]);
                    }

                    this.resultWriter.Write(']');
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
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Literal))
                            this.resultWriter.Write("\"\"");
                        return node;
                    }
                }
            }

            bool isClosure = false;
            using (this.resultWriter.Operation(node))
            {
                var metadataProvider = this.Options.GetMetadataProvider();
                var pos = this.resultWriter.Length;
                if (node.Expression == null)
                {
                    var decl = node.Member.DeclaringType;
                    if (decl != null)
                    {
                        // TODO: there should be a way to customize the name of types through metadata
                        this.resultWriter.Write(decl.FullName);
                        this.resultWriter.Write('.');
                        this.resultWriter.Write(decl.Name);
                    }
                }
                else if (node.Expression.Type.IsClosureRootType())
                {
                    isClosure = true;
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

                if (this.resultWriter.Length > pos)
                    this.resultWriter.Write('.');

                if (!isClosure)
                {
                    var propInfo = node.Member as PropertyInfo;
                    if (propInfo?.DeclaringType != null
                        && node.Type == typeof(int)
                        && node.Member.Name == "Count"
                        && TypeHelpers.IsListType(propInfo.DeclaringType))
                    {
                        this.resultWriter.Write("length");
                    }
                    else
                    {
                        var meta = metadataProvider.GetMemberMetadata(node.Member);
                        Debug.Assert(!string.IsNullOrEmpty(meta?.MemberName), "!string.IsNullOrEmpty(meta?.MemberName)");
                        this.resultWriter.Write(meta?.MemberName);
                    }
                }
            }

            if (isClosure)
            {
                var cte = ((ConstantExpression)node.Expression).Value;
                var value = ((FieldInfo)node.Member).GetValue(cte);
                this.Visit(Expression.Constant(value, node.Type));
            }

            return node;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            using (this.resultWriter.Operation(node))
            {
                var isPostOp = JsOperationHelper.IsPostfixOperator(node.NodeType);

                if (!isPostOp)
                    this.resultWriter.WriteOperator(node.NodeType, node.Type);
                this.Visit(node.Operand);
                if (isPostOp)
                    this.resultWriter.WriteOperator(node.NodeType, node.Type);

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
            this.resultWriter.Write(node.Name);
            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            using (this.resultWriter.Operation(0))
            {
                this.resultWriter.Write('[');

                var posStart = this.resultWriter.Length;
                foreach (var item in node.Expressions)
                {
                    if (this.resultWriter.Length > posStart)
                        this.resultWriter.Write(',');

                    this.Visit(item);
                }

                this.resultWriter.Write(']');
            }

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            // Detecting inlineable objects
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (node.Members != null && node.Members.Count > 0)
            {
                using (this.resultWriter.Operation(0))
                {
                    this.resultWriter.Write('{');

                    var posStart = this.resultWriter.Length;
                    for (int itMember = 0; itMember < node.Members.Count; itMember++)
                    {
                        var member = node.Members[itMember];
                        if (this.resultWriter.Length > posStart)
                            this.resultWriter.Write(',');

                        if (Regex.IsMatch(member.Name, @"^\w[\d\w]*$"))
                            this.resultWriter.Write(member.Name);
                        else
                            this.WriteStringLiteral(member.Name);

                        this.resultWriter.Write(':');
                        this.Visit(node.Arguments[itMember]);
                    }

                    this.resultWriter.Write('}');
                }
            }
            else if (node.Type != typeof(Regex))
            {
                using (this.resultWriter.Operation(0))
                {
                    this.resultWriter.Write("new");
                    this.resultWriter.Write(' ');
                    using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.resultWriter.Write(node.Type.FullName.Replace('+', '.'));

                        this.resultWriter.Write('(');

                        var posStart = this.resultWriter.Length;
                        foreach (var argExpr in node.Arguments)
                        {
                            if (this.resultWriter.Length > posStart)
                                this.resultWriter.Write(',');

                            this.Visit(argExpr);
                        }

                        this.resultWriter.Write(')');
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
                    this.resultWriter.Write('/');

                    var pattern = (string)((ConstantExpression)node.Arguments[0]).Value;
                    this.resultWriter.Write(pattern);
                    var args = node.Arguments.Count;

                    this.resultWriter.Write('/');
                    this.resultWriter.Write('g');
                    RegexOptions options = 0;
                    if (args == 2)
                    {
                        options = (RegexOptions)((ConstantExpression)node.Arguments[1]).Value;

                        if ((options & RegexOptions.IgnoreCase) != 0)
                            this.resultWriter.Write('i');
                        if ((options & RegexOptions.Multiline) != 0)
                            this.resultWriter.Write('m');
                    }

                    // creating a Regex object with `ECMAScript` to make sure the pattern is valid in JavaScript.
                    // If it is not valid, then an exception is thrown.
                    // ReSharper disable once UnusedVariable
                    var ecmaRegex = new Regex(pattern, options | RegexOptions.ECMAScript);
                }
                else
                {
                    using (this.resultWriter.Operation(JavascriptOperationTypes.New))
                    {
                        this.resultWriter.Write("new RegExp(");

                        using (this.resultWriter.Operation(JavascriptOperationTypes.ParamIsolatedLhs))
                            this.Visit(node.Arguments[0]);

                        var args = node.Arguments.Count;

                        if (args == 2)
                        {
                            this.resultWriter.Write(',');

                            var optsConst = node.Arguments[1] as ConstantExpression;
                            if (optsConst == null)
                                throw new NotSupportedException("The options parameter of a Regex must be constant");

                            var options = (RegexOptions)optsConst.Value;

                            this.resultWriter.Write('\'');
                            this.resultWriter.Write('g');
                            if ((options & RegexOptions.IgnoreCase) != 0)
                                this.resultWriter.Write('i');
                            if ((options & RegexOptions.Multiline) != 0)
                                this.resultWriter.Write('m');
                            this.resultWriter.Write('\'');
                        }

                        this.resultWriter.Write(')');
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
                    using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                    {
                        this.Visit(node.Object);
                        this.resultWriter.Write('[');

                        using (this.resultWriter.Operation(0))
                        {
                            var posStart0 = this.resultWriter.Length;
                            foreach (var arg in node.Arguments)
                            {
                                if (this.resultWriter.Length != posStart0)
                                    this.resultWriter.Write(',');

                                this.Visit(arg);
                            }
                        }

                        this.resultWriter.Write(']');
                        return node;
                    }
                }

                if (node.Method.Name == "set_Item")
                {
                    using (this.resultWriter.Operation(0))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.AssignRhs))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                            {
                                this.Visit(node.Object);
                                this.resultWriter.Write('[');

                                using (this.resultWriter.Operation(0))
                                {
                                    var posStart0 = this.resultWriter.Length;
                                    foreach (var arg in node.Arguments)
                                    {
                                        if (this.resultWriter.Length != posStart0)
                                            this.resultWriter.Write(',');

                                        this.Visit(arg);
                                    }
                                }

                                this.resultWriter.Write(']');
                            }
                        }

                        this.resultWriter.Write('=');
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
                    using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.resultWriter.Write(".hasOwnProperty(");
                        using (this.resultWriter.Operation(0))
                            this.Visit(node.Arguments.Single());
                        this.resultWriter.Write(')');
                        return node;
                    }
                }
            }

            if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name == "Contains")
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_includes))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".includes(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');
                            return node;
                        }
                    }
                    else if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_indexOf))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Comparison))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                            {
                                using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                    this.Visit(node.Object);
                                this.resultWriter.Write(".indexOf(");
                                using (this.resultWriter.Operation(0))
                                {
                                    var posStart = this.resultWriter.Length;
                                    foreach (var arg in node.Arguments)
                                    {
                                        if (this.resultWriter.Length > posStart)
                                            this.resultWriter.Write(',');
                                        this.Visit(arg);
                                    }
                                }

                                this.resultWriter.Write(')');
                            }

                            this.resultWriter.Write(">=0");
                            return node;
                        }
                    }
                }
                else if (node.Method.Name == nameof(string.StartsWith))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_startsWith))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".startsWith(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');
                            return node;

                        }
                }
                else if (node.Method.Name == nameof(string.EndsWith))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_endsWith))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".endsWith(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');
                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.ToLower))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_toLowerCase))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".toLowerCase(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.ToUpper))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_toUpperCase))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".toUpperCase(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.Trim))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_trim))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".trim(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.TrimEnd))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_trimRight))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".trimRight(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.TrimStart))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_trimLeft))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".trimLeft(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.Substring))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_substring))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".substring(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.PadLeft))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_padStart))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".padStart(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.PadRight))
                {
                    if (this.Options.ScriptVersion.Supports(JavascriptApiFeature.String_prototype_padEnd))
                        using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                        {
                            using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                                this.Visit(node.Object);
                            this.resultWriter.Write(".padEnd(");
                            using (this.resultWriter.Operation(0))
                            {
                                var posStart = this.resultWriter.Length;
                                foreach (var arg in node.Arguments)
                                {
                                    if (this.resultWriter.Length > posStart)
                                        this.resultWriter.Write(',');
                                    this.Visit(arg);
                                }
                            }

                            this.resultWriter.Write(')');

                            return node;
                        }
                }
                else if (node.Method.Name == nameof(string.LastIndexOf))
                {
                    using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.resultWriter.Write(".lastIndexOf(");
                        using (this.resultWriter.Operation(0))
                        {
                            var posStart = this.resultWriter.Length;
                            foreach (var arg in node.Arguments)
                            {
                                if (this.resultWriter.Length > posStart)
                                    this.resultWriter.Write(',');
                                this.Visit(arg);
                            }
                        }

                        this.resultWriter.Write(')');

                        return node;
                    }
                }
                else if (node.Method.Name == nameof(string.IndexOf))
                {
                    using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.resultWriter.Write(".indexOf(");
                        using (this.resultWriter.Operation(0))
                        {
                            var posStart = this.resultWriter.Length;
                            foreach (var arg in node.Arguments)
                            {
                                if (this.resultWriter.Length > posStart)
                                    this.resultWriter.Write(',');
                                this.Visit(arg);
                            }
                        }

                        this.resultWriter.Write(')');

                        return node;
                    }
                }
                else if (node.Method.Name == nameof(string.Concat))
                {
                    using (this.resultWriter.Operation(JavascriptOperationTypes.Concat))
                    {
                        if (node.Arguments.Count == 0)
                            this.resultWriter.Write("''");
                        else
                        {
                            if (node.Arguments[0].Type != typeof(string))
                                this.resultWriter.Write("''+");

                            var posStart = this.resultWriter.Length;
                            foreach (var arg in node.Arguments)
                            {
                                if (this.resultWriter.Length > posStart)
                                    this.resultWriter.Write('+');
                                this.Visit(arg);
                            }
                        }

                        return node;
                    }
                }
            }

            if (node.Method.Name == "ToString" && node.Type == typeof(string) && node.Object != null)
            {
                string methodName = null;
                if (node.Arguments.Count == 0 || typeof(IFormatProvider).GetTypeInfo().IsAssignableFrom(node.Arguments[0].Type.GetTypeInfo()))
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
                    using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                    {
                        using (this.resultWriter.Operation(JavascriptOperationTypes.IndexerProperty))
                            this.Visit(node.Object);
                        this.resultWriter.WriteFormat(".{0}", methodName);
                        return node;
                    }
            }

            if (!node.Method.IsStatic)
                throw new NotSupportedException(string.Format("By default, Lambda2Js cannot convert custom instance methods, only static ones. `{0}` is not static.", node.Method.Name));

            using (this.resultWriter.Operation(JavascriptOperationTypes.Call))
                if (node.Method.DeclaringType != null)
                {
                    this.resultWriter.Write(node.Method.DeclaringType.FullName);
                    this.resultWriter.Write('.');
                    this.resultWriter.Write(node.Method.Name);
                    this.resultWriter.Write('(');

                    var posStart = this.resultWriter.Length;
                    using (this.resultWriter.Operation(0))
                        foreach (var arg in node.Arguments)
                        {
                            if (this.resultWriter.Length != posStart)
                                this.resultWriter.Write(',');

                            this.Visit(arg);
                        }

                    this.resultWriter.Write(')');

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