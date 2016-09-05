using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class EnumTests
    {
        [TestMethod]
        public void EnumAsInteger0()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => 0;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(JsCompilationFlags.BodyOnly));
            Assert.AreEqual(@"0", js);
        }

        [TestMethod]
        public void EnumAsInteger1()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => SomeFlagsEnum.A;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(JsCompilationFlags.BodyOnly));
            Assert.AreEqual(@"1", js);
        }

        [TestMethod]
        public void EnumCompareAsInteger0()
        {
            Expression<Func<MyClassWithEnum, bool>> expr = o => o.SomeFlagsEnum == 0;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"SomeFlagsEnum===0", js);
        }

        [TestMethod]
        public void EnumCompareAsInteger1()
        {
            Expression<Func<MyClassWithEnum, bool>> expr = o => o.SomeFlagsEnum == SomeFlagsEnum.A;
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"SomeFlagsEnum===1", js);
        }

        [TestMethod]
        public void EnumCompareAsInteger2()
        {
            Expression<Func<MyClassWithEnum, bool>> expr = o => o.SomeFlagsEnum == (SomeFlagsEnum.A | SomeFlagsEnum.B);
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"SomeFlagsEnum===3", js);
        }

        [TestMethod]
        public void EnumCompareWithEnclosed()
        {
            SomeFlagsEnum enclosed = SomeFlagsEnum.B;
            Expression<Func<MyClassWithEnum, bool>> expr = o => o.SomeFlagsEnum == (SomeFlagsEnum.A ^ ~enclosed);
            var js = expr.CompileToJavascript();
            Assert.AreEqual(@"SomeFlagsEnum===(1^~2)", js);
        }

        [TestMethod]
        public void EnumCallWithEnumParam()
        {
            Expression<Action<MyClassWithEnum>> expr = o => o.SetGender(SomeFlagsEnum.B);
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new MyCustomClassEnumMethods()));
            Assert.AreEqual(@"o.SetGender(2)", js);
        }

        [TestMethod]
        public void EnumCallWithEnumParam2()
        {
            Expression<Action<MyClassWithEnum>> expr = o => o.SetGender(SomeFlagsEnum.A | SomeFlagsEnum.B);
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new MyCustomClassEnumMethods()));
            Assert.AreEqual(@"o.SetGender(3)", js);
        }

        [TestMethod]
        public void EnumAsString0()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => 0;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.UseStrings)));
            Assert.AreEqual(@"""""", js);
        }

        [TestMethod]
        public void EnumAsString1()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => SomeFlagsEnum.A;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.UseStrings)));
            Assert.AreEqual(@"""A""", js);
        }

        [TestMethod]
        public void EnumAsString2()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => SomeFlagsEnum.A | SomeFlagsEnum.B;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.FlagsAsStringWithSeparator | EnumOptions.UseStrings)));
            Assert.AreEqual(@"""B|A""", js);
        }

        [TestMethod]
        public void EnumAsString3()
        {
            Expression<Func<SomeStrangeFlagsEnum>> expr = () => SomeStrangeFlagsEnum.A | SomeStrangeFlagsEnum.B;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.FlagsAsStringWithSeparator | EnumOptions.UseStrings)));
            Assert.AreEqual(@"""C|1""", js);
        }

        [TestMethod]
        public void EnumAsArrayOfStrings0()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => 0;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.FlagsAsArray | EnumOptions.UseStrings)));
            Assert.AreEqual(@"[]", js);
        }

        [TestMethod]
        public void EnumAsArrayOfStrings1()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => SomeFlagsEnum.A;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.FlagsAsArray | EnumOptions.UseStrings)));
            Assert.AreEqual(@"[""A""]", js);
        }

        [TestMethod]
        public void EnumAsArrayOfStrings2()
        {
            Expression<Func<SomeFlagsEnum>> expr = () => SomeFlagsEnum.A | SomeFlagsEnum.B;
            var js = expr.CompileToJavascript(
                new JavascriptCompilationOptions(
                    JsCompilationFlags.BodyOnly,
                    new EnumConversionExtension(EnumOptions.FlagsAsArray | EnumOptions.UseStrings)));
            Assert.AreEqual(@"[""B"",""A""]", js);
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
                                context.PreventDefault();
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

    class MyClassWithEnum : MyClass
    {
        public SomeFlagsEnum SomeFlagsEnum { get; }
        public SomeLongEnum SomeLongEnum { get; }
        public SomeUnorderedFlagsEnum SomeUnorderedFlagsEnum { get; }
        public void SetGender(SomeFlagsEnum someFlagsEnum) { }
    }

    [Flags]
    enum SomeFlagsEnum
    {
        A = 1,
        B = 2,
    }

    [Flags]
    enum SomeStrangeFlagsEnum
    {
        A = 0x011,
        B = 0x101,
        C = 0x110,
    }

    enum SomeLongEnum : long
    {
        A = 1,
        B = 2,
    }

    [Flags]
    enum SomeUnorderedFlagsEnum
    {
        AB = 3,
        A = 1,
        B = 2,
    }
}