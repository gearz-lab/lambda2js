using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Masb.ExpressionTreeToJavascript
{
    public static class LambdaExpressionExtensions
    {
        public static string CompileToJavascript([NotNull] this LambdaExpression expr, JavascriptCompilationOptions options = null)
        {
            if (expr == null)
                throw new ArgumentNullException("expr");

            options = options ?? JavascriptCompilationOptions.DefaultOptions;

            var visitor =
                new JavascriptCompilerExpressionVisitor(
                    options.ScopeParameter ? expr.Parameters.SingleOrDefault() : null,
                    options.Extensions);

            visitor.Visit(options.BodyOnly ? expr.Body : expr);
            return visitor.Result;
        }
    }
}