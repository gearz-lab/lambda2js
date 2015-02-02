using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Lambda2Js
{
    public static class JavascriptConversionHelpers
    {
        public static JavascriptConversionContext Write(this JavascriptConversionContext context, Expression node)
        {
            context.Visitor.Visit(node);
            return context;
        }

        public static JavascriptConversionContext Write(this JavascriptConversionContext context, char ch)
        {
            var writer = context.GetWriter();
            writer.Write(ch);
            return context;
        }

        public static JavascriptConversionContext WriteMany(
            this JavascriptConversionContext context,
            char separator,
            IEnumerable<Expression> nodes)
        {
            var writer = context.GetWriter();
            int count = 0;
            foreach (var node in nodes)
            {
                if (count++ > 0)
                    writer.Write(separator);

                context.Visitor.Visit(node);
            }
            return context;
        }

        public static JavascriptConversionContext WriteMany(
            this JavascriptConversionContext context,
            char separator,
            params Expression[] nodes)
        {
            return WriteMany(context, separator, (IEnumerable<Expression>)nodes);
        }

        /// <summary>
        /// Writes many expressions isolated from outer and inner operations by opening, closing and separator characters.
        /// </summary>
        /// <param name="context">The Javascript conversion context.</param>
        /// <param name="opening">First character to render, isolating from outer operation.</param>
        /// <param name="closing">Last character to render, isolating from outer operation.</param>
        /// <param name="separator">Separator character to render, isolating one parameter from the other.</param>
        /// <param name="nodes">Nodes to render.</param>
        public static JavascriptConversionContext WriteManyIsolated(
            this JavascriptConversionContext context,
            char opening,
            char closing,
            char separator,
            IEnumerable<Expression> nodes)
        {
            var writer = context.GetWriter();
            writer.Write(opening);
            using (writer.Operation(0))
                context.WriteMany(separator, nodes);
            writer.Write(closing);
            return context;
        }

        /// <summary>
        /// Writes many expressions isolated from outer and inner operations by opening, closing and separator characters.
        /// </summary>
        /// <param name="context">The Javascript conversion context.</param>
        /// <param name="opening">First character to render, isolating from outer operation.</param>
        /// <param name="closing">Last character to render, isolating from outer operation.</param>
        /// <param name="separator">Separator character to render, isolating one parameter from the other.</param>
        /// <param name="nodes">Nodes to render.</param>
        public static JavascriptConversionContext WriteManyIsolated(
            this JavascriptConversionContext context,
            char opening,
            char closing,
            char separator,
            params Expression[] nodes)
        {
            return WriteManyIsolated(context, opening, closing, separator, (IEnumerable<Expression>)nodes);
        }

        public static IDisposable Operation(
            this JavascriptConversionContext context,
            JavascriptOperationTypes op)
        {
            return context.GetWriter().Operation(op);
        }

        public static JavascriptConversionContext Write(this JavascriptConversionContext context, string str)
        {
            context.GetWriter().Write(str);
            return context;
        }

        [StringFormatMethod("format")]
        public static JavascriptConversionContext WriteFormat(this JavascriptConversionContext context, string format, params object[] values)
        {
            context.GetWriter().WriteFormat(format, values);
            return context;
        }
    }
}