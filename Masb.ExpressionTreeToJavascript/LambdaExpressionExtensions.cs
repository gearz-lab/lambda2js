using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Masb.ExpressionTreeToJavascript
{
    public static class LambdaExpressionExtensions
    {
        public static string CompileToJavascript([NotNull] this LambdaExpression expr, bool bodyOnly = true)
        {
            if (expr == null)
                throw new ArgumentNullException("expr");

            var visitor = new ExpressionTreeToJavascriptVisitor(expr.Parameters.FirstOrDefault());
            visitor.Visit(bodyOnly ? expr.Body : expr);
            return visitor.Result;
        }
    }
}