using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class EnumTests
    {
        [TestMethod]
        public void EnumCompareWithEnclosed()
        {
            SomeEnum enclosed = SomeEnum.B;
            Expression<Func<MyClassWithEnum, bool>> expr = o => o.SomeEnum == (SomeEnum.A ^ ~enclosed);
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"SomeEnum===(1^~2)", js);
        }
    }

    public class MyCustomClassEnumMethods : JavascriptConversionExtension
    {
        public override void ConvertToJavascript(JavascriptConversionContext context)
        {
            var methodCall = context.Node as MethodCallExpression;
            if (methodCall != null)
                if (methodCall.Method.DeclaringType == typeof(MyClassWithEnum))
                {
                    switch (methodCall.Method.Name)
                    {
                        case "SetGender":
                            {
                                using (context.Operation(JavascriptOperationTypes.Call))
                                {
                                    using (context.Operation(JavascriptOperationTypes.IndexerProperty))
                                    {
                                        context.WriteNode(methodCall.Object);
                                        context.WriteAccessor("SetGender");
                                    }

                                    context.WriteManyIsolated('(', ')', ',', methodCall.Arguments);
                                }

                                return;
                            }
                    }
                }
        }
    }

    public class EnumAsString : JavascriptConversionExtension
    {
        public override void ConvertToJavascript(JavascriptConversionContext context)
        {
            var cte = context.Node as ConstantExpression;
            if (cte != null)
            {

            }
        }
    }

    class MyClassWithEnum : MyClass
    {
        public SomeEnum SomeEnum { get; }
        public void SetGender(SomeEnum someEnum) { }
    }

    [Flags]
    enum SomeEnum
    {
        A = 1,
        B = 2,
    }
}