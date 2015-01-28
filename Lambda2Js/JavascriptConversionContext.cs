using System;
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
}
