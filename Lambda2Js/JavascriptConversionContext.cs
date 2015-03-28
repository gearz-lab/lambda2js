using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Lambda2Js
{
    public class JavascriptConversionContext
    {
        [NotNull]
        private readonly JavascriptWriter result;

        [NotNull]
        private Expression node;

        internal bool preventDefault;

        internal bool gotWriter;

        public JavascriptConversionContext(
            [NotNull] Expression node,
            [NotNull] ExpressionVisitor visitor,
            [NotNull] JavascriptWriter result)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (visitor == null)
                throw new ArgumentNullException("visitor");
            if (result == null)
                throw new ArgumentNullException("result");
            this.result = result;
            this.Visitor = visitor;
            this.node = node;
        }

        [NotNull]
        public Expression Node
        {
            get
            {
                return this.node;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                this.node = value;
            }
        }

        public void PreventDefault()
        {
            this.preventDefault = true;
        }

        public ExpressionVisitor Visitor { get; private set; }

        public JavascriptWriter GetWriter()
        {
            this.gotWriter = true;
            return this.result;
        }
    }

    public static class JavascriptConversionHelpers
    {
        public static void Write(this JavascriptConversionContext context, Expression node)
        {
            context.Visitor.Visit(node);
        }

        public static void WriteMany(
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
        }

        public static void WriteMany(
            this JavascriptConversionContext context,
            char separator,
            params Expression[] nodes)
        {
            WriteMany(context, separator, (IEnumerable<Expression>)nodes);
        }

        /// <summary>
        /// Writes many expressions isolated from outer and inner operations by opening, closing and separator characters.
        /// </summary>
        /// <param name="context">The Javascript conversion context.</param>
        /// <param name="opening">First character to render, isolating from outer operation.</param>
        /// <param name="closing">Last character to render, isolating from outer operation.</param>
        /// <param name="separator">Separator character to render, isolating one parameter from the other.</param>
        /// <param name="nodes">Nodes to render.</param>
        public static void WriteManyIsolated(
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
        }

        /// <summary>
        /// Writes many expressions isolated from outer and inner operations by opening, closing and separator characters.
        /// </summary>
        /// <param name="context">The Javascript conversion context.</param>
        /// <param name="opening">First character to render, isolating from outer operation.</param>
        /// <param name="closing">Last character to render, isolating from outer operation.</param>
        /// <param name="separator">Separator character to render, isolating one parameter from the other.</param>
        /// <param name="nodes">Nodes to render.</param>
        public static void WriteManyIsolated(
            this JavascriptConversionContext context,
            char opening,
            char closing,
            char separator,
            params Expression[] nodes)
        {
            WriteManyIsolated(context, opening, closing, separator, (IEnumerable<Expression>)nodes);
        }

        public static IDisposable Operation(
            this JavascriptConversionContext context,
            JavascriptOperationTypes op)
        {
            return context.GetWriter().Operation(op);
        }

        public static JavascriptWriter Write(this JavascriptConversionContext context, string str)
        {
            return context.GetWriter().Write(str);
        }
    }
}
