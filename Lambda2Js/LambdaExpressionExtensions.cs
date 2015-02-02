using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Lambda2Js
{
    public static class LambdaExpressionExtensions
    {
        /// <summary>
        /// Compiles an expression to JavaScript code.
        /// </summary>
        /// <param name="expr">Expression to compile to JavaScript.</param>
        /// <param name="options">
        /// Conversion options:
        /// whether to include only the body of the lambda,
        /// whether to use a single scope parameter,
        /// what extensions to use (i.e. StaticStringMethods, StaticMathMethods, or any other custom extensions).
        /// </param>
        /// <returns>JavaScript code represented as a string.</returns>
        public static string CompileToJavascript([NotNull] this LambdaExpression expr, JavascriptCompilationOptions options = null)
        {
            if (expr == null)
                throw new ArgumentNullException("expr");

            options = options ?? JavascriptCompilationOptions.DefaultOptions;

            var visitor =
                new JavascriptCompilerExpressionVisitor(
                    options.ScopeParameter ? expr.Parameters.SingleOrDefault() : null,
                    options.Extensions);

            visitor.Visit(options.BodyOnly || options.ScopeParameter ? expr.Body : expr);
            if (!options.BodyOnly && options.ScopeParameter)
                if (visitor.UsedScopeMembers != null)
                    return string.Format(
                        "function({0}){{return {1};}}",
                        string.Join(",", visitor.UsedScopeMembers),
                        visitor.Result);

            return visitor.Result;
        }
    }
}