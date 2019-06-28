using Lambda2Js.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Tests with .NET 4.0 from NuGet");

            //Expression<Func<int, int>> expr = x => 1024 + x;
            //var js = expr.CompileToJavascript(new JavascriptCompilationOptions(0, ScriptVersion.Es60));
            //Assert.AreEqual(@"x=>1024+x", js);

            //Console.WriteLine("Press ENTER to exit");
            //Console.ReadLine();



            Assert.Default = new MyAssert();

            Console.WriteLine("Tests with .NET 4.0");
            Console.WriteLine("===================");
            Console.WriteLine("");
            Console.WriteLine("This tool will run all the tests using the .Net Framework 4.0");
            Console.WriteLine("It is needed because I could not find the usual test classes");
            Console.WriteLine("in this version of the framework.");
            Console.WriteLine("");
            Console.WriteLine("===================");
            Console.WriteLine("");

            var allTestClasses = typeof(GeneralTests).Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestClassAttribute>().Any())
                .ToArray();

            int passed = 0;
            int total = 0;

            foreach (var t in allTestClasses)
            {
                var methods = t.GetMethods()
                    .Where(m => m.GetCustomAttributes<TestMethodAttribute>().Any())
                    .ToArray();

                var obj = Activator.CreateInstance(t);

                foreach (var mi in methods)
                {
                    total++;
                    bool ok = false;
                    try
                    {
                        mi.Invoke(obj, null);
                        ok = true;
                    }
                    catch (TargetInvocationException tex)
                    {
                        var ex = tex.InnerException;

                        ok = mi.GetCustomAttributes<ExpectedException>().Where(x => x.Accept(ex)).Any();

                        if (!ok)
                        {
                            var prev = Console.ForegroundColor;
                            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.Write(ok ? "  OK  " : " FAIL ");
                            Console.ForegroundColor = prev;
                            Console.WriteLine($"{ex.GetType().Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ok = false;
                        var prev = Console.ForegroundColor;
                        Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.Write(ok ? "  OK  " : " FAIL ");
                        Console.ForegroundColor = prev;
                        Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                    }

                    if (ok)
                        passed++;
                }
            }

            var prev2 = Console.ForegroundColor;
            Console.WriteLine("");
            Console.WriteLine("===================");
            Console.WriteLine("");
            Console.WriteLine("Tests finished running:");
            Console.Write("Passed: ");
            Console.ForegroundColor = passed > 0 ? ConsoleColor.Green : prev2;
            Console.WriteLine(passed);
            Console.ForegroundColor = prev2;
            Console.Write("Failed: ");
            Console.ForegroundColor = total - passed > 0 ? ConsoleColor.Red : prev2;
            Console.WriteLine(total - passed);
            Console.ForegroundColor = prev2;
            Console.Write("Total: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(total);
            Console.ForegroundColor = prev2;
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(intercept: true);
        }
    }
}
