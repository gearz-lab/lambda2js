using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Lambda2Js
{
    public class JavascriptWriter
    {
        private readonly StringBuilder result = new StringBuilder();
        private readonly List<JavascriptOperationTypes> operandTypes = new List<JavascriptOperationTypes>();

        public override string ToString()
        {
            return this.result.ToString();
        }

        public int Length
        {
            get { return this.result.Length; }
        }

        public PrecedenceController Operation(JavascriptOperationTypes op)
        {
            return new PrecedenceController(this.result, this.operandTypes, op);
        }

        public IDisposable Operation(Expression node)
        {
            var op = JsOperationHelper.GetJsOperator(node.NodeType, node.Type);
            if (op == JavascriptOperationTypes.NoOp)
                return null;

            return new PrecedenceController(this.result, this.operandTypes, op);
        }

        public JavascriptWriter Append(char p)
        {
            this.result.Append(p);
            return this;
        }

        public JavascriptWriter WriteOperator(ExpressionType expressionType)
        {
            JsOperationHelper.WriteOperator(this.result, expressionType);
            return this;
        }

        public JavascriptWriter Append(string str)
        {
            this.result.Append(str);
            return this;
        }

        public JavascriptWriter Append(object value)
        {
            this.result.Append(value);
            return this;
        }

        public JavascriptWriter AppendFormat(string format, params object[] values)
        {
            this.result.AppendFormat(format, values);
            return this;
        }
    }
}