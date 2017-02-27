using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lambda2Js.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class UsingCustomMethods
    {
        [TestMethod]
        public void LinqWhere1()
        {
            Expression<Func<JSArray, object>> expr = array => array.RemoveAt(2);

            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly | JsCompilationFlags.ScopeParameter,
                    new[] { new LinqMethods(), }));

            Assert.AreEqual("array.splice(2,1)", js);
        }

        public class JSArray
        {
            [JavascriptMethodName("splice", PositionalArguments = new object[] {0,1})]
            public JSArray RemoveAt(int index)
            {
                throw new NotSupportedException("Never called");
            }
        }
    }
}