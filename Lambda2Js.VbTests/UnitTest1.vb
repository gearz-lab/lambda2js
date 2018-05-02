Imports System.Linq.Expressions
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Lambda2Js.VbTests
    <TestClass>
    Public Class UnitTest1
        <TestMethod>
        Sub TestSub()
            Dim expr As Expression(Of Func(Of Char, String)) = Function(x As Char) x & "a"c
            Dim js = expr.CompileToJavascript()
            Assert.AreEqual("x+""a""", js)
        End Sub
    End Class
End Namespace

