using System;
using System.Linq;
using System.Linq.Expressions;

namespace Lambda2Js
{
    public class StaticStringMethods : JavascriptConversionExtension
    {
        public override void ConvertToJavascript(JavascriptConversionContext context)
        {
            var methodCall = context.Node as MethodCallExpression;
            if (methodCall != null)
                if (methodCall.Method.DeclaringType == typeof(string))
                {
                    switch (methodCall.Method.Name)
                    {
                        case "Concat":
                            {
                                var writer = context.GetWriter();
                                using (writer.Operation(JavascriptOperationTypes.Concat))
                                {
                                    writer.Append("''+");
                                    var posStart = writer.Length;
                                    foreach (var arg in methodCall.Arguments)
                                    {
                                        if (writer.Length > posStart)
                                            writer.Append('+');

                                        context.Visitor.Visit(arg);
                                    }
                                }

                                return;
                            }

                        case "Join":
                            {
                                var writer = context.GetWriter();
                                using (writer.Operation(JavascriptOperationTypes.Call))
                                {
                                    using (writer.Operation(JavascriptOperationTypes.IndexerProperty))
                                    {
                                        var pars = methodCall.Method.GetParameters();
                                        if (pars.Length == 4 && pars[1].ParameterType.IsArray && pars[2].ParameterType == typeof(int) && pars[3].ParameterType == typeof(int))
                                            throw new NotSupportedException("The `String.Join` method with start and count paramaters is not supported.");

                                        if (pars.Length != 2 || !TypeHelpers.IsEnumerableType(pars[1].ParameterType))
                                            throw new NotSupportedException("This `String.Join` method is not supported.");

                                        // if second parameter is an enumerable, render it directly
                                        context.Visitor.Visit(methodCall.Arguments[1]);
                                        writer.Append(".join");
                                    }

                                    writer.Append('(');

                                    // separator
                                    using (writer.Operation(0))
                                        context.Visitor.Visit(methodCall.Arguments[0]);

                                    writer.Append(')');
                                }

                                return;
                            }
                    }
                }
        }
    }
}
