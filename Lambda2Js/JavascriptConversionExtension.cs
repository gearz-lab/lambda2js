using System;
using System.Linq.Expressions;

namespace Lambda2Js
{
    public abstract class JavascriptConversionExtension
    {
        public abstract void ConvertToJavascript(JavascriptConversionContext context);

        protected static Type GetTypeOfExpression(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
                return ((UnaryExpression)expression).Operand.Type;
            return expression.Type;
        }
    }
}