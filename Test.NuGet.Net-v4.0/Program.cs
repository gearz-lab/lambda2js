using Lambda2Js;
using System;
using System.Linq.Expressions;

namespace Test.NuGet.Net_v4._0
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Tests with .NET 4.0 from NuGet");

            Expression<Func<int, int>> expr = x => 1024 + x;
            var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0, ScriptVersion.Es60));
            Assert.AreEqual(@"x=>1024+x", js);

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
